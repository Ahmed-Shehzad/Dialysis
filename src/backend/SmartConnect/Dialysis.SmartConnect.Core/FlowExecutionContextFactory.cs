using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using Dialysis.SmartConnect.VariableMaps;

namespace Dialysis.SmartConnect;

/// <summary>
/// Builds the per-dispatch <see cref="FlowExecutionContext"/> (Mirth Source/Channel/Connector/Response
/// scopes), hydrating <see cref="FlowExecutionContext.SourceMap"/> from the
/// <see cref="FlowRuntimeEngine.SourceMapMetadataKey"/> metadata JSON when present.
/// </summary>
internal static class FlowExecutionContextFactory
{
    /// <summary>Creates the per-dispatch variable-map context for <paramref name="message"/> and <paramref name="flow"/>.</summary>
    public static FlowExecutionContext Create(IntegrationMessage message, IntegrationFlow flow)
    {
        var sourceMap = ParseSourceMap(message);
        var connectorMaps = new ConcurrentDictionary<string, object?>[flow.Pipeline.OutboundRoutes.Count];
        for (var i = 0; i < connectorMaps.Length; i++)
        {
            connectorMaps[i] = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        }

        return new FlowExecutionContext
        {
            MessageId = message.Id,
            FlowId = message.FlowId,
            SourceMap = sourceMap,
            ConnectorMaps = connectorMaps,
        };
    }

    private static IReadOnlyDictionary<string, object?> ParseSourceMap(IntegrationMessage message)
    {
        if (!message.Metadata.TryGetValue(FlowRuntimeEngine.SourceMapMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return ImmutableDictionary<string, object?>.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return ImmutableDictionary<string, object?>.Empty;
            }

            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = JsonElementToObject(prop.Value);
            }

            return dict;
        }
        catch (JsonException)
        {
            return ImmutableDictionary<string, object?>.Empty;
        }
    }

    private static object? JsonElementToObject(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                return el.TryGetInt64(out var l) ? (object)l : el.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Object:
            {
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var p in el.EnumerateObject())
                {
                    dict[p.Name] = JsonElementToObject(p.Value);
                }
                return dict;
            }
            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in el.EnumerateArray())
                {
                    list.Add(JsonElementToObject(item));
                }
                return list;
            }
            default:
                return null;
        }
    }
}
