using Dialysis.CQRS.Commands;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Outbound.CareSummary;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Outbound.Features.GenerateCareSummary;

/// <summary>
/// Assembles a Continuity of Care Document (CCD) for the patient from the FHIR resources HIE has
/// already mapped, and queues it for Directed Exchange. Purpose-gated against patient consent.
/// </summary>
public sealed record GenerateCareSummaryCommand : ICommand<CareSummaryResult>, IPermissionedCommand
{
    /// <summary>
    /// Assembles a Continuity of Care Document (CCD) for the patient from the FHIR resources HIE has
    /// already mapped, and queues it for Directed Exchange. Purpose-gated against patient consent.
    /// </summary>
    public GenerateCareSummaryCommand(Guid PatientId, string? Purpose = null)
    {
        this.PatientId = PatientId;
        this.Purpose = Purpose;
    }

    public string RequiredPermission => HiePermissions.OutboundPublish;
    public Guid PatientId { get; init; }

    /// <summary>Optional TEFCA permitted purpose for the disclosure; defaults to Treatment.</summary>
    public string? Purpose { get; init; }

    public void Deconstruct(out Guid patientId, out string? purpose)
    {
        patientId = this.PatientId;
        purpose = this.Purpose;
    }
}
