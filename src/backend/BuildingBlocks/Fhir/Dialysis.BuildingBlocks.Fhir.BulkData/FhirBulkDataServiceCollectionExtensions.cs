using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

public static class FhirBulkDataServiceCollectionExtensions
{
    /// <summary>
    /// Registers the bulk-data engine: in-memory job store, local file storage,
    /// and the orchestrator interface (implementation supplied by host or replaced by EF/cloud variants).
    /// </summary>
    public static IServiceCollection AddFhirBulkData(this IServiceCollection services, string storageRoot)
    {
        services.TryAddSingleton<IExportJobStore, InMemoryExportJobStore>();
        services.TryAddSingleton<IBulkDataStorage>(new LocalFileBulkDataStorage(storageRoot));
        return services;
    }

    /// <summary>
    /// Maps <c>GET /fhir/$export</c>, <c>GET /fhir/Patient/$export</c>, <c>GET /fhir/Group/{id}/$export</c>,
    /// <c>GET /fhir/bulk-data/jobs/{id}</c>, <c>DELETE /fhir/bulk-data/jobs/{id}</c> per the IG.
    /// </summary>
    public static IEndpointRouteBuilder MapFhirBulkDataEndpoints(this IEndpointRouteBuilder endpoints, string baseUrl = "/fhir")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var prefix = baseUrl.TrimEnd('/');

        endpoints.MapGet(prefix + "/$export", (HttpContext ctx) => StartExportAsync(ctx, ExportScope.System, groupId: null));
        endpoints.MapGet(prefix + "/Patient/$export", (HttpContext ctx) => StartExportAsync(ctx, ExportScope.Patient, groupId: null));
        endpoints.MapGet(prefix + "/Group/{id}/$export", (HttpContext ctx, string id) => StartExportAsync(ctx, ExportScope.Group, groupId: id));
        endpoints.MapGet(prefix + "/bulk-data/jobs/{id}", PollJobAsync);
        endpoints.MapDelete(prefix + "/bulk-data/jobs/{id}", CancelJobAsync);

        return endpoints;
    }

    private static async Task StartExportAsync(HttpContext context, ExportScope scope, string? groupId)
    {
        var orchestrator = context.RequestServices.GetService<IExportJobOrchestrator>();
        if (orchestrator is null)
        {
            context.Response.StatusCode = StatusCodes.Status501NotImplemented;
            return;
        }

        var since = context.Request.Query.TryGetValue("_since", out var sv) && DateTimeOffset.TryParse(sv, out var sinceVal)
            ? sinceVal
            : (DateTimeOffset?)null;
        var resourceTypes = context.Request.Query.TryGetValue("_type", out var tv) && tv.Count > 0
            ? tv.ToString().Split(',')
            : Array.Empty<string>();
        var deid = context.Request.Query.TryGetValue("_deIdentify", out var dv) ? dv.ToString() : null;

        var job = await orchestrator.EnqueueAsync(
            scope,
            resourceTypes,
            since,
            groupId,
            requestorId: context.User.Identity?.Name,
            deIdentificationProfile: deid,
            context.RequestAborted).ConfigureAwait(false);

        context.Response.Headers.ContentLocation = $"/fhir/bulk-data/jobs/{job.Id}";
        context.Response.StatusCode = StatusCodes.Status202Accepted;
    }

    private static async Task PollJobAsync(HttpContext context, string id)
    {
        var store = context.RequestServices.GetRequiredService<IExportJobStore>();
        var storage = context.RequestServices.GetRequiredService<IBulkDataStorage>();
        var job = await store.GetAsync(id, context.RequestAborted).ConfigureAwait(false);
        if (job is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        if (job.Status is ExportJobStatus.Queued or ExportJobStatus.InProgress)
        {
            context.Response.Headers["X-Progress"] = job.Status.ToString();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return;
        }
        if (job.Status == ExportJobStatus.Failed)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }
        var manifest = new
        {
            transactionTime = job.CompletedAt ?? job.CreatedAt,
            request = $"/fhir/{(job.Scope == ExportScope.Patient ? "Patient/" : job.Scope == ExportScope.Group ? $"Group/{job.GroupId}/" : "")}$export",
            requiresAccessToken = true,
            output = job.Outputs.Select(o => new { type = o.ResourceType, url = storage.BuildOutputUrl(job.Id, o.ResourceType) }),
            error = Array.Empty<object>(),
        };
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(manifest, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task CancelJobAsync(HttpContext context, string id)
    {
        var orchestrator = context.RequestServices.GetService<IExportJobOrchestrator>();
        if (orchestrator is null)
        {
            context.Response.StatusCode = StatusCodes.Status501NotImplemented;
            return;
        }
        await orchestrator.CancelAsync(id, context.RequestAborted).ConfigureAwait(false);
        context.Response.StatusCode = StatusCodes.Status202Accepted;
    }
}
