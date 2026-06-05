using System.Text.Json;
using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Integration.Features.IngestDeviceReading;

/// <summary>
/// Guards the device-ingest entry point — an integration boundary that accepts data from
/// devices/gateways outside the trust boundary, so the payload is validated before it reaches
/// the handler (defence in depth alongside the durable-bus + idempotency machinery).
/// </summary>
public sealed class IngestDeviceReadingCommandValidator : AbstractValidator<IngestDeviceReadingCommand>
{
    public IngestDeviceReadingCommandValidator()
    {
        RuleFor(static c => c.DeviceId, nameof(IngestDeviceReadingCommand.DeviceId))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 128)
            .WithMessage("DeviceId is required and must be at most 128 characters.");

        RuleFor(static c => c.PatientId, nameof(IngestDeviceReadingCommand.PatientId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("PatientId is required.");

        RuleFor(static c => c.PayloadJson, nameof(IngestDeviceReadingCommand.PayloadJson))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && IsValidJson(v))
            .WithMessage("PayloadJson is required and must be well-formed JSON.");

        RuleFor(static c => c.ExternalMessageId, nameof(IngestDeviceReadingCommand.ExternalMessageId))
            .Must(static (_, v) => v is null || v.Length <= 128)
            .WithMessage("ExternalMessageId must be at most 128 characters.");
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
