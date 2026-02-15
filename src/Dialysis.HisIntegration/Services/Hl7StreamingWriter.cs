using Dialysis.ApiClients;
using Dialysis.HisIntegration.Features.AdtSync;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace Dialysis.HisIntegration.Services;

public interface IHl7StreamingWriter
{
    Task<Hl7StreamingResult> ConvertAndPersistAsync(string rawHl7, string? messageType, string? tenantId, CancellationToken cancellationToken = default);
}

public sealed record Hl7StreamingResult
{
    public bool Processed { get; init; }
    public string? PatientId { get; init; }
    public string? EncounterId { get; init; }
    public IReadOnlyList<string> ResourceIds { get; init; } = [];
    public string? Error { get; init; }
}

public sealed class AzureHl7StreamingWriter : IHl7StreamingWriter
{
    private readonly IAzureConvertDataClient _convertClient;
    private readonly IFhirApiFactory _fhirApiFactory;
    private readonly ITenantFhirResolver _resolver;
    private readonly AzureConvertDataOptions _options;

    public AzureHl7StreamingWriter(
        IAzureConvertDataClient convertClient,
        IFhirApiFactory fhirApiFactory,
        ITenantFhirResolver resolver,
        IOptions<AzureConvertDataOptions> options)
    {
        _convertClient = convertClient;
        _fhirApiFactory = fhirApiFactory;
        _resolver = resolver;
        _options = options.Value;
    }

    public async Task<Hl7StreamingResult> ConvertAndPersistAsync(string rawHl7, string? messageType, string? tenantId, CancellationToken cancellationToken = default)
    {
        var baseUrl = _resolver.GetBaseUrl(tenantId);
        if (string.IsNullOrEmpty(baseUrl))
            return new Hl7StreamingResult { Processed = false, Error = "No FHIR base URL for tenant" };

        var rootTemplate = InferRootTemplate(messageType);
        var bundle = await _convertClient.ConvertAsync(baseUrl, rawHl7, rootTemplate, cancellationToken);
        if (bundle is null)
            return new Hl7StreamingResult { Processed = false, Error = "Convert returned no bundle" };

        if (!_options.PersistAsTransaction)
            return new Hl7StreamingResult { Processed = true, ResourceIds = [] };

        bundle.Type = Bundle.BundleType.Transaction;
        var api = _fhirApiFactory.ForBaseUrl(baseUrl);
        var response = await api.PostTransaction(bundle, cancellationToken);
        response.EnsureSuccessStatusCode();

        var ids = ExtractResourceIdsFromBundle(bundle);
        var (patientId, encounterId) = ExtractPatientAndEncounterIds(bundle);

        return new Hl7StreamingResult
        {
            Processed = true,
            PatientId = patientId,
            EncounterId = encounterId,
            ResourceIds = ids
        };
    }

    private static string InferRootTemplate(string? messageType)
    {
        if (string.IsNullOrEmpty(messageType)) return "ADT_A01";
        var upper = messageType.ToUpperInvariant();
        if (upper.Contains("A01")) return "ADT_A01";
        if (upper.Contains("A02") || upper.Contains("A03")) return "ADT_A03";
        if (upper.Contains("A04")) return "ADT_A04";
        if (upper.Contains("A08")) return "ADT_A08";
        if (upper.Contains("ORU") || upper.Contains("R01")) return "ORU_R01";
        if (upper.Contains("ORM") || upper.Contains("O01")) return "ORM_O01";
        return "ADT_A01";
    }

    private static IReadOnlyList<string> ExtractResourceIdsFromBundle(Bundle bundle)
    {
        return bundle.Entry
            .Where(e => e.Resource?.Id != null)
            .Select(e => $"{e.Resource!.TypeName}/{e.Resource.Id}")
            .ToList();
    }

    private static (string? PatientId, string? EncounterId) ExtractPatientAndEncounterIds(Bundle bundle)
    {
        string? patientId = null;
        string? encounterId = null;
        foreach (var e in bundle.Entry)
        {
            if (e.Resource is Patient p)
                patientId = p.Id;
            else if (e.Resource is Encounter enc)
                encounterId = enc.Id;
        }
        return (patientId, encounterId);
    }
}
