using Microsoft.Extensions.Hosting;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Drains the <see cref="ExportJobQueue"/> and hands each job id to <see cref="ExportJobRunner"/>.
/// Job execution (streaming, de-identification, NDJSON writing, failure marking) lives in the runner;
/// this service is just the in-process pump.
/// </summary>
internal sealed class ExportJobBackgroundProcessor : BackgroundService
{
    private readonly ExportJobQueue _queue;
    private readonly ExportJobRunner _runner;

    public ExportJobBackgroundProcessor(ExportJobQueue queue, ExportJobRunner runner)
    {
        _queue = queue;
        _runner = runner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in _queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await _runner.RunAsync(jobId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
