using System.Text;
using Jint;
using Jint.Runtime;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Executes channel lifecycle JavaScript scripts in a sandboxed Jint engine.
/// </summary>
public sealed class ChannelScriptExecutor(IVariableMapStore variableMapStore, ILogger<ChannelScriptExecutor> logger)
{
    private static readonly TimeSpan ScriptTimeout = TimeSpan.FromSeconds(3);
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

        var channelMap = await variableMapStore
            .GetAllAsync(VariableMapScope.GlobalChannel, message.FlowId, cancellationToken)
            .ConfigureAwait(false);
        var globalMap = await variableMapStore
            .GetAllAsync(VariableMapScope.Global, null, cancellationToken)
            .ConfigureAwait(false);
        var configMap = await variableMapStore
            .GetAllAsync(VariableMapScope.Configuration, null, cancellationToken)
            .ConfigureAwait(false);

        var channelMapDict = channelMap.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
        var globalMapDict = globalMap.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

        try
        {
            var engine = CreateEngine();
            engine.SetValue("msg", payloadText);
            engine.SetValue("channelMap", new ScriptMap(channelMapDict));
            engine.SetValue("globalMap", new ScriptMap(globalMapDict));
            engine.SetValue("configurationMap", new ReadOnlyScriptMap(configMap));
            engine.SetValue("logger", new ScriptLogger(logger));
            engine.SetValue("flowId", message.FlowId.ToString());
            engine.SetValue("correlationId", message.CorrelationId);

            var result = engine.Evaluate(script);

            // Persist channelMap changes back
            foreach (var kvp in channelMapDict)
            {
                await variableMapStore.SetAsync(
                    VariableMapScope.GlobalChannel, message.FlowId, kvp.Key, kvp.Value?.ToString() ?? "", cancellationToken)
                    .ConfigureAwait(false);
            }

            // Persist globalMap changes back
            foreach (var kvp in globalMapDict)
            {
                await variableMapStore.SetAsync(
                    VariableMapScope.Global, null, kvp.Key, kvp.Value?.ToString() ?? "", cancellationToken)
                    .ConfigureAwait(false);
            }

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

            // Otherwise, pass through original payload
            return PreProcessorResult.PassThrough();
        }
        catch (TimeoutException)
        {
            logger.LogWarning("PreProcessor script timed out for flow {FlowId}.", message.FlowId);
            return PreProcessorResult.PassThrough();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PreProcessor script error for flow {FlowId}.", message.FlowId);
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
        var globalMap = await variableMapStore
            .GetAllAsync(VariableMapScope.Global, null, cancellationToken)
            .ConfigureAwait(false);
        var globalMapDict = globalMap.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

        try
        {
            var engine = CreateEngine();
            engine.SetValue("msg", payloadText);
            engine.SetValue("globalMap", new ScriptMap(globalMapDict));
            engine.SetValue("logger", new ScriptLogger(logger));
            engine.SetValue("flowId", message.FlowId.ToString());
            engine.SetValue("correlationId", message.CorrelationId);
            engine.SetValue("succeeded", succeeded);
            engine.Execute(script);

            foreach (var kvp in globalMapDict)
            {
                await variableMapStore.SetAsync(
                    VariableMapScope.Global, null, kvp.Key, kvp.Value?.ToString() ?? "", cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PostProcessor script error for flow {FlowId}.", message.FlowId);
        }
    }

    /// <summary>Runs deploy/undeploy scripts (no message context).</summary>
    public void RunLifecycleScript(string script, Guid flowId)
    {
        try
        {
            var engine = CreateEngine();
            engine.SetValue("flowId", flowId.ToString());
            engine.SetValue("logger", new ScriptLogger(logger));
            engine.Execute(script);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lifecycle script error for flow {FlowId}.", flowId);
        }
    }

    private static Engine CreateEngine() =>
        new(options => options
            .TimeoutInterval(ScriptTimeout)
            .LimitRecursion(MaxRecursionDepth)
            .Strict(false));

    public sealed class ScriptMap(Dictionary<string, object?> store)
    {
        public object? get(string key) => store.TryGetValue(key, out var v) ? v : null;
        public void put(string key, object? value) => store[key] = value?.ToString();
    }

    public sealed class ReadOnlyScriptMap(IReadOnlyDictionary<string, string> store)
    {
        public string? get(string key) => store.TryGetValue(key, out var v) ? v : null;
    }

    public sealed class ScriptLogger(ILogger logger)
    {
        public void info(string msg) => logger.LogInformation("[Script] {Message}", msg);
        public void warn(string msg) => logger.LogWarning("[Script] {Message}", msg);
        public void error(string msg) => logger.LogError("[Script] {Message}", msg);
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
