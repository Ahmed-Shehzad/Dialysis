using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.VariableMaps;
using Jint;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Executes channel lifecycle JavaScript scripts in a sandboxed Jint engine.
/// </summary>
public sealed class ChannelScriptExecutor
{
    private readonly IVariableMapStore _variableMapStore;
    private readonly ILogger<ChannelScriptExecutor> _logger;
    private readonly IFlowExecutionContextAccessor? _contextAccessor;
    private readonly ICodeTemplateLibraryRepository? _codeTemplateRepository;
    private readonly IAttachmentStore? _attachmentStore;
    /// <summary>
    /// Executes channel lifecycle JavaScript scripts in a sandboxed Jint engine.
    /// </summary>
    public ChannelScriptExecutor(IVariableMapStore variableMapStore,
        ILogger<ChannelScriptExecutor> logger,
        IFlowExecutionContextAccessor? contextAccessor = null,
        ICodeTemplateLibraryRepository? codeTemplateRepository = null,
        IAttachmentStore? attachmentStore = null)
    {
        _variableMapStore = variableMapStore;
        _logger = logger;
        _contextAccessor = contextAccessor;
        _codeTemplateRepository = codeTemplateRepository;
        _attachmentStore = attachmentStore;
    }
    private static readonly TimeSpan _scriptTimeout = TimeSpan.FromSeconds(3);
    private const int MaxRecursionDepth = 64;

    /// <summary>
    /// Runs a preprocessor script. Returns modified payload (or null to reject/drop the message).
    /// </summary>
    public async Task<PreProcessorResult> RunPreProcessorAsync(
        string script,
        IntegrationMessage message,
        CancellationToken cancellationToken)
    {
        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);

        var globalChannel = await _variableMapStore
            .GetAllAsync(VariableMapScope.GlobalChannel, message.FlowId, cancellationToken)
            .ConfigureAwait(false);
        var global = await _variableMapStore
            .GetAllAsync(VariableMapScope.Global, null, cancellationToken)
            .ConfigureAwait(false);
        var configuration = await _variableMapStore
            .GetAllAsync(VariableMapScope.Configuration, null, cancellationToken)
            .ConfigureAwait(false);

        var ctx = _contextAccessor?.Current ?? new FlowExecutionContext();

