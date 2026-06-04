using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Transforms;

/// <summary>
/// Route filter (kind <c>verify-hl7</c>) that drops the message when:
/// <list type="bullet">
///   <item>The payload doesn't parse as HL7 v2 (missing MSH, bad encoding chars), OR</item>
///   <item>An operator-supplied <c>requiredSegments</c> list isn't satisfied, OR</item>
///   <item>An operator-supplied <c>minVersion</c> exceeds the message's MSH.12.</item>
/// </list>
/// Parameters JSON:
/// <code>{ "requiredSegments": ["MSH","PID","PV1"], "minVersion": "2.5" }</code>
/// All fields optional. Bare <c>verify-hl7</c> (no params) drops only on parse failure.
/// Pair with the strict transform-stage variant when the channel should fail loudly instead
/// of silently dropping.
/// </summary>
public sealed class VerifyHl7RouteFilter : IRouteFilter
{
    public const string KindValue = "verify-hl7";

    public string Kind => KindValue;

    public Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(message.Payload.Span);
        var verdict = VerifyHl7Core.Inspect(text, message.Metadata);
        return Task.FromResult(verdict.IsValid ? RouteFilterResult.Allow() : RouteFilterResult.Drop());
    }
}

/// <summary>
/// Strict variant — fails the dispatch (OutboundFailed) when the same conditions are unmet, so
/// upstream callers see an error response instead of a silent drop. Kind <c>verify-hl7-strict</c>.
/// </summary>
public sealed class VerifyHl7TransformStage : ITransformStage
{
    public const string KindValue = "verify-hl7-strict";

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(message.Payload.Span);
        var verdict = VerifyHl7Core.Inspect(text, message.Metadata);
        if (!verdict.IsValid)
        {
            throw new InvalidOperationException($"verify-hl7-strict: {verdict.Reason}");
        }
        return Task.FromResult(message);
    }
}

/// <summary>
/// Reusable HL7 v2 inspection helper — used by the <c>verify-hl7</c> filter, the
/// <c>verify-hl7-strict</c> transform stage, and the operator-facing HL7 Workbench.
/// </summary>
public static class VerifyHl7Core
{
    /// <summary>Verdict returned by <see cref="Inspect"/>.</summary>
    public readonly record struct Inspection
    {
        /// <summary>Verdict returned by <see cref="Inspect"/>.</summary>
        public Inspection(bool IsValid, string? Reason)
        {
            this.IsValid = IsValid;
            this.Reason = Reason;
        }
        public bool IsValid { get; init; }
        public string? Reason { get; init; }
        public void Deconstruct(out bool IsValid, out string? Reason)
        {
            IsValid = this.IsValid;
            Reason = this.Reason;
        }
    }

    /// <summary>
    /// Parses <paramref name="payloadText"/> and applies the operator-supplied rules carried in
    /// <paramref name="metadata"/> (under <c>smartconnect.filter.parameters</c> or
    /// <c>smartconnect.transform.parameters</c>). Returns a valid verdict only when every rule
    /// passes; fails closed on malformed parameter JSON.
    /// </summary>
    public static Inspection Inspect(string payloadText, IReadOnlyDictionary<string, string>? metadata)
    {
        if (string.IsNullOrEmpty(payloadText) || !payloadText.StartsWith("MSH", StringComparison.Ordinal))
        {
            return new(false, "Payload is not an HL7 v2 message (missing MSH segment).");
        }

        Hl7V2Message parsed;
        try
        {
            parsed = Hl7V2Message.Parse(payloadText);
        }
        catch (FormatException ex)
        {
            return new(false, $"HL7 parse failed: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return new(false, $"HL7 parse failed: {ex.Message}");
        }

        var paramsJson = metadata is not null
            && metadata.TryGetValue("smartconnect.filter.parameters", out var rfp)
                ? rfp
                : (metadata is not null
                    && metadata.TryGetValue("smartconnect.transform.parameters", out var tsp)
                        ? tsp
                        : null);
        if (!string.IsNullOrWhiteSpace(paramsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(paramsJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("requiredSegments", out var segs)
                        && segs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var seg in segs.EnumerateArray())
                        {
                            var name = seg.GetString();
                            if (!string.IsNullOrWhiteSpace(name)
                                && !parsed.Segments.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                            {
                                return new(false, $"Required segment '{name}' is missing.");
                            }
                        }
                    }

                    if (doc.RootElement.TryGetProperty("minVersion", out var verEl)
                        && verEl.ValueKind == JsonValueKind.String)
                    {
                        var minStr = verEl.GetString() ?? string.Empty;
                        var msgVer = parsed.GetValue("MSH.12") ?? string.Empty;
                        if (!IsVersionAtLeast(msgVer, minStr))
                        {
                            return new(false, $"HL7 version {msgVer} is below the configured minimum {minStr}.");
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Malformed params — treat as configuration error; fail closed (drop / throw).
                return new(false, "verify-hl7 parameters JSON is invalid.");
            }
        }

        return new(true, null);
    }

    private static bool IsVersionAtLeast(string actual, string minimum)
    {
        // HL7 v2 versions are numeric dotted (2.1, 2.3.1, 2.5, 2.5.1, ...). Compare component-wise.
        var a = actual.Split('.');
        var m = minimum.Split('.');
        var len = Math.Max(a.Length, m.Length);
        for (var i = 0; i < len; i++)
        {
            var ai = i < a.Length && int.TryParse(a[i], out var av) ? av : 0;
            var mi = i < m.Length && int.TryParse(m[i], out var mv) ? mv : 0;
            if (ai != mi) return ai > mi;
        }
        return true;
    }
}
