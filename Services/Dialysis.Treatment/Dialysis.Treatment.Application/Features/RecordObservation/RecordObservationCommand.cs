using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.RecordObservation;

public sealed record RecordObservationCommand(
    SessionId SessionId,
    MedicalRecordNumber? PatientMrn,
    DeviceId? DeviceId,
    IReadOnlyList<ObservationInfo> Observations) : ICommand<RecordObservationResponse>;
