using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Billing.Features.CaptureCharge;

public sealed record CaptureChargeCommand : ICommand<Guid>, IPermissionedCommand
{
    public CaptureChargeCommand(Guid PatientId,
        Guid EncounterId,
        string CptCode,
        IReadOnlyList<string> DiagnosisPointerIcd10Codes,
        decimal BilledAmount,
        string CurrencyCode)
    {
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.CptCode = CptCode;
        this.DiagnosisPointerIcd10Codes = DiagnosisPointerIcd10Codes;
        this.BilledAmount = BilledAmount;
        this.CurrencyCode = CurrencyCode;
    }
    public string RequiredPermission => EhrPermissions.ChargeCapture;
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public string CptCode { get; init; }
    public IReadOnlyList<string> DiagnosisPointerIcd10Codes { get; init; }
    public decimal BilledAmount { get; init; }
    public string CurrencyCode { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid EncounterId, out string CptCode, out IReadOnlyList<string> DiagnosisPointerIcd10Codes, out decimal BilledAmount, out string CurrencyCode)
    {
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        CptCode = this.CptCode;
        DiagnosisPointerIcd10Codes = this.DiagnosisPointerIcd10Codes;
        BilledAmount = this.BilledAmount;
        CurrencyCode = this.CurrencyCode;
    }
}
