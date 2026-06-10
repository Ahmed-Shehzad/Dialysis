using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Inbound.Insights.Features;

/// <summary>
/// Builds the cross-source "Community Health Record" summary for a patient from the resources HIE
/// has received from outside organisations.
/// </summary>
public sealed record GetPatientInsightsQuery : IQuery<PatientInsightsSummary>, IPermissionedCommand
{
    /// <summary>
    /// Builds the cross-source "Community Health Record" summary for a patient.
    /// </summary>
    public GetPatientInsightsQuery(string PatientReference, int Scan = 500, int RecentTake = 20)
    {
        this.PatientReference = PatientReference;
        this.Scan = Scan;
        this.RecentTake = RecentTake;
    }

    public string RequiredPermission => HiePermissions.InboundReceive;

    /// <summary>The patient id as referenced by the received resources (the external subject id).</summary>
    public string PatientReference { get; init; }

    /// <summary>How many recent inbound rows to scan for the patient.</summary>
    public int Scan { get; init; }

    /// <summary>How many items to include in the recent-activity strip.</summary>
    public int RecentTake { get; init; }

    public void Deconstruct(out string patientReference, out int scan, out int recentTake)
    {
        patientReference = PatientReference;
        scan = Scan;
        recentTake = RecentTake;
    }
}
