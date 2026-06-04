using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Testing;

public sealed class FakeFhirSearcher<TResource> : IFhirSearcher<TResource>
    where TResource : Resource
{
    private readonly Func<FhirSearchRequest, Bundle> _respond;
    public FakeFhirSearcher(Func<FhirSearchRequest, Bundle>? respond = null) => _respond = respond ?? (_ => new Bundle { Type = Bundle.BundleType.Searchset });

    public ValueTask<Bundle> SearchAsync(FhirSearchRequest request, CancellationToken cancellationToken)
        => new(_respond(request));
}
