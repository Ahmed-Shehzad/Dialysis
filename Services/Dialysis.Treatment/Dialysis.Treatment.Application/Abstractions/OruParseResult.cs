using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Abstractions;

public sealed record OruParseResult(
    SessionId SessionId,
    MedicalRecordNumber? PatientMrn,
    DeviceId? DeviceId,
    EventPhase? Phase,
    string? SendingApplication,
    string? SendingFacility,
    DateTimeOffset? MessageTimestamp,
    IReadOnlyList<ObservationInfo> Observations,
    string? DeviceEui64 = null,
    string? TherapyId = null);
