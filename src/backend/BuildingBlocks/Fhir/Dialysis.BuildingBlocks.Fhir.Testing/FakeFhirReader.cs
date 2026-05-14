using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Testing;

public sealed class FakeFhirReader<TResource>(Func<string, TResource?>? lookup = null) : IFhirReader<TResource>
    where TResource : Resource
{
    private readonly Func<string, TResource?> _lookup = lookup ?? (_ => null);

    public ValueTask<FhirReadResult<TResource>> ReadAsync(string id, CancellationToken cancellationToken)
        => new(new FhirReadResult<TResource>(_lookup(id)));
}
