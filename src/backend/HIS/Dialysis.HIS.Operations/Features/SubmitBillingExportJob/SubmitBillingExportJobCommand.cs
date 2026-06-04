using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.SubmitBillingExportJob;

public sealed record SubmitBillingExportJobCommand : ICommand<Guid>, IPermissionedCommand
{
    public SubmitBillingExportJobCommand(string PayerCode,
        DateOnly PeriodStart,
        DateOnly PeriodEnd,
        string? Notes = null)
    {
        this.PayerCode = PayerCode;
        this.PeriodStart = PeriodStart;
        this.PeriodEnd = PeriodEnd;
        this.Notes = Notes;
    }
    public string RequiredPermission => HisPermissions.DataReport;
    public string PayerCode { get; init; }
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out string PayerCode, out DateOnly PeriodStart, out DateOnly PeriodEnd, out string? Notes)
    {
        PayerCode = this.PayerCode;
        PeriodStart = this.PeriodStart;
        PeriodEnd = this.PeriodEnd;
        Notes = this.Notes;
    }
}
