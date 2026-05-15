using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.GetPatientChart;

public sealed record PatientChartItem(
    string Kind,
    Guid Id,
    DateTime RecordedAtUtc,
    string Code,
    string Display,
    string? Value,
    string? Status);

public sealed record PatientChartView(
    Guid PatientId,
    IReadOnlyList<PatientChartItem> Allergies,
    IReadOnlyList<PatientChartItem> Problems,
    IReadOnlyList<PatientChartItem> Medications,
    IReadOnlyList<PatientChartItem> Vitals,
    IReadOnlyList<PatientChartItem> Immunizations);

public sealed record GetPatientChartQuery(Guid PatientId)
    : IQuery<PatientChartView>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.ChartRead;
}
