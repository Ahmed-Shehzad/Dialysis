using Dialysis.BuildingBlocks.Fhir.DeIdentification;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Runs a single export job: streams each requested resource type through its feeder, optionally
/// de-identifies each resource, and writes one NDJSON file per type. Extracted from the background
/// processor (which is now a thin queue loop) so the job-execution path — including the fail-closed
/// de-identification gate — is directly unit-testable.
/// </summary>
public sealed class ExportJobRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NdjsonFeederBinder _binder;
    private readonly TimeProvider _time;
    private readonly ILogger<ExportJobRunner> _logger;

    /// <summary>Creates the runner with the scope factory, feeder binder, clock, and logger.</summary>
    public ExportJobRunner(
        IServiceScopeFactory scopeFactory,
        NdjsonFeederBinder binder,
        TimeProvider time,
        ILogger<ExportJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _binder = binder;
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// Executes the job. On any failure (including a de-identification request that cannot be honoured)
    /// the job is transitioned to <see cref="ExportJobStatus.Failed"/> with the error — never left
    /// half-written. Cancellation on host shutdown is rethrown so the processor loop can stop cleanly.
    /// </summary>
    public async Task RunAsync(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            await RunCoreAsync(jobId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export job {JobId} failed", jobId);
            await MarkFailedAsync(jobId, ex.Message).ConfigureAwait(false);
        }
    }

    private async Task RunCoreAsync(string jobId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<IExportJobStore>();
        var storage = services.GetRequiredService<IBulkDataStorage>();

        var job = await store.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job is null || job.Status == ExportJobStatus.Cancelled)
        {
            return;
        }

        // Resolve the de-identification transform up front — before any byte is written — so a job
        // that asks for de-identification we can't honour fails closed (no identified PHI is streamed).
        var transform = ResolveDeIdentification(job, services);

        var inProgress = job with { Status = ExportJobStatus.InProgress };
        await store.UpdateAsync(inProgress, cancellationToken).ConfigureAwait(false);

        var types = job.ResourceTypes.Count == 0
            ? [.. _binder.SupportedResourceTypes]
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

            if (!_binder.TryGet(resourceType, out var binding))
            {
                continue;
            }

            var count = await WriteNdjsonAsync(services, storage, binding, inProgress, transform, cancellationToken).ConfigureAwait(false);
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
            CompletedAt = _time.GetUtcNow(),
            Outputs = outputs,
        };
        await store.UpdateAsync(completed, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the per-resource de-identification transform for the job, or <see langword="null"/> when
    /// none was requested. Throws when a profile is requested but no <see cref="IFhirDeIdentifier"/> is
    /// registered, or the profile name is unrecognised — both surface as a failed job (fail-closed).
    /// </summary>
    private static Func<Resource, Resource>? ResolveDeIdentification(ExportJob job, IServiceProvider services)
    {
        var profile = ExportDeIdentification.ResolveProfile(job.DeIdentificationProfile);
        if (profile is null)
        {
            return null;
        }

        var deIdentifier = services.GetService<IFhirDeIdentifier>()
            ?? throw new InvalidOperationException(
                $"Export job requested de-identification profile '{job.DeIdentificationProfile}' but no IFhirDeIdentifier is registered. " +
                "Register one (services.AddFhirDeIdentification()) or omit the _deIdentify parameter.");

        return resource => deIdentifier.Apply(resource, profile.Value);
    }

    private static async Task<long> WriteNdjsonAsync(
        IServiceProvider services,
        IBulkDataStorage storage,
        INdjsonFeederBinding binding,
        ExportJob job,
        Func<Resource, Resource>? transform,
        CancellationToken cancellationToken)
    {
        await using var stream = await storage.OpenWriteAsync(job.Id, binding.ResourceType, cancellationToken).ConfigureAwait(false);
        await using var writer = new StreamWriter(stream);
        long count = 0;
        await foreach (var resource in binding.StreamAsync(services, job, cancellationToken).ConfigureAwait(false))
        {
            var emitted = transform is null ? resource : transform(resource);
            var json = FhirJsonSerializerProvider.Serialize(emitted);
            await writer.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            count++;
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return count;
    }

    private async Task MarkFailedAsync(string jobId, string error)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IExportJobStore>();
        var job = await store.GetAsync(jobId, CancellationToken.None).ConfigureAwait(false);
        if (job is null)
        {
            return;
        }
        var failed = job with
        {
            Status = ExportJobStatus.Failed,
            CompletedAt = _time.GetUtcNow(),
            Error = error,
        };
        await store.UpdateAsync(failed, CancellationToken.None).ConfigureAwait(false);
    }
}
