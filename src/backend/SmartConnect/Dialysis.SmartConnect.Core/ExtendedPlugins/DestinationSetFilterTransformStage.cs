using System.Text;
using System.Text.Json;
using Jint;
using Jint.Native;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Mirth-equivalent Destination Set Filter transformer step (User Guide p 284). Runs a JavaScript snippet
/// whose only side-effect is mutating a <c>destinationSet</c> object that exposes
/// <c>removeAllExcept([routeNames])</c>, <c>remove([routeNames])</c>, and <c>removeAll()</c>. After the
/// script runs, the resulting set of allowed route names is written to
/// <see cref="DestinationSetMetadataKey"/> on the returned message as a comma-separated list. The
/// <see cref="FlowRuntimeEngine"/> reads that metadata between transform and dispatch and skips routes
/// not in the set.
///
/// <para>Parameters JSON shape:</para>
/// <code>
/// {
///   "script": "destinationSet.removeAllExcept(['route-a']);",
///   "availableRouteNames": ["route-a", "route-b", "route-c"]
/// }
/// </code>
/// If <c>availableRouteNames</c> is omitted, the script starts with an empty allowed set and must call
/// <c>removeAllExcept</c> (additive). Either way, the engine treats missing-from-set routes as Skipped.
/// </summary>
public sealed class DestinationSetFilterTransformStage : ITransformStage
{
    public const string ParametersMetadataKey = "smartconnect.transform.parameters";
    public const string KindValue = "destinationSetFilter";

    /// <summary>Metadata key the engine reads to find the allowed-route-names set.</summary>
    public const string DestinationSetMetadataKey = "smartconnect.destinationSet";

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(message);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var script = root.TryGetProperty("script", out var scriptEl) ? scriptEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(script))
            return Task.FromResult(message);

        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("availableRouteNames", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var name in arr.EnumerateArray())
            {
                var s = name.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    available.Add(s);
            }
        }

        // Start with everything allowed (matches Mirth: the script REMOVES from a full set).
        var allowed = new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);
        var destinationSet = new DestinationSetController(allowed, available);

        cancellationToken.ThrowIfCancellationRequested();
        var engine = new Engine(options =>
        {
            options.LimitRecursion(64);
            options.TimeoutInterval(TimeSpan.FromSeconds(3));
        });

        engine.SetValue("destinationSet", destinationSet);

        var payloadText = message.PayloadFormat is PayloadFormat.Utf8Text or PayloadFormat.PlainText or PayloadFormat.Json
            ? Encoding.UTF8.GetString(message.Payload.Span)
            : Convert.ToBase64String(message.Payload.Span);
        engine.SetValue("payloadText", payloadText);

        engine.Evaluate(script!);

        var allowedCsv = string.Join(',', allowed);
        return Task.FromResult(message.WithMetadata(DestinationSetMetadataKey, allowedCsv));
    }

    /// <summary>JS-exposed controller that mutates the allowed-route-names set in place.</summary>
    public sealed class DestinationSetController
    {
        private readonly HashSet<string> _allowed;
        private readonly HashSet<string> _available;

        public DestinationSetController(HashSet<string> allowed, HashSet<string> available)
        {
            _allowed = allowed;
            _available = available;
        }

        public void removeAllExcept(JsValue keep)
        {
            var keepSet = ToStringSet(keep);
            _allowed.Clear();
            foreach (var name in _available.Where(keepSet.Contains))
                _allowed.Add(name);
            // Also allow names that aren't in _available (user knows what they want).
            foreach (var name in keepSet.Where(n => !_allowed.Contains(n)))
                _allowed.Add(name);
        }

        public void remove(JsValue drop)
        {
            foreach (var name in ToStringSet(drop))
                _allowed.Remove(name);
        }

        public void removeAll() => _allowed.Clear();

        private static HashSet<string> ToStringSet(JsValue value)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (value is null || value.IsNull() || value.IsUndefined())
                return set;
            if (value.IsArray())
            {
                var array = value.AsArray();
                for (uint i = 0; i < array.Length; i++)
                {
                    var v = array[i];
                    if (v.IsString())
                        set.Add(v.AsString());
                }

                return set;
            }

            if (value.IsString())
                set.Add(value.AsString());
            return set;
        }
    }
}
