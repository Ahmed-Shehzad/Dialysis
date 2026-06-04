using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.Pharmacy;

/// <summary>
/// Transform stage that turns an upstream <c>MedicationAdministeredIntegrationEvent</c> (carried
/// as a JSON payload) into an HL7 v2.5 <c>RAS^O17</c> pharmacy-administration message, ready for
/// MLLP dispatch through <c>TcpOutboundAdapter</c> to an external pharmacy system.
///
/// Decoupling note: the stage deserialises into a local <see cref="EventDto"/> rather than
/// referencing the PDMS contracts assembly — SmartConnect ships as a standalone NuGet package and
/// must not take a module dependency. Property matching is case-insensitive so it tolerates either
/// the PascalCase Transponder envelope or a camelCase re-serialisation.
///
/// Fail-soft: a payload that isn't the expected JSON shape (missing medication code, unparseable)
/// passes through unchanged so a downstream route can handle it — mirroring
/// <c>Hl7V2ToFhirTransformStage</c>.
/// </summary>
public sealed class MedicationAdministeredToHl7RasTransformStage : ITransformStage
{
    private readonly TimeProvider _clock;
    /// <summary>
    /// Transform stage that turns an upstream <c>MedicationAdministeredIntegrationEvent</c> (carried
    /// as a JSON payload) into an HL7 v2.5 <c>RAS^O17</c> pharmacy-administration message, ready for
    /// MLLP dispatch through <c>TcpOutboundAdapter</c> to an external pharmacy system.
    ///
    /// Decoupling note: the stage deserialises into a local <see cref="EventDto"/> rather than
    /// referencing the PDMS contracts assembly — SmartConnect ships as a standalone NuGet package and
    /// must not take a module dependency. Property matching is case-insensitive so it tolerates either
    /// the PascalCase Transponder envelope or a camelCase re-serialisation.
    ///
    /// Fail-soft: a payload that isn't the expected JSON shape (missing medication code, unparseable)
    /// passes through unchanged so a downstream route can handle it — mirroring
    /// <c>Hl7V2ToFhirTransformStage</c>.
    /// </summary>
    public MedicationAdministeredToHl7RasTransformStage(TimeProvider clock) => _clock = clock;
    public const string KindValue = "medication.administered.to.hl7-ras";

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

        var frame = new MedicationAdministrationFrame(
            PatientIdentifier: dto.PatientId,
            PlacerOrderNumber: dto.RelatedOrderId,
            Medication: new PharmacyMedication(
                dto.MedicationCode,
                dto.MedicationDisplay ?? dto.MedicationCode,
                dto.MedicationCodeSystem ?? string.Empty),
            DoseQuantity: dto.DoseQuantity,
            DoseUnit: dto.DoseUnit ?? string.Empty,
            Route: dto.Route ?? string.Empty,
            AdministeredAtUtc: dto.AdministeredAtUtc,
            AdministeredBy: dto.AdministeredBySub ?? string.Empty);

        var controlId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : message.CorrelationId;

        var wire = Hl7V2RasO17Builder.Build(frame, controlId, _clock.GetUtcNow().UtcDateTime);
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(wire), PayloadFormat.Utf8Text);
    }

    private sealed record EventDto
    {
        public string? PatientId { get; init; }
        public string? MedicationCodeSystem { get; init; }
        public string? MedicationCode { get; init; }
        public string? MedicationDisplay { get; init; }
        public decimal DoseQuantity { get; init; }
        public string? DoseUnit { get; init; }
        public string? Route { get; init; }
        public DateTime AdministeredAtUtc { get; init; }
        public string? AdministeredBySub { get; init; }
        public string? RelatedOrderId { get; init; }
    }
}