        try
        {
            var engine = CreateEngine();
            engine.SetValue("msg", payloadText);
            engine.SetValue("logger", new ScriptLogger(_logger));
            engine.SetValue("flowId", message.FlowId.ToString());
            engine.SetValue("correlationId", message.CorrelationId);

            var bound = VariableMapsJsBinder.BindAll(engine, ctx, globalChannel, global, configuration);
            await BindCodeTemplatesAsync(engine, message.FlowId, CodeTemplateContext.ChannelPreprocessor, cancellationToken).ConfigureAwait(false);
            AttachmentJsBinder.Bind(engine, _attachmentStore, message.FlowId, message.Id, "application/octet-stream", cancellationToken);

            var result = await engine.EvaluateAsync(script, cancellationToken: cancellationToken).ConfigureAwait(false);

            await PersistBackAsync(bound, message.FlowId, cancellationToken).ConfigureAwait(false);

            // If script returns false explicitly, drop the message
            if (result.IsBoolean() && !result.AsBoolean())
            {
                return PreProcessorResult.Drop();
            }

            // If script returns a string, use it as the new payload
            if (result.IsString())
            {
                var newPayload = Encoding.UTF8.GetBytes(result.AsString());
                return PreProcessorResult.Mutated(newPayload);
            }

            return PreProcessorResult.PassThrough();
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "PreProcessor script timed out for flow {FlowId}.", message.FlowId);
            return PreProcessorResult.PassThrough();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PreProcessor script error for flow {FlowId}.", message.FlowId);
            return PreProcessorResult.PassThrough();
        }
    }

    /// <summary>
    /// Runs a postprocessor script (fire-and-forget, no return value used).
    /// </summary>
    public async Task RunPostProcessorAsync(
        string script,
        IntegrationMessage message,
        bool succeeded,
        CancellationToken cancellationToken)
    {
        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);

        var globalChannel = await _variableMapStore
            .GetAllAsync(VariableMapScope.GlobalChannel, message.FlowId, cancellationToken)
            .ConfigureAwait(false);
        var global = await _variableMapStore
            .GetAllAsync(VariableMapScope.Global, null, cancellationToken)
            .ConfigureAwait(false);
        var configuration = await _variableMapStore
            .GetAllAsync(VariableMapScope.Configuration, null, cancellationToken)
            .ConfigureAwait(false);

        var ctx = _contextAccessor?.Current ?? new FlowExecutionContext();

        try
        {
            var engine = CreateEngine();
            engine.SetValue("msg", payloadText);
            engine.SetValue("logger", new ScriptLogger(_logger));
            engine.SetValue("flowId", message.FlowId.ToString());
            engine.SetValue("correlationId", message.CorrelationId);
            engine.SetValue("succeeded", succeeded);

            var bound = VariableMapsJsBinder.BindAll(engine, ctx, globalChannel, global, configuration);
            await BindCodeTemplatesAsync(engine, message.FlowId, CodeTemplateContext.ChannelPostprocessor, cancellationToken).ConfigureAwait(false);
            AttachmentJsBinder.Bind(engine, _attachmentStore, message.FlowId, message.Id, "application/octet-stream", cancellationToken);

            await engine.ExecuteAsync(script, cancellationToken: cancellationToken).ConfigureAwait(false);

            await PersistBackAsync(bound, message.FlowId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostProcessor script error for flow {FlowId}.", message.FlowId);
        }
    }

    /// <summary>Runs deploy/undeploy scripts (no message context).</summary>
    public void RunLifecycleScript(string script, Guid flowId)
    {
        try
        {
            var engine = CreateEngine();
            engine.SetValue("flowId", flowId.ToString());
            engine.SetValue("logger", new ScriptLogger(_logger));
            engine.Execute(script);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lifecycle script error for flow {FlowId}.", flowId);
        }
    }

    private static Engine CreateEngine() =>
        new(options => options
            .TimeoutInterval(_scriptTimeout)
            .LimitRecursion(MaxRecursionDepth)
            .Strict(false));

    private async Task BindCodeTemplatesAsync(Engine engine, Guid flowId, CodeTemplateContext context, CancellationToken ct)
    {
        if (_codeTemplateRepository is null)
            return;
        await CodeTemplateJsBinder.PrependLinkedTemplatesAsync(engine, _codeTemplateRepository, flowId, context, ct).ConfigureAwait(false);
    }

    private async Task PersistBackAsync(
        VariableMapsJsBinder.BoundMaps bound,
        Guid flowId,
        CancellationToken cancellationToken)
    {
        foreach (var kvp in bound.GlobalChannel)
        {
            await _variableMapStore.SetAsync(
                VariableMapScope.GlobalChannel,
                flowId,
                kvp.Key,
                kvp.Value?.ToString() ?? "",
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var kvp in bound.Global)
        {
            await _variableMapStore.SetAsync(
                VariableMapScope.Global,
                null,
                kvp.Key,
                kvp.Value?.ToString() ?? "",
                cancellationToken).ConfigureAwait(false);
        }
    }

    public sealed class ScriptLogger
    {
        private readonly ILogger _logger1;
        public ScriptLogger(ILogger logger) => _logger1 = logger;
        public void Info(string msg) => _logger1.LogInformation("[Script] {Message}", msg);
        public void Warn(string msg) => _logger1.LogWarning("[Script] {Message}", msg);
        public void Error(string msg) => _logger1.LogError("[Script] {Message}", msg);
    }
}

public readonly struct PreProcessorResult
{
    public bool Dropped { get; private init; }
    public byte[]? NewPayload { get; private init; }

    public static PreProcessorResult Drop() => new() { Dropped = true };
    public static PreProcessorResult PassThrough() => new() { Dropped = false, NewPayload = null };
    public static PreProcessorResult Mutated(byte[] payload) => new() { Dropped = false, NewPayload = payload };
}
