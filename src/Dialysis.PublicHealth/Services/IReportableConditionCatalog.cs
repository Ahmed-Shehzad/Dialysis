namespace Dialysis.PublicHealth.Services;

/// <summary>Catalog of reportable conditions (e.g. hepatitis, HIV, ESRD). Supports jurisdiction-specific filtering.</summary>
public interface IReportableConditionCatalog
{
    /// <summary>List conditions, optionally filtered by jurisdiction (e.g. US, DE, UK).</summary>
    Task<IReadOnlyList<Models.ReportableCondition>> ListAsync(string? jurisdiction = null, CancellationToken cancellationToken = default);

    Task<Models.ReportableCondition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
