using Refit;

namespace Dialysis.DataProducerSimulator;

/// <summary>
/// Refit API for Gateway HL7 and Treatment endpoints. Produces data for the entire backend.
/// </summary>
internal interface IGatewayApi
{
    [Post("/api/hl7/oru")]
    Task<HttpResponseMessage> PostOruAsync([Body] RawHl7Request request, CancellationToken ct = default);

    [Post("/api/hl7/oru/batch")]
    Task<HttpResponseMessage> PostOruBatchAsync([Body] RawHl7BatchRequest request, CancellationToken ct = default);

    [Post("/api/hl7/alarm")]
    Task<HttpResponseMessage> PostAlarmAsync([Body] RawHl7Request request, CancellationToken ct = default);

    [Post("/api/hl7/qbp-q22")]
    Task<HttpResponseMessage> PostQbpQ22Async([Body] RawHl7Request request, CancellationToken ct = default);

    [Post("/api/hl7/qbp-d01")]
    Task<HttpResponseMessage> PostQbpD01Async([Body] RawHl7Request request, CancellationToken ct = default);

    [Post("/api/hl7/rsp-k22")]
    Task<HttpResponseMessage> PostRspK22PatientAsync([Body] RawHl7Request request, CancellationToken ct = default);

    [Post("/api/prescriptions/hl7/rsp-k22")]
    Task<HttpResponseMessage> PostRspK22PrescriptionAsync([Body] RawHl7Request request, CancellationToken ct = default);

    [Get("/api/treatment-sessions/fhir")]
    [Headers("Accept: application/fhir+json")]
    Task<FhirBundle> GetTreatmentSessionsFhirAsync(
        [Query] int limit = 50,
        [Query] string? dateFrom = null,
        [Query] string? dateTo = null,
        CancellationToken ct = default);
}

internal sealed record RawHl7Request(string RawHl7Message);

internal sealed record RawHl7BatchRequest(string RawHl7Batch);

internal sealed record FhirBundle(IReadOnlyList<FhirEntry>? Entry);

internal sealed record FhirEntry(FhirResource? Resource);

internal sealed record FhirResource(string? ResourceType, string? Id);
