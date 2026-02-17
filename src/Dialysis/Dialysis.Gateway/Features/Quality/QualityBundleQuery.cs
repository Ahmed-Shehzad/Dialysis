using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Quality;

public sealed record QualityBundleQuery(string BaseUrl, DateTime From, DateTime To, int Limit) : IQuery<QualityBundleQueryResult>;

public sealed record QualityBundleQueryResult(string FhirBundleJson);
