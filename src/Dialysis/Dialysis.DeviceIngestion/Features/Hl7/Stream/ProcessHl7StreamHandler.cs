using System.Globalization;

using Dialysis.Domain.Aggregates;
using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;
using Dialysis.Persistence.Entities;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Hl7.Stream;

/// <summary>
/// Parses HL7 v2 messages: ORU^R01 (vitals/lab), ADT^A04 (register), ADT^A08 (update patient).
/// Phase 4.1.3: Idempotency via MSH-10; failed messages stored in DLQ.
/// </summary>
public sealed class ProcessHl7StreamHandler : ICommandHandler<ProcessHl7StreamCommand, Hl7StreamResponse>
{
    private readonly DialysisDbContext _db;
    private readonly IObservationRepository _observationRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IProcessedHl7MessageStore _processedStore;
    private readonly IFailedHl7MessageStore _failedStore;
    private readonly IPublisher _publisher;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProcessHl7StreamHandler> _logger;

    public ProcessHl7StreamHandler(
        DialysisDbContext db,
        IObservationRepository observationRepository,
        IPatientRepository patientRepository,
        IProcessedHl7MessageStore processedStore,
        IFailedHl7MessageStore failedStore,
        IPublisher publisher,
        ITenantContext tenantContext,
        ILogger<ProcessHl7StreamHandler> logger)
    {
        _db = db;
        _observationRepository = observationRepository;
        _patientRepository = patientRepository;
        _processedStore = processedStore;
        _failedStore = failedStore;
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
        var messageControlId = msh.Length > 9 ? msh[9].Trim() : null; // MSH-10
        var messageType = msh.Length > 8 ? msh[8] : ""; // MSH-9: ORU^R01, ADT^A04, ADT^A08

        if (!string.IsNullOrWhiteSpace(messageControlId) && await _processedStore.ExistsAsync(tenantId, messageControlId, cancellationToken))
        {
            _logger.LogInformation("HL7 idempotent skip: MessageControlId={MessageControlId}", messageControlId);
            return new Hl7StreamResponse { MessageId = messageId };
        }

        try
        {
            return await ProcessMessageAsync(segments, tenantId, messageId, messageType, messageControlId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HL7 processing failed. MessageId={MessageId}", messageId);
            var failed = FailedHl7Message.Create(tenantId, request.RawMessage, ex.Message, messageControlId);
            await _failedStore.AddAsync(failed, cancellationToken);
            return new Hl7StreamResponse
            {
                MessageId = messageId,
                Failed = true,
                FailedMessageId = failed.Id.ToString(),
                Error = ex.Message,
                Status = "Failed"
            };
        }
    }

    private async Task<Hl7StreamResponse> ProcessMessageAsync(
        string[] segments,
        TenantId tenantId,
        string messageId,
        string messageType,
        string? messageControlId,
        CancellationToken cancellationToken)
    {
        if (messageType.StartsWith("ADT", StringComparison.OrdinalIgnoreCase))
        {
            var adtResult = await HandleAdtAsync(segments, tenantId, messageId, messageType, cancellationToken);
            if (!string.IsNullOrWhiteSpace(messageControlId))
                await _processedStore.AddAsync(ProcessedHl7Message.Create(tenantId, messageControlId), cancellationToken);
            return adtResult;
        }

        if (!messageType.StartsWith("ORU", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("HL7 message type {Type} received; only ORU and ADT supported. MessageId={MessageId}", messageType, messageId);
            return new Hl7StreamResponse { MessageId = messageId };
        }

        PatientId? patientId = null;
        foreach (var seg in segments)
        {
            if (seg.StartsWith("PID|", StringComparison.Ordinal))
            {
                var pid = ParseSegment(seg);
                if (pid.Length > 3 && !string.IsNullOrWhiteSpace(pid[3]))
                    patientId = new PatientId(pid[3].Trim().Split('^')[0]);
                break;
            }
        }

        if (patientId is null)
        {
            _logger.LogWarning("HL7 ORU missing PID segment or patient ID. MessageId={MessageId}", messageId);
            return new Hl7StreamResponse { MessageId = messageId };
        }

        await ProcessObrSegmentsAsync(segments, tenantId, patientId!, cancellationToken);

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

        if (!string.IsNullOrWhiteSpace(messageControlId))
            await _processedStore.AddAsync(ProcessedHl7Message.Create(tenantId, messageControlId), cancellationToken);

        _logger.LogInformation("HL7 ORU processed. MessageId={MessageId}, PatientId={PatientId}", messageId, patientId.Value);
        return new Hl7StreamResponse { MessageId = messageId };
    }

    private async Task<Hl7StreamResponse> HandleAdtAsync(
        string[] segments,
        TenantId tenantId,
        string messageId,
        string messageType,
        CancellationToken cancellationToken)
    {
        var isA04 = messageType.Contains("A04", StringComparison.OrdinalIgnoreCase);
        var isA08 = messageType.Contains("A08", StringComparison.OrdinalIgnoreCase);
        if (!isA04 && !isA08)
        {
            _logger.LogInformation("HL7 ADT {Type} received; only A04/A08 supported. MessageId={MessageId}", messageType, messageId);
            return new Hl7StreamResponse { MessageId = messageId };
        }

        var (patientId, familyName, givenNames, birthDate) = ParsePidFromSegments(segments);
        if (patientId is null)
        {
            _logger.LogWarning("HL7 ADT missing PID segment or patient ID. MessageId={MessageId}", messageId);
            return new Hl7StreamResponse { MessageId = messageId };
        }

        var existing = await _db.Patients
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.LogicalId == patientId, cancellationToken);
        if (existing is not null)
        {
            existing.Update(familyName, givenNames, birthDate);
            await _patientRepository.UpdateAsync(existing, cancellationToken);
            _logger.LogInformation("HL7 ADT patient updated. MessageId={MessageId}, PatientId={PatientId}", messageId, patientId.Value);
        }
        else
        {
            var patient = Patient.Create(tenantId, patientId, familyName, givenNames, birthDate);
            await _patientRepository.AddAsync(patient, cancellationToken);
            _logger.LogInformation("HL7 ADT patient created. MessageId={MessageId}, PatientId={PatientId}", messageId, patientId.Value);
        }

        return new Hl7StreamResponse { MessageId = messageId };
    }

    /// <summary>Process OBR segments for lab order status (Phase 1.2.3). OBR-2/3=order ids, OBR-4=service, OBR-25=status.</summary>
    private async Task ProcessObrSegmentsAsync(string[] segments, TenantId tenantId, PatientId patientId, CancellationToken cancellationToken)
    {
        foreach (var seg in segments)
        {
            if (!seg.StartsWith("OBR|", StringComparison.Ordinal))
                continue;

            var obr = ParseSegment(seg);
            if (obr.Length < 4)
                continue;

            var placer = obr.Length > 2 ? obr[2].Trim().Split('^')[0] : "";
            var filler = obr.Length > 3 ? obr[3].Trim() : "";
            var serviceId = obr.Length > 4 ? obr[4].Trim().Split('^')[0] : null;
            var status = obr.Length > 25 ? obr[25].Trim() : "IP";

            if (string.IsNullOrWhiteSpace(placer) && string.IsNullOrWhiteSpace(filler))
                continue;

            var existing = await _db.LabOrderStatus
                .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.PlacerOrderNumber == placer && l.FillerOrderNumber == filler, cancellationToken);
            if (existing is not null)
            {
                existing.Status = status;
                existing.LastUpdatedUtc = DateTime.UtcNow;
            }
            else
            {
                _db.LabOrderStatus.Add(LabOrderStatus.Create(tenantId, patientId, placer, filler, serviceId, status));
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Parse PID segment: PID-3 (id), PID-5 (name^family^given), PID-7 (DOB).</summary>
    private static (PatientId? PatientId, string? FamilyName, string? GivenNames, DateTime? BirthDate) ParsePidFromSegments(string[] segments)
    {
        foreach (var seg in segments)
        {
            if (!seg.StartsWith("PID|", StringComparison.Ordinal))
                continue;

            var pid = ParseSegment(seg);
            if (pid.Length < 4)
                return (null, null, null, null);

            var idStr = pid[3].Split('^')[0].Trim();
            if (string.IsNullOrWhiteSpace(idStr))
                return (null, null, null, null);

            var patientId = new PatientId(idStr);

            string? family = null, given = null;
            if (pid.Length > 5 && !string.IsNullOrWhiteSpace(pid[5]))
            {
                var nameParts = pid[5].Split('^');
                family = nameParts.Length > 0 ? nameParts[0].Trim() : null;
                given = nameParts.Length > 1 ? nameParts[1].Trim() : null;
            }

            DateTime? dob = null;
            if (pid.Length > 7 && !string.IsNullOrWhiteSpace(pid[7]) &&
                DateTime.TryParseExact(pid[7].Trim(), ["yyyyMMdd", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                dob = parsed;
            }

            return (patientId, family, given, dob);
        }

        return (null, null, null, null);
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
