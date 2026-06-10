using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.BulkData;

public sealed class DefaultExportJobOrchestratorTests : IAsyncLifetime
{
    private string _storageRoot = string.Empty;

    public Task InitializeAsync()
    {
        _storageRoot = Path.Combine(Path.GetTempPath(), "fhir-export-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storageRoot);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try
        { Directory.Delete(_storageRoot, recursive: true); }
        catch { /* best-effort */ }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Enqueue_Creates_Queued_Job_And_Background_Service_Completes_It()
    {
        using var host = BuildHost();
        await host.StartAsync();

        var orchestrator = host.Services.GetRequiredService<IExportJobOrchestrator>();
        var store = host.Services.GetRequiredService<IExportJobStore>();

        var job = await orchestrator.EnqueueAsync(
            ExportScope.System,
            resourceTypes: new[] { "Patient" },
            since: null,
            groupId: null,
            requestorId: "tester",
            deIdentificationProfile: null,
            CancellationToken.None);

        job.Status.ShouldBe(ExportJobStatus.Queued);

        var completed = await WaitForStatusAsync(store, job.Id, ExportJobStatus.Completed, TimeSpan.FromSeconds(5));
        completed.Status.ShouldBe(ExportJobStatus.Completed);
        completed.Outputs.Count.ShouldBe(1);
        completed.Outputs[0].ResourceType.ShouldBe("Patient");
        completed.Outputs[0].ResourceCount.ShouldBe(2);

        var ndjsonPath = Path.Combine(_storageRoot, job.Id, "Patient.ndjson");
        File.Exists(ndjsonPath).ShouldBeTrue();
        var lines = await File.ReadAllLinesAsync(ndjsonPath);
        lines.Length.ShouldBe(2);

        var parser = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));
        var first = parser.Deserialize<Patient>(lines[0]);
        first.Id.ShouldBe("p1");

        await host.StopAsync();
    }

    [Fact]
    public async Task Cancel_Transitions_To_Cancelled_When_Job_Not_Yet_Running()
    {
        using var host = BuildHost(includeHostedService: false);
        await host.StartAsync();

        var orchestrator = host.Services.GetRequiredService<IExportJobOrchestrator>();
        var store = host.Services.GetRequiredService<IExportJobStore>();

        var job = await orchestrator.EnqueueAsync(
            ExportScope.System,
            resourceTypes: [],
            since: null, groupId: null, requestorId: null, deIdentificationProfile: null,
            CancellationToken.None);

        await orchestrator.CancelAsync(job.Id, CancellationToken.None);

        var after = await store.GetAsync(job.Id, CancellationToken.None);
        after.ShouldNotBeNull();
        after!.Status.ShouldBe(ExportJobStatus.Cancelled);

        await host.StopAsync();
    }

    [Fact]
    public async Task Cancel_Is_NoOp_When_Job_Already_Completed()
    {
        using var host = BuildHost(includeHostedService: false);
        await host.StartAsync();

        var store = host.Services.GetRequiredService<IExportJobStore>();
        var time = host.Services.GetRequiredService<TimeProvider>();
        var completed = new ExportJob(
            Id: "abc",
            Scope: ExportScope.System,
            GroupId: null,
            ResourceTypes: [],
            Since: null,
            DeIdentificationProfile: null,
            RequestorId: null,
            Status: ExportJobStatus.Completed,
            CreatedAt: time.GetUtcNow(),
            CompletedAt: time.GetUtcNow(),
            Outputs: [],
            Error: null);
        await store.CreateAsync(completed, CancellationToken.None);

        var orchestrator = host.Services.GetRequiredService<IExportJobOrchestrator>();
        await orchestrator.CancelAsync("abc", CancellationToken.None);

        var after = await store.GetAsync("abc", CancellationToken.None);
        after!.Status.ShouldBe(ExportJobStatus.Completed);

        await host.StopAsync();
    }

    private IHost BuildHost(bool includeHostedService = true)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        builder.Services.AddFhir(_ => { });
        builder.Services.AddFhirBulkData(_storageRoot);

        if (includeHostedService)
        {
            builder.Services.AddFhirBulkDataOrchestrator();
        }
        else
        {
            // Register orchestrator deps without the hosted service so jobs stay Queued.
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<ExportJobQueue>();
            builder.Services.AddSingleton<NdjsonFeederBinder>();
            builder.Services.AddSingleton<IExportJobOrchestrator, DefaultExportJobOrchestrator>();
        }

        builder.Services.AddFhirBulkDataFeeder<TwoPatientsFeeder, Patient>();
        return builder.Build();
    }

    private static async Task<ExportJob> WaitForStatusAsync(IExportJobStore store, string jobId, ExportJobStatus status, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var job = await store.GetAsync(jobId, CancellationToken.None);
            if (job is not null && job.Status == status)
            {
                return job;
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Job {jobId} did not reach status {status} within {timeout}.");
    }

    private sealed class TwoPatientsFeeder : INdjsonResourceFeeder<Patient>
    {
        public async IAsyncEnumerable<Patient> StreamAsync(ExportJob job, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new Patient { Id = "p1" };
            await Task.Yield();
            yield return new Patient { Id = "p2" };
        }
    }
}
