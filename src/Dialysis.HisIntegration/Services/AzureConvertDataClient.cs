using Dialysis.ApiClients;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Refit;

namespace Dialysis.HisIntegration.Services;

public interface IAzureConvertDataClient
{
    Task<Bundle?> ConvertAsync(string fhirBaseUrl, string rawHl7, string rootTemplate, CancellationToken cancellationToken = default);
}

public sealed class AzureConvertDataClient : IAzureConvertDataClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AzureConvertDataOptions _options;

    public AzureConvertDataClient(IHttpClientFactory httpFactory, IOptions<AzureConvertDataOptions> options)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
    }

    public async Task<Bundle?> ConvertAsync(string fhirBaseUrl, string rawHl7, string rootTemplate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fhirBaseUrl) || string.IsNullOrWhiteSpace(rawHl7))
            return null;

        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(fhirBaseUrl.TrimEnd('/') + "/");
        var api = RestService.For<IAzureConvertDataApi>(client);

        var body = new ConvertDataRequest(
            "Parameters",
            [
                new ConvertDataParameter("inputData", rawHl7, null),
                new ConvertDataParameter("inputDataType", "Hl7v2", null),
                new ConvertDataParameter("templateCollectionReference", _options.TemplateCollectionReference, null),
                new ConvertDataParameter("rootTemplate", rootTemplate, null)
            ]);

        return await api.ConvertAsync(body, cancellationToken);
    }
}

public sealed class AzureConvertDataOptions
{
    public const string SectionName = "AzureConvertData";

    public string? FhirServiceUrl { get; set; }
    public string TemplateCollectionReference { get; set; } = "microsofthealth/fhirconverter:default";
    public bool PersistAsTransaction { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(FhirServiceUrl);
}
