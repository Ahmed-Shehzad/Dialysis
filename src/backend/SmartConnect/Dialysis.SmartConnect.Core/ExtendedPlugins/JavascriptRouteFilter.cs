using System.Text;
using System.Text.Json;
using Jint;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Sandboxed JavaScript route filter via Jint. Parameters JSON must include <c>script</c>.
/// Exposes <c>payloadText</c>, <c>metadata</c> (object), <c>correlationId</c>, and <c>flowId</c>.
/// Truthy return → Allow; falsy → Drop.
/// </summary>
public sealed class JavascriptRouteFilter : IRouteFilter
{
    public const string ParametersMetadataKey = "smartconnect.filter.parameters";

    public string Kind => "javascript";

    public Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(RouteFilterResult.Allow());
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("script", out var scriptEl))
        {
            return Task.FromResult(RouteFilterResult.Allow());
        }

        var script = scriptEl.GetString();
        if (string.IsNullOrWhiteSpace(script))
        {
            return Task.FromResult(RouteFilterResult.Allow());
        }

        var payloadText = message.PayloadFormat is PayloadFormat.Utf8Text or PayloadFormat.PlainText or PayloadFormat.Json
            ? Encoding.UTF8.GetString(message.Payload.Span)
            : Convert.ToBase64String(message.Payload.Span);

        cancellationToken.ThrowIfCancellationRequested();

        var engine = new Engine(options =>
        {
            options.LimitRecursion(64);
            options.TimeoutInterval(TimeSpan.FromSeconds(3));
        });

        engine.SetValue("payloadText", payloadText);
        engine.SetValue("correlationId", message.CorrelationId);
        engine.SetValue("flowId", message.FlowId.ToString());

        // Expose metadata as a plain JS object
        var metaDict = new Dictionary<string, string>(message.Metadata.Count);
        foreach (var kvp in message.Metadata)
        {
            metaDict[kvp.Key] = kvp.Value;
        }

        engine.SetValue("metadata", metaDict);

        var result = engine.Evaluate(script!).ToObject();
        var truthy = result switch
        {
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            null => false,
            _ => true,
        };

        return Task.FromResult(truthy ? RouteFilterResult.Allow() : RouteFilterResult.Drop());
    }
}
