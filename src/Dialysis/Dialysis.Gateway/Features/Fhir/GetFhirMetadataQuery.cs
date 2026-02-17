using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir;

public sealed record GetFhirMetadataQuery(string BaseUrl) : IQuery<GetFhirMetadataResult>;

public sealed record GetFhirMetadataResult(string Json, string ContentType);
