namespace Dialysis.Gateway.Services;

/// <summary>
/// Builds de-identified quality bundles for regulatory reporting. Single responsibility: quality bundle data.
/// </summary>
public interface IQualityBundleService
{
    Task<QualityBundleResult> GetDeidentifiedBundleAsync(string tenantId, DateTime from, DateTime to, int limit, CancellationToken cancellationToken = default);
}

public sealed record QualityBundleResult(IReadOnlyList<Domain.Aggregates.Session> SessionsInRange);
