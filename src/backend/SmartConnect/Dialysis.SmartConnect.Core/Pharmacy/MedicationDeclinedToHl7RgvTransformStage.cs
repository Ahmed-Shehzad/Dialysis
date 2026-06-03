using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.Pharmacy;

/// <summary>
/// Transform stage that turns an upstream <c>MedicationDeclinedIntegrationEvent</c> (carried as a
/// JSON payload) into an HL7 v2.5 <c>RGV^O15</c> pharmacy-give message with an NTE refusal note,
/// communicating to an external pharmacy system that an ordered dose was declined and not given.
///
/// Decoupling + fail-soft behaviour matches
/// <see cref="MedicationAdministeredToHl7RasTransformStage"/>: deserialises into a local DTO (no
/// PDMS reference), and a payload that isn't the expected JSON shape passes through unchanged.
/// </summary>
public sealed class MedicationDeclinedToHl7RgvTransformStage(TimeProvider clock) : ITransformStage
{
    public const string KindValue = "medication.declined.to.hl7-rgv";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
        => Task.FromResult(Transform(message));

    private IntegrationMessage Transform(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        EventDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<EventDto>(message.Payload.Span, _jsonOptions);
        }
        catch (JsonException)
        {
            return message;
        }

        if (dto is null
            || string.IsNullOrWhiteSpace(dto.MedicationCode)
            || string.IsNullOrWhiteSpace(dto.PatientId))
        {
            return message;
        }

        var frame = new MedicationGiveFrame(
            PatientIdentifier: dto.PatientId,
            PlacerOrderNumber: dto.RelatedOrderId,
            Medication: new PharmacyMedication(
                dto.MedicationCode,
                dto.MedicationCode,
                dto.MedicationCodeSystem ?? string.Empty),
            GiveAtUtc: dto.DeclinedAtUtc,
            RecordedBy: dto.DeclinedBySub ?? string.Empty,
            Reason: string.IsNullOrWhiteSpace(dto.Reason) ? "Declined" : dto.Reason);

        var controlId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : message.CorrelationId;

        var wire = Hl7V2RgvO15Builder.Build(frame, controlId, clock.GetUtcNow().UtcDateTime);
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(wire), PayloadFormat.Utf8Text);
    }

    private sealed record EventDto
    {
        public string? PatientId { get; init; }
        public string? MedicationCodeSystem { get; init; }
        public string? MedicationCode { get; init; }
        public DateTime DeclinedAtUtc { get; init; }
        public string? DeclinedBySub { get; init; }
        public string? Reason { get; init; }
        public string? RelatedOrderId { get; init; }
    }
}
