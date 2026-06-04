using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Integration.Features.IngestLabResult;

/// <summary>
/// Receives an inbound HL7v2-ORU / FHIR Observation from the lab and persists it as a LabResult,
/// then publishes <c>LabResultReceivedIntegrationEvent</c> so PatientChart can update.
/// </summary>
public sealed record IngestLabResultCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>
    /// Receives an inbound HL7v2-ORU / FHIR Observation from the lab and persists it as a LabResult,
    /// then publishes <c>LabResultReceivedIntegrationEvent</c> so PatientChart can update.
    /// </summary>
    public IngestLabResultCommand(string LabFacilityCode,
        string ExternalControlNumber,
        Guid LabOrderId,
        Guid PatientId,
        string LoincCode,
        string ValueText,
        string? UnitCode,
        string? ReferenceRangeText,
        string AbnormalFlagCode,
        DateTime ObservedAtUtc)
    {
        this.LabFacilityCode = LabFacilityCode;
        this.ExternalControlNumber = ExternalControlNumber;
        this.LabOrderId = LabOrderId;
        this.PatientId = PatientId;
        this.LoincCode = LoincCode;
        this.ValueText = ValueText;
        this.UnitCode = UnitCode;
        this.ReferenceRangeText = ReferenceRangeText;
        this.AbnormalFlagCode = AbnormalFlagCode;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public string RequiredPermission => EhrPermissions.IntegrationInboundIngest;
    public string LabFacilityCode { get; init; }
    public string ExternalControlNumber { get; init; }
    public Guid LabOrderId { get; init; }
    public Guid PatientId { get; init; }
    public string LoincCode { get; init; }
    public string ValueText { get; init; }
    public string? UnitCode { get; init; }
    public string? ReferenceRangeText { get; init; }
    public string AbnormalFlagCode { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public void Deconstruct(out string LabFacilityCode, out string ExternalControlNumber, out Guid LabOrderId, out Guid PatientId, out string LoincCode, out string ValueText, out string? UnitCode, out string? ReferenceRangeText, out string AbnormalFlagCode, out DateTime ObservedAtUtc)
    {
        LabFacilityCode = this.LabFacilityCode;
        ExternalControlNumber = this.ExternalControlNumber;
        LabOrderId = this.LabOrderId;
        PatientId = this.PatientId;
        LoincCode = this.LoincCode;
        ValueText = this.ValueText;
        UnitCode = this.UnitCode;
        ReferenceRangeText = this.ReferenceRangeText;
        AbnormalFlagCode = this.AbnormalFlagCode;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}
