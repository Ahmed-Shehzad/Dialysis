using System.Globalization;

using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Hl7.Stream;

/// <summary>
/// Parses HL7 v2 ORU messages and creates FHIR Observations. Stub supports ORU^R01 (lab/vitals).
/// </summary>
public sealed class ProcessHl7StreamHandler : ICommandHandler<ProcessHl7StreamCommand, Hl7StreamResponse>
{
    private readonly IObservationRepository _observationRepository;
    private readonly IPublisher _publisher;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProcessHl7StreamHandler> _logger;

    public ProcessHl7StreamHandler(
        IObservationRepository observationRepository,
        IPublisher publisher,
        ITenantContext tenantContext,
        ILogger<ProcessHl7StreamHandler> logger)
    {
        _observationRepository = observationRepository;
        _publisher = publisher;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Hl7StreamResponse> HandleAsync(ProcessHl7StreamCommand request, CancellationToken cancellationToken = default)
    {
        var messageId = Ulid.NewUlid().ToString();
        var tenantId = _tenantContext.TenantId;

        var segments = request.RawMessage
            .Replace("\r\n", "\r")
            .Replace("\n", "\r")
            .Split('\r', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return new Hl7StreamResponse { MessageId = messageId };

        var msh = ParseSegment(segments[0]);
        var messageType = msh.Length > 8 ? msh[8] : ""; // MSH-9: ORU^R01

        if (!messageType.StartsWith("ORU", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("HL7 message type {Type} received; only ORU supported. MessageId={MessageId}", messageType, messageId);
            return new Hl7StreamResponse { MessageId = messageId };
        }

        PatientId? patientId = null;
        foreach (var seg in segments)
        {
            if (seg.StartsWith("PID|", StringComparison.Ordinal))
            {
                var pid = ParseSegment(seg);
                if (pid.Length > 3 && !string.IsNullOrWhiteSpace(pid[3]))
                    patientId = new PatientId(pid[3].Trim());
                break;
            }
        }

        if (patientId is null)
        {
            _logger.LogWarning("HL7 ORU missing PID segment or patient ID. MessageId={MessageId}", messageId);
            return new Hl7StreamResponse { MessageId = messageId };
        }

        var effective = ObservationEffective.UtcNow;
        foreach (var seg in segments)
        {
            if (!seg.StartsWith("OBX|", StringComparison.Ordinal))
                continue;

            var obx = ParseSegment(seg);
            if (obx.Length < 6)
                continue;

            var code = ExtractLoincFromObx3(obx[2]);
            var valueStr = obx.Length > 4 ? obx[4] : null;
            var unit = obx.Length > 5 ? obx[5].Trim() : null;

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(valueStr))
                continue;

            if (!decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
                continue;

            var loinc = new LoincCode(code);
            UnitOfMeasure? um = null;
            if (!string.IsNullOrWhiteSpace(unit))
                um = new UnitOfMeasure(unit, "http://unitsofmeasure.org");

            var display = $"{code} {valueStr} {unit}".Trim();

            var observation = Observation.Create(
                tenantId,
                patientId,
                loinc,
                display,
                um,
                numericValue,
                effective);

            await _observationRepository.AddAsync(observation, cancellationToken);

            foreach (var evt in observation.IntegrationEvents)
                await _publisher.PublishAsync(evt, cancellationToken);
            observation.ClearIntegrationEvents();
        }

        _logger.LogInformation("HL7 ORU processed. MessageId={MessageId}, PatientId={PatientId}", messageId, patientId.Value);
        return new Hl7StreamResponse { MessageId = messageId };
    }

    private static string[] ParseSegment(string segment) =>
        segment.Split('|');

    /// <summary>Extract LOINC code from OBX-3 (e.g. 85354-9^BP systolic^^^^LN).</summary>
    private static string? ExtractLoincFromObx3(string obx3)
    {
        var parts = obx3.Split('^');
        return parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0].Trim() : null;
    }
}
