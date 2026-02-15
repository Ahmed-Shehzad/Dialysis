using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using FhirCore.Packages;

namespace FhirCore.Gateway.Validation;

public sealed class IgResourceResolver : IAsyncResourceResolver
{
    private readonly IgLoader _igLoader;

    public IgResourceResolver(IgLoader igLoader)
    {
        _igLoader = igLoader;
    }

    public System.Threading.Tasks.Task<Resource?> ResolveByUriAsync(string uri)
    {
        return ResolveByCanonicalUriAsync(uri);
    }

    public System.Threading.Tasks.Task<Resource?> ResolveByCanonicalUriAsync(string uri)
    {
        var index = _igLoader.Index;
        var def = index.ResolveByCanonical(uri) ?? index.ResolveByUrl(uri);
        return System.Threading.Tasks.Task.FromResult<Resource?>(def);
    }
}
