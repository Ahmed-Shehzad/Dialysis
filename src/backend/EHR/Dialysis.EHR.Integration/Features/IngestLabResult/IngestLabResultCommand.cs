using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Integration.Features.IngestLabResult;

/// <summary>
/// Receives an inbound HL7v2-ORU / FHIR Observation from the lab and persists it as a LabResult,
/// then publishes <c>LabResultReceivedIntegrationEvent</c> so PatientChart can update.
/// </summary>
public sealed record IngestLabResultCommand(
    string LabFacilityCode,
    string ExternalControlNumber,
    Guid LabOrderId,
    Guid PatientId,
    string LoincCode,
    string ValueText,
    string? UnitCode,
    string? ReferenceRangeText,
    string AbnormalFlagCode,
    DateTime ObservedAtUtc)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.IntegrationInboundIngest;
}
