using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.GetPatientChart;

public sealed record PatientChartItem
{
    public PatientChartItem(string Kind,
        Guid Id,
        DateTime RecordedAtUtc,
        string Code,
        string Display,
        string? Value,
        string? Status)
    {
        this.Kind = Kind;
        this.Id = Id;
        this.RecordedAtUtc = RecordedAtUtc;
        this.Code = Code;
        this.Display = Display;
        this.Value = Value;
        this.Status = Status;
    }
    public string Kind { get; init; }
    public Guid Id { get; init; }
    public DateTime RecordedAtUtc { get; init; }
    public string Code { get; init; }
    public string Display { get; init; }
    public string? Value { get; init; }
    public string? Status { get; init; }
    public void Deconstruct(out string Kind, out Guid Id, out DateTime RecordedAtUtc, out string Code, out string Display, out string? Value, out string? Status)
    {
        Kind = this.Kind;
        Id = this.Id;
        RecordedAtUtc = this.RecordedAtUtc;
        Code = this.Code;
        Display = this.Display;
        Value = this.Value;
        Status = this.Status;
    }
}

public sealed record PatientChartView
{
    public PatientChartView(Guid PatientId,
        IReadOnlyList<PatientChartItem> Allergies,
        IReadOnlyList<PatientChartItem> Problems,
        IReadOnlyList<PatientChartItem> Medications,
        IReadOnlyList<PatientChartItem> Vitals,
        IReadOnlyList<PatientChartItem> Immunizations)
    {
        this.PatientId = PatientId;
        this.Allergies = Allergies;
        this.Problems = Problems;
        this.Medications = Medications;
        this.Vitals = Vitals;
        this.Immunizations = Immunizations;
    }
    public Guid PatientId { get; init; }
    public IReadOnlyList<PatientChartItem> Allergies { get; init; }
    public IReadOnlyList<PatientChartItem> Problems { get; init; }
    public IReadOnlyList<PatientChartItem> Medications { get; init; }
    public IReadOnlyList<PatientChartItem> Vitals { get; init; }
    public IReadOnlyList<PatientChartItem> Immunizations { get; init; }
    public void Deconstruct(out Guid PatientId, out IReadOnlyList<PatientChartItem> Allergies, out IReadOnlyList<PatientChartItem> Problems, out IReadOnlyList<PatientChartItem> Medications, out IReadOnlyList<PatientChartItem> Vitals, out IReadOnlyList<PatientChartItem> Immunizations)
    {
        PatientId = this.PatientId;
        Allergies = this.Allergies;
        Problems = this.Problems;
        Medications = this.Medications;
        Vitals = this.Vitals;
        Immunizations = this.Immunizations;
    }
}

public sealed record GetPatientChartQuery : IQuery<PatientChartView>, IPermissionedCommand
{
    public GetPatientChartQuery(Guid PatientId) => this.PatientId = PatientId;
    public string RequiredPermission => EhrPermissions.ChartRead;
    public Guid PatientId { get; init; }
    public void Deconstruct(out Guid PatientId) => PatientId = this.PatientId;
}
