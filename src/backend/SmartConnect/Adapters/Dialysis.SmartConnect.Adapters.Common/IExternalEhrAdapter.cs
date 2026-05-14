using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Adapters;

public sealed record ExternalEhrContext(string TenantId, string? PatientLaunchContext, IReadOnlyDictionary<string, string>? Headers = null);

public sealed record ExternalEhrAdapterDescriptor(string VendorName, string FhirVersion, string BaseUrl);

public interface IExternalEhrAdapter
{
    ExternalEhrAdapterDescriptor Describe();

    Task<TResource> ReadAsync<TResource>(string id, ExternalEhrContext context, CancellationToken cancellationToken)
        where TResource : Resource;

    Task<Bundle> SearchAsync(string resourceType, IDictionary<string, string> parameters, ExternalEhrContext context, CancellationToken cancellationToken);
}

public interface IExternalEhrAuthProvider
{
    string VendorName { get; }

    Task<string> AcquireAccessTokenAsync(ExternalEhrContext context, CancellationToken cancellationToken);
}
