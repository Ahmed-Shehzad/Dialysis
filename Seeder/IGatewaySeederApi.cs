using Refit;

namespace Dialysis.Seeder;

internal interface IGatewaySeederApi
{
    [Post("/api/prescriptions/hl7/rsp-k22")]
    Task<HttpResponseMessage> PostRspK22Async(
        [Body] IngestRspK22Request request,
        [Header("X-Tenant-Id")] string tenantId,
        CancellationToken cancellationToken = default);

    [Post("/api/hl7/oru/batch")]
    Task<HttpResponseMessage> PostOruBatchAsync(
        [Body] IngestOruBatchRequest request,
        [Header("X-Tenant-Id")] string tenantId,
        CancellationToken cancellationToken = default);

    [Post("/api/hl7/alarm")]
    Task<HttpResponseMessage> PostAlarmAsync(
        [Body] IngestAlarmRequest request,
        [Header("X-Tenant-Id")] string tenantId,
        CancellationToken cancellationToken = default);
}

internal sealed record IngestRspK22Request(string RawHl7Message, object? ValidationContext = null);

internal sealed record IngestOruBatchRequest(string RawHl7Batch);

internal sealed record IngestAlarmRequest(string RawHl7Message);
