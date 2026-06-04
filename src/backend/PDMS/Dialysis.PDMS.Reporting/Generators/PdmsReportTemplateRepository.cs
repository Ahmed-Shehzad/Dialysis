using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Reporting.Domain;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Concrete <see cref="IReportTemplateRepository"/> over the shared
/// <see cref="IPdmsRepository{TAggregate,TId}"/> registration (EF + Postgres in hosted
/// environments, in-memory in dev/tests). Resolution is delegated to
/// <see cref="ReportTemplateResolver"/> so the language-aware fallback lives in one place.
///
/// Template counts are small (one row per kind × language), so listing all and resolving in
/// memory is cheaper than a bespoke query and keeps the resolver provider-agnostic.
/// </summary>
public sealed class PdmsReportTemplateRepository : IReportTemplateRepository
{
    private readonly IPdmsRepository<ReportTemplate, Guid> _templates;
    /// <summary>
    /// Concrete <see cref="IReportTemplateRepository"/> over the shared
    /// <see cref="IPdmsRepository{TAggregate,TId}"/> registration (EF + Postgres in hosted
    /// environments, in-memory in dev/tests). Resolution is delegated to
    /// <see cref="ReportTemplateResolver"/> so the language-aware fallback lives in one place.
    ///
    /// Template counts are small (one row per kind × language), so listing all and resolving in
    /// memory is cheaper than a bespoke query and keeps the resolver provider-agnostic.
    /// </summary>
    public PdmsReportTemplateRepository(IPdmsRepository<ReportTemplate, Guid> templates) => _templates = templates;
    public async Task<ReportTemplate?> FindActiveAsync(
        ReportKind kind,
        string? preferredLanguageCode,
        CancellationToken cancellationToken)
    {
        var all = await _templates.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return ReportTemplateResolver.Resolve(all, kind, preferredLanguageCode);
    }
}
