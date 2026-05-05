using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dialysis.SmartConnect.Transforms;

/// <summary>
/// Transform stage that extracts or restructures JSON payloads using JSONPath-like key mappings.
/// Parameters JSON: { "mappings": { "outputKey": "$.inputPath" } } or { "expression": "$.path" } for single-value extraction.
/// </summary>
public sealed class JsonTransformStage : ITransformStage
{
    public string Kind => "json-transform";

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var parametersJson = message.Metadata.TryGetValue("smartconnect.transform.parameters", out var p) ? p : null;
        if (string.IsNullOrWhiteSpace(parametersJson))
            return Task.FromResult(message);

        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(payloadText);
        }
        catch (JsonException)
        {
            return Task.FromResult(message);
        }

        if (root is null)
            return Task.FromResult(message);

        var parameters = JsonNode.Parse(parametersJson);
        if (parameters is null)
            return Task.FromResult(message);

        // Single expression mode
        if (parameters["expression"] is JsonValue exprVal)
        {
            var path = exprVal.GetValue<string>();
            var extracted = EvaluateSimplePath(root, path);
            var resultBytes = Encoding.UTF8.GetBytes(extracted?.ToJsonString() ?? "null");
            return Task.FromResult(message.CloneWithPayload(resultBytes));
        }

        // Mappings mode
        if (parameters["mappings"] is JsonObject mappings)
        {
            var output = new JsonObject();
            foreach (var (key, valueNode) in mappings)
            {
                var path = valueNode?.GetValue<string>();
                if (path is null) continue;
                var extracted = EvaluateSimplePath(root, path);
                output[key] = extracted?.DeepClone();
            }

            var resultBytes = Encoding.UTF8.GetBytes(output.ToJsonString());
            return Task.FromResult(message.CloneWithPayload(resultBytes));
        }

        return Task.FromResult(message);
    }

    /// <summary>Simple dot-notation path evaluator (supports $.foo.bar and $.foo[0]).</summary>
    private static JsonNode? EvaluateSimplePath(JsonNode root, string path)
    {
        if (!path.StartsWith("$."))
            return null;

        var segments = path[2..].Split('.');
        JsonNode? current = root;
        foreach (var segment in segments)
        {
            if (current is null) return null;

            // Array index: segment like "items[0]"
            var bracketIdx = segment.IndexOf('[');
            if (bracketIdx >= 0)
            {
                var propName = segment[..bracketIdx];
                var indexStr = segment[(bracketIdx + 1)..^1];
                if (!string.IsNullOrEmpty(propName))
                    current = current[propName];
                if (current is JsonArray arr && int.TryParse(indexStr, out var idx))
                    current = arr[idx];
                else
                    return null;
            }
            else
            {
                current = current[segment];
            }
        }

        return current;
    }
}
