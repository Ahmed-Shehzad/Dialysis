using Dialysis.BuildingBlocks.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

internal sealed class ExportJobBackgroundProcessor(
    ExportJobQueue queue,
    IServiceScopeFactory scopeFactory,
    NdjsonFeederBinder binder,
    TimeProvider time,
    ILogger<ExportJobBackgroundProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await RunJobAsync(jobId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Export job {JobId} failed", jobId);
                await MarkFailedAsync(jobId, ex.Message).ConfigureAwait(false);
            }
        }
    }

    private async Task RunJobAsync(string jobId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<IExportJobStore>();
        var storage = services.GetRequiredService<IBulkDataStorage>();
        var serializer = services.GetRequiredService<FhirJsonSerializerProvider>();

        var job = await store.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job is null || job.Status == ExportJobStatus.Cancelled)
        {
            return;
        }

        var inProgress = job with { Status = ExportJobStatus.InProgress };
        await store.UpdateAsync(inProgress, cancellationToken).ConfigureAwait(false);

        var types = job.ResourceTypes.Count == 0
            ? [.. binder.SupportedResourceTypes]
            : job.ResourceTypes;

        var outputs = new List<ExportJobOutput>(types.Count);
        foreach (var resourceType in types)
        {
            // Honor mid-flight cancellation by re-reading job status before each resource type.
            var current = await store.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
            if (current is null || current.Status == ExportJobStatus.Cancelled)
            {
                return;
            }

            if (!binder.TryGet(resourceType, out var binding))
            {
                continue;
            }

            var count = await WriteNdjsonAsync(services, storage, serializer, binding, inProgress, cancellationToken).ConfigureAwait(false);
            outputs.Add(new ExportJobOutput(resourceType, storage.BuildOutputUrl(jobId, resourceType), count));
        }

        var latest = await store.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (latest is null || latest.Status == ExportJobStatus.Cancelled)
        {
            return;
        }

        var completed = latest with
        {
            Status = ExportJobStatus.Completed,
            CompletedAt = time.GetUtcNow(),
            Outputs = outputs,
        };
        await store.UpdateAsync(completed, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<long> WriteNdjsonAsync(
        IServiceProvider services,
        IBulkDataStorage storage,
        FhirJsonSerializerProvider serializer,
        INdjsonFeederBinding binding,
        ExportJob job,
        CancellationToken cancellationToken)
    {
        await using var stream = await storage.OpenWriteAsync(job.Id, binding.ResourceType, cancellationToken).ConfigureAwait(false);
        await using var writer = new StreamWriter(stream);
        long count = 0;
        await foreach (var resource in binding.StreamAsync(services, job, cancellationToken).ConfigureAwait(false))
        {
            var json = serializer.Serialize(resource);
            await writer.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            count++;
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    private async Task MarkFailedAsync(string jobId, string error)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportJobStore>();
        var job = await store.GetAsync(jobId, CancellationToken.None).ConfigureAwait(false);
        if (job is null)
        {
            return;
        }
        var failed = job with
        {
            Status = ExportJobStatus.Failed,
            CompletedAt = time.GetUtcNow(),
            Error = error,
        };
        await store.UpdateAsync(failed, CancellationToken.None).ConfigureAwait(false);
    }
}
