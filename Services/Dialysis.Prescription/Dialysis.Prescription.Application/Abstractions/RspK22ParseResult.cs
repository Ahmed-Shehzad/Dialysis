using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain;

namespace Dialysis.Prescription.Application.Abstractions;

public sealed record RspK22ParseResult(
    string OrderId,
    MedicalRecordNumber PatientMrn,
    string? Modality,
    string? OrderingProvider,
    string? CallbackPhone,
    string? QueryTag,
    string? MsaAcknowledgmentCode,
    string? MsaControlId,
    string? QpdQueryName,
    IReadOnlyList<ProfileSetting> Settings);
