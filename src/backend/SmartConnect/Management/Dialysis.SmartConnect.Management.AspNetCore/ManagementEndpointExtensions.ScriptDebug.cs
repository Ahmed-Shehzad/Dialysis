using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Script-debugging routes (<c>/debug/evaluate-script</c>).</summary>
public static partial class ManagementEndpointExtensions
{
    // Debug: evaluate a JavaScript snippet against a stubbed HL7 payload through the same
    // binding path the real JavascriptTransformStage uses. Engineers can sanity-check a
    // transformer before deploying it, instead of round-tripping through send-message +
    // inspect-ledger. Body: { "script": "msg.GetValue('PID.3.1')", "payloadText": "MSH|..." }.
    // Response: { "result": "MRN-12345" } on success, { "error": "..." } on failure.
    internal static void MapScriptDebugEndpoints(RouteGroupBuilder admin)
    {
        admin.MapPost(
                "/debug/evaluate-script",
                async (
                    EvaluateScriptRequest body,
                    IServiceScopeFactory scopeFactory,
                    CancellationToken ct) =>
                {
                    if (string.IsNullOrWhiteSpace(body.Script))
                    {
                        return Results.BadRequest(new { error = "script is required." });
                    }

                    var parameters = JsonSerializer.Serialize(new { script = body.Script });
                    var payloadBytes = Encoding.UTF8.GetBytes(body.PayloadText ?? string.Empty);
                    var stub = new IntegrationMessage
                    {
                        Id = Guid.NewGuid(),
                        FlowId = Guid.Empty,
                        CorrelationId = "debug-" + Guid.NewGuid().ToString("N")[..8],
                        Payload = payloadBytes,
                        PayloadFormat = PayloadFormat.Utf8Text,
                        Metadata = ImmutableDictionary<string, string>.Empty.Add(
                            JavascriptTransformStage.ParametersMetadataKey,
                            parameters),
                        ReceivedAtUtc = DateTimeOffset.UtcNow,
                    };

                    try
                    {
                        // Resolve the stage from a fresh DI scope so the captured IServiceProvider
                        // inside JavascriptTransformStage can resolve Scoped variable-map services
                        // (IVariableMapStore, ICodeTemplateLibraryRepository, etc.).
                        await using var scope = scopeFactory.CreateAsyncScope();
                        var stage = new JavascriptTransformStage(scope.ServiceProvider);
                        var result = await stage.TransformAsync(stub, ct).ConfigureAwait(false);
                        return Results.Ok(new
                        {
                            result = Encoding.UTF8.GetString(result.Payload.Span),
                        });
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        return Results.UnprocessableEntity(new
                        {
                            error = ex.Message,
                            type = ex.GetType().Name,
                        });
                    }
                })
            .WithName("SmartConnect_EvaluateScript");
    }
}

/// <summary>Request body for <c>POST /api/v1/admin/debug/evaluate-script</c>.</summary>
public sealed class EvaluateScriptRequest
{
    public string? Script { get; set; }

    public string? PayloadText { get; set; }
}
