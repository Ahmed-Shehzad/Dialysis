using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Declarative route filter (subset of Mirth Rule Builder): JSON rules on payload and metadata.
/// Parameters JSON: { "match": "all"|"any", "rules": [ { "type": "payloadContains", "value": "..." }, { "type": "metadataEquals", "key": "...", "value": "..." } ] }
/// </summary>
public sealed class RuleBuilderRouteFilter : IRouteFilter
{
    public const string KindValue = "rule-builder";

    public string Kind => KindValue;

    public Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue("smartconnect.filter.parameters", out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return Task.FromResult(RouteFilterResult.Allow());
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            return Task.FromResult(RouteFilterResult.Allow());
        }

        if (root is null)
            return Task.FromResult(RouteFilterResult.Allow());

        var matchAll = !string.Equals(root["match"]?.GetValue<string>(), "any", StringComparison.OrdinalIgnoreCase);
        var rules = root["rules"] as JsonArray;
        if (rules is null || rules.Count == 0)
            return Task.FromResult(RouteFilterResult.Allow());

        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);
        var results = new List<bool>();
        foreach (var ruleNode in rules)
        {
            if (ruleNode is not JsonObject ro)
                continue;
            var type = ro["type"]?.GetValue<string>();
            var ok = type switch
            {
                "payloadContains" => payloadText.Contains(ro["value"]?.GetValue<string>() ?? "", StringComparison.Ordinal),
                "metadataEquals" =>
                    message.Metadata.TryGetValue(ro["key"]?.GetValue<string>() ?? "", out var mv) &&
                    string.Equals(mv, ro["value"]?.GetValue<string>(), StringComparison.Ordinal),
                _ => true,
            };
            results.Add(ok);
        }

        if (results.Count == 0)
            return Task.FromResult(RouteFilterResult.Allow());

        var pass = matchAll ? results.TrueForAll(x => x) : results.Exists(x => x);
        return Task.FromResult(pass ? RouteFilterResult.Allow() : RouteFilterResult.Drop());
    }
}
