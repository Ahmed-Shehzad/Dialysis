using System.Text;
using System.Text.Json;
using Hl7.Fhir.Serialization;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// Transform stage that turns an upstream <c>LabOrderPlacedIntegrationEvent</c> (carried as a JSON
/// payload) into a FHIR R4 collection <c>Bundle</c> of <c>ServiceRequest</c> resources (one per
/// requested test), ready for dispatch through an HTTP outbound adapter to a FHIR-native Laboratory
/// Information System. The FHIR counterpart of <see cref="LabOrderPlacedToHl7OrmTransformStage"/> —
/// an operator selects HL7 v2 ORM or FHIR <c>ServiceRequest</c> per flow route by choosing the stage
/// Kind, so both transports are config-selected.
///
/// Shares <see cref="LabOrderEventParser"/> with the HL7 stage (same decoupled local DTO), and is
/// fail-soft: a payload that isn't a usable order passes through unchanged.
/// </summary>
public sealed class LabOrderPlacedToFhirServiceRequestStage : ITransformStage
{
    private readonly TimeProvider _clock;

    /// <summary>Creates the stage with the host clock used for the bundle timestamp.</summary>
    public LabOrderPlacedToFhirServiceRequestStage(TimeProvider clock) => _clock = clock;

    /// <summary>Flow-route stage kind selecting FHIR <c>ServiceRequest</c> transmission of a placed lab order.</summary>
    public const string KindValue = "lab.order.placed.to.fhir-servicerequest";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <inheritdoc />
    public string Kind => KindValue;

    /// <inheritdoc />
    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
        => Task.FromResult(Transform(message));

    private IntegrationMessage Transform(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var frame = LabOrderEventParser.TryParse(message.Payload.Span, _jsonOptions);
        if (frame is null)
        {
            return message;
        }

        var bundle = LabServiceRequestBuilder.BuildBundle(frame, _clock.GetUtcNow());

        // Synchronous ToJson kept off the async path, same rationale as Hl7V2ToFhirTransformStage.
        var json = bundle.ToJson();
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(json), PayloadFormat.Json);
    }
}
