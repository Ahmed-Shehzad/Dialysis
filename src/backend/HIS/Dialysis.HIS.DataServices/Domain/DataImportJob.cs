namespace Dialysis.HIS.DataServices.Domain;

public sealed class DataImportJob
{
    public Guid Id { get; set; }

    public string SourceDescription { get; set; } = string.Empty;

    public DateTime SubmittedAtUtc { get; set; }

    public string StatusCode { get; set; } = "Queued";

    /// <summary>Outcome of host-side validation / staging (e.g. accepted, rejected reason).</summary>
    public string? ValidationSummary { get; set; }
}
