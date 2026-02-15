using System.Text.Json;
using Dialysis.PublicHealth.Configuration;
using Dialysis.PublicHealth.Models;

namespace Dialysis.PublicHealth.Services;

/// <summary>Loads reportable conditions from JSON config. Falls back to built-in list when config not found.</summary>
public sealed class ConfigurableReportableConditionCatalog : IReportableConditionCatalog
{
    private readonly IReadOnlyList<ReportableCondition> _conditions;

    public ConfigurableReportableConditionCatalog(string? configPath)
    {
        _conditions = LoadFromConfig(configPath);
    }

    private static IReadOnlyList<ReportableCondition> LoadFromConfig(string? configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
            return BuiltInConditions;

        var path = configPath;
        if (!Path.IsPathRooted(path))
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
        if (!File.Exists(path))
            return BuiltInConditions;

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<ReportableConditionsConfig>(json);
            if (config?.Conditions is not { Count: > 0 })
                return BuiltInConditions;

            var list = new List<ReportableCondition>();
            var idx = 0;
            foreach (var c in config.Conditions)
            {
                if (string.IsNullOrWhiteSpace(c.Code) || !c.Reportable) continue;
                var jurisdictions = c.Jurisdictions ?? [];
                var category = c.Category ?? "infectious";

                if (jurisdictions.Count == 0)
                {
                    list.Add(new ReportableCondition
                    {
                        Id = $"{++idx}",
                        Code = c.Code,
                        Display = c.Display ?? c.Code,
                        Category = category,
                        Jurisdiction = null,
                        IsActive = true
                    });
                }
                else
                {
                    foreach (var j in jurisdictions)
                    {
                        list.Add(new ReportableCondition
                        {
                            Id = $"{++idx}",
                            Code = c.Code,
                            Display = c.Display ?? c.Code,
                            Category = category,
                            Jurisdiction = j,
                            IsActive = true
                        });
                    }
                }
            }
            return list;
        }
        catch
        {
            return BuiltInConditions;
        }
    }

    private static IReadOnlyList<ReportableCondition> BuiltInConditions { get; } =
    [
        new ReportableCondition { Id = "1", Code = "B20", Display = "HIV disease", Category = "infectious", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "2", Code = "B18.1", Display = "Chronic viral hepatitis C", Category = "infectious", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "3", Code = "B18.0", Display = "Chronic viral hepatitis B", Category = "infectious", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "4", Code = "N18.6", Display = "End stage renal disease", Category = "ESRD", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "5", Code = "Z22", Display = "Carrier of infectious disease", Category = "infectious", Jurisdiction = "US", IsActive = true },
        new ReportableCondition { Id = "10", Code = "B20", Display = "HIV-Erkrankung", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "11", Code = "B18.1", Display = "Chronische Hepatitis C", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "12", Code = "B18.0", Display = "Chronische Hepatitis B", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "13", Code = "N18.6", Display = "Terminales Nierenversagen", Category = "ESRD", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "14", Code = "A41", Display = "Sepsis", Category = "infectious", Jurisdiction = "DE", IsActive = true },
        new ReportableCondition { Id = "20", Code = "B20", Display = "HIV disease", Category = "infectious", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "21", Code = "B18.1", Display = "Hepatitis C", Category = "infectious", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "22", Code = "B18.0", Display = "Hepatitis B", Category = "infectious", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "23", Code = "N18.6", Display = "End stage renal disease", Category = "ESRD", Jurisdiction = "UK", IsActive = true },
        new ReportableCondition { Id = "24", Code = "A41.9", Display = "Sepsis unspecified", Category = "infectious", Jurisdiction = "UK", IsActive = true },
    ];

    public Task<IReadOnlyList<ReportableCondition>> ListAsync(string? jurisdiction = null, CancellationToken cancellationToken = default)
    {
        var list = string.IsNullOrWhiteSpace(jurisdiction)
            ? _conditions.Where(c => c.IsActive).ToList()
            : _conditions.Where(c => c.IsActive && string.Equals(c.Jurisdiction, jurisdiction, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult<IReadOnlyList<ReportableCondition>>(list);
    }

    public Task<ReportableCondition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_conditions.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));
}
