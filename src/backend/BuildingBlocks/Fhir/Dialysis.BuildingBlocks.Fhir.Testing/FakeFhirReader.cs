using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Testing;

public sealed class FakeFhirReader<TResource> : IFhirReader<TResource>
    where TResource : Resource
{
    private readonly Func<string, TResource?> _lookup;
    public FakeFhirReader(Func<string, TResource?>? lookup = null) => _lookup = lookup ?? (_ => null);

    public ValueTask<FhirReadResult<TResource>> ReadAsync(string id, CancellationToken cancellationToken)
        => new(new FhirReadResult<TResource>(_lookup(id)));
}
