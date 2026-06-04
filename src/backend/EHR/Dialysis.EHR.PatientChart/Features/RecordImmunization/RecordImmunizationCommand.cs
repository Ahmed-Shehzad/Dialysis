using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordImmunization;

public sealed record RecordImmunizationCommand : ICommand<Guid>, IPermissionedCommand
{
    public RecordImmunizationCommand(Guid PatientId,
        string CvxCode,
        string? CvxDisplay,
        DateOnly AdministeredOn,
        string? LotNumber,
        string? Manufacturer,
        string? SiteCode,
        Guid? AdministeringProviderId)
    {
        this.PatientId = PatientId;
        this.CvxCode = CvxCode;
        this.CvxDisplay = CvxDisplay;
        this.AdministeredOn = AdministeredOn;
        this.LotNumber = LotNumber;
        this.Manufacturer = Manufacturer;
        this.SiteCode = SiteCode;
        this.AdministeringProviderId = AdministeringProviderId;
    }
    public string RequiredPermission => EhrPermissions.ImmunizationRecord;
    public Guid PatientId { get; init; }
    public string CvxCode { get; init; }
    public string? CvxDisplay { get; init; }
    public DateOnly AdministeredOn { get; init; }
    public string? LotNumber { get; init; }
    public string? Manufacturer { get; init; }
    public string? SiteCode { get; init; }
    public Guid? AdministeringProviderId { get; init; }
    public void Deconstruct(out Guid PatientId, out string CvxCode, out string? CvxDisplay, out DateOnly AdministeredOn, out string? LotNumber, out string? Manufacturer, out string? SiteCode, out Guid? AdministeringProviderId)
    {
        PatientId = this.PatientId;
        CvxCode = this.CvxCode;
        CvxDisplay = this.CvxDisplay;
        AdministeredOn = this.AdministeredOn;
        LotNumber = this.LotNumber;
        Manufacturer = this.Manufacturer;
        SiteCode = this.SiteCode;
        AdministeringProviderId = this.AdministeringProviderId;
    }
}
