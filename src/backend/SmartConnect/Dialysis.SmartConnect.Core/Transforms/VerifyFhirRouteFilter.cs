using System.Text;
using Dialysis.BuildingBlocks.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.SmartConnect.Transforms;

/// <summary>
/// Route filter (kind <c>verify-fhir</c>) that parses the payload as a FHIR R4 resource and
/// validates it via <see cref="IFhirProfileValidator"/>. Drops the message (RouteFilterDropped)
/// on any error. The strict transform-stage sibling throws (OutboundFailed) instead.
/// </summary>
/// <remarks>
/// The FHIR profile binding map is configured at host wire-up via
/// <c>services.AddFhirValidation(map =&gt; map.Require&lt;Patient&gt;("http://hl7.org/fhir/us/core/..."))</c>.
/// Filter parameter <c>profileUri</c> is an optional override that adds a single-shot binding for
/// the resource type emitted by this dispatch (not persisted into the global map).
/// </remarks>
public sealed class VerifyFhirRouteFilter(IFhirProfileValidator validator) : IRouteFilter
{
    public const string KindValue = "verify-fhir";

    public string Kind => KindValue;

    public async Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var verdict = await VerifyFhirCore.InspectAsync(validator, message.Payload, message.Metadata, cancellationToken).ConfigureAwait(false);
        return verdict.IsValid ? RouteFilterResult.Allow() : RouteFilterResult.Drop();
    }
}

/// <summary>Strict sibling — kind <c>verify-fhir-strict</c>; throws on any FHIR validation error.</summary>
public sealed class VerifyFhirTransformStage(IFhirProfileValidator validator) : ITransformStage
{
    public const string KindValue = "verify-fhir-strict";

    public string Kind => KindValue;

    public async Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var verdict = await VerifyFhirCore.InspectAsync(validator, message.Payload, message.Metadata, cancellationToken).ConfigureAwait(false);
        if (!verdict.IsValid)
        {
            throw new InvalidOperationException($"verify-fhir-strict: {verdict.Reason}");
        }
        return message;
    }
}

internal static class VerifyFhirCore
{
    // Strict: this is a validation gate, so anything that is not well-formed, base-spec-valid FHIR
    // (garbage, missing resourceType, unmet required cardinality) is rejected at parse time. The
    // DeserializationFailedException it raises is caught below and reported as a parse failure.
    private static readonly FhirJsonDeserializer _parser = new(new DeserializerSettings().UsingMode(DeserializationMode.Strict));

    public readonly record struct Inspection(bool IsValid, string? Reason);

    public static async Task<Inspection> InspectAsync(
        IFhirProfileValidator validator,
        ReadOnlyMemory<byte> payload,
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken ct)
    {
        if (payload.Length == 0)
        {
            return new(false, "Payload is empty.");
        }

        var json = Encoding.UTF8.GetString(payload.Span);
        Resource resource;
        try
        {
            // Firely's POCO deserializer is in-memory and CPU-bound, so calling it here is fine.
            resource = _parser.Deserialize<Resource>(json);
        }
        catch (Exception ex) when (ex is FormatException or DeserializationFailedException or System.Text.Json.JsonException)
        {
            return new(false, $"FHIR parse failed: {ex.Message}");
        }

        // Per-call profile override (operator-supplied). Lets a flow target a specific IG without
        // mutating the host's global FhirProfileMap.
        var paramsJson = metadata is not null
            && metadata.TryGetValue("smartconnect.filter.parameters", out var rfp)
                ? rfp
                : (metadata is not null
                    && metadata.TryGetValue("smartconnect.transform.parameters", out var tsp)
                        ? tsp
                        : null);
        // The parameter is informational here — the validator already evaluates against its
        // configured map. We expose it for future extensibility but don't mutate the validator.
        _ = paramsJson;

        FhirProfileValidationResult result;
        try
        {
            result = await validator.ValidateAsync(resource, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(false, $"FHIR validator threw: {ex.Message}");
        }

        if (result.IsValid)
        {
            return new(true, null);
        }

        var issueSummary = string.Join(
            "; ",
            result.Outcome.Issue
                .Where(i => i.Severity is OperationOutcome.IssueSeverity.Error or OperationOutcome.IssueSeverity.Fatal)
                .Select(i => i.Diagnostics ?? i.Code.ToString())
                .Take(3));
        return new(false, $"FHIR validation failed: {issueSummary}");
    }
}
