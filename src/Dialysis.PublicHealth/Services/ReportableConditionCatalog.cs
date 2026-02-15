using Dialysis.PublicHealth.Models;

namespace Dialysis.PublicHealth.Services;

/// <summary>In-memory catalog of dialysis-related reportable conditions with jurisdiction-specific lists.</summary>
public sealed class ReportableConditionCatalog : IReportableConditionCatalog
{
    private static readonly IReadOnlyList<ReportableCondition> Conditions =
    [
        // US (federal)
        new ReportableCondition { Id = "1", Code = "B20", Display = "HIV disease", Category = "infectious", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "2", Code = "B18.1", Display = "Chronic viral hepatitis C", Category = "infectious", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "3", Code = "B18.0", Display = "Chronic viral hepatitis B", Category = "infectious", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "4", Code = "N18.6", Display = "End stage renal disease", Category = "ESRD", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "5", Code = "Z22", Display = "Carrier of infectious disease", Category = "infectious", Jurisdiction = "US", IsActive = true },
        // Germany (IfSG notifiable diseases – dialysis-relevant)
        new ReportableCondition { Id = "10", Code = "B20", Display = "HIV-Erkrankung", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "11", Code = "B18.1", Display = "Chronische Hepatitis C", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "12", Code = "B18.0", Display = "Chronische Hepatitis B", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "13", Code = "N18.6", Display = "Terminales Nierenversagen", Category = "ESRD", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "14", Code = "A41", Display = "Sepsis (blutstromassoziiert)", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        // UK (dialysis-relevant notifiable conditions)
        new ReportableCondition { Id = "20", Code = "B20", Display = "HIV disease", Category = "infectious", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "21", Code = "B18.1", Display = "Hepatitis C", Category = "infectious", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "22", Code = "B18.0", Display = "Hepatitis B", Category = "infectious", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "23", Code = "N18.6", Display = "End stage renal disease", Category = "ESRD", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "24", Code = "A41.9", Display = "Sepsis unspecified", Category = "infectious", Jurisdiction = "UK", IsActive = true },
    ];

    public Task<IReadOnlyList<ReportableCondition>> ListAsync(string? jurisdiction = null, CancellationToken cancellationToken = default)
    {
        var list = string.IsNullOrWhiteSpace(jurisdiction)
            ? Conditions.Where(c => c.IsActive).ToList()
            : Conditions.Where(c => c.IsActive && string.Equals(c.Jurisdiction, jurisdiction, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<ReportableCondition>>(list);
    }

    public Task<ReportableCondition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Conditions.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
}
