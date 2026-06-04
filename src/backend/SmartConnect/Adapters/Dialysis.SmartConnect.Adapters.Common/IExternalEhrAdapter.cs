using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Adapters;

public sealed record ExternalEhrContext
{
    public ExternalEhrContext(string TenantId, string? PatientLaunchContext, IReadOnlyDictionary<string, string>? Headers = null)
    {
        this.TenantId = TenantId;
        this.PatientLaunchContext = PatientLaunchContext;
        this.Headers = Headers;
    }
    public string TenantId { get; init; }
    public string? PatientLaunchContext { get; init; }
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
    public void Deconstruct(out string TenantId, out string? PatientLaunchContext, out IReadOnlyDictionary<string, string>? Headers)
    {
        TenantId = this.TenantId;
        PatientLaunchContext = this.PatientLaunchContext;
        Headers = this.Headers;
    }
}

public sealed record ExternalEhrAdapterDescriptor
{
    public ExternalEhrAdapterDescriptor(string VendorName, string FhirVersion, string BaseUrl)
    {
        this.VendorName = VendorName;
        this.FhirVersion = FhirVersion;
        this.BaseUrl = BaseUrl;
    }
    public string VendorName { get; init; }
    public string FhirVersion { get; init; }
    public string BaseUrl { get; init; }
    public void Deconstruct(out string VendorName, out string FhirVersion, out string BaseUrl)
    {
        VendorName = this.VendorName;
        FhirVersion = this.FhirVersion;
        BaseUrl = this.BaseUrl;
    }
}

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
