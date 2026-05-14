using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Testing;

public sealed class FakeFhirSearcher<TResource>(Func<FhirSearchRequest, Bundle>? respond = null) : IFhirSearcher<TResource>
    where TResource : Resource
{
    private readonly Func<FhirSearchRequest, Bundle> _respond = respond ?? (_ => new Bundle { Type = Bundle.BundleType.Searchset });

    public ValueTask<Bundle> SearchAsync(FhirSearchRequest request, CancellationToken cancellationToken)
        => new(_respond(request));
}
