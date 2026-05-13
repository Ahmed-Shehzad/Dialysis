using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.CaptureCharge;

public sealed record CaptureChargeCommand(
    Guid PatientId,
    Guid EncounterId,
    string CptCode,
    IReadOnlyList<string> DiagnosisPointerIcd10Codes,
    decimal BilledAmount,
    string CurrencyCode)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ChargeCapture;
}
