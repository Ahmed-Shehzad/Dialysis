using System.Text.Json;

using Refit;

namespace Dialysis.DataProducerSimulator;

/// <summary>
/// Refit-based client for Gateway API. Produces data for Patient, Prescription, Treatment, and Alarm services.
/// </summary>
internal sealed class GatewayApiClient : IDisposable
{
    private readonly IGatewayApi _api;
    private readonly HttpClient _http;

    public GatewayApiClient(string gatewayBaseUrl, string tenantId)
    {
        string baseUrl = gatewayBaseUrl.TrimEnd('/') + "/";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");

        _api = RestService.For<IGatewayApi>(_http, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        });
    }

    public async Task<bool> PostOruAsync(string rawHl7Message, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _api.PostOruAsync(new RawHl7Request(rawHl7Message), ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostOruBatchAsync(string rawHl7Batch, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _api.PostOruBatchAsync(new RawHl7BatchRequest(rawHl7Batch), ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostAlarmAsync(string rawHl7Message, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _api.PostAlarmAsync(new RawHl7Request(rawHl7Message), ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostQbpQ22Async(string rawHl7Message, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _api.PostQbpQ22Async(new RawHl7Request(rawHl7Message), ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostQbpD01Async(string rawHl7Message, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _api.PostQbpD01Async(new RawHl7Request(rawHl7Message), ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostRspK22PatientAsync(string rawHl7Message, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _api.PostRspK22PatientAsync(new RawHl7Request(rawHl7Message), ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PostRspK22PrescriptionAsync(string rawHl7Message, CancellationToken ct = default)
    {
        HttpResponseMessage response = await _api.PostRspK22PrescriptionAsync(new RawHl7Request(rawHl7Message), ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<string>> GetTreatmentSessionIdsAsync(int limit = 50, CancellationToken ct = default)
    {
        try
        {
            var from = DateTimeOffset.UtcNow.AddDays(-7);
            var to = DateTimeOffset.UtcNow;
            FhirBundle bundle = await _api.GetTreatmentSessionsFhirAsync(
                limit,
                from.ToString("o"),
                to.ToString("o"),
                ct).ConfigureAwait(false);

            var ids = new List<string>();
            foreach (var e in bundle?.Entry ?? [])
            {
                var res = e?.Resource;
                if (res?.ResourceType == "Procedure" && res.Id?.StartsWith("proc-", StringComparison.Ordinal) == true)
                    ids.Add(res.Id["proc-".Length..]);
            }
            return ids;
        }
        catch
        {
            return [];
        }
    }

    public void Dispose() => _http.Dispose();
}
