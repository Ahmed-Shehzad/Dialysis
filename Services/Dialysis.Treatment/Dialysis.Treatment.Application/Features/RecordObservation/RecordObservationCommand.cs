using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.RecordObservation;

public sealed record RecordObservationCommand(
    SessionId SessionId,
    MedicalRecordNumber? PatientMrn,
    DeviceId? DeviceId,
    EventPhase? Phase,
    IReadOnlyList<ObservationInfo> Observations,
    string? DeviceEui64 = null,
    string? TherapyId = null,
    double? MessageTimeDriftSeconds = null) : ICommand<RecordObservationResponse>;
