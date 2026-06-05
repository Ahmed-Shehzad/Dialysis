using System.Runtime.CompilerServices;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.DeIdentification;
using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.BulkData;

/// <summary>
/// Coverage for the de-identification gate in <see cref="ExportJobRunner"/>: a job requested with a
/// de-identification profile must have its resources scrubbed before they are written, and must fail
/// closed (no output) when the profile can't be honoured.
/// </summary>
public sealed class ExportJobRunnerDeIdentificationTests : IAsyncLifetime
{
    private string _root = string.Empty;

    public Task InitializeAsync()
    {
        _root = Path.Combine(Path.GetTempPath(), "fhir-export-deid-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch { /* best-effort */ }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Applies_De_Identification_To_Streamed_Resources_Async()
    {
        await using var provider = BuildProvider(includeDeIdentifier: true);
        var store = provider.GetRequiredService<IExportJobStore>();
        var serializer = provider.GetRequiredService<FhirJsonSerializerProvider>();
        var runner = ActivatorUtilities.CreateInstance<ExportJobRunner>(provider);

        var job = await store.CreateAsync(NewJob("SafeHarbor"), CancellationToken.None);
        await runner.RunAsync(job.Id, CancellationToken.None);

        var completed = await store.GetAsync(job.Id, CancellationToken.None);
        completed!.Status.ShouldBe(ExportJobStatus.Completed);

        var lines = await File.ReadAllLinesAsync(Path.Combine(_root, job.Id, "Patient.ndjson"));
        lines.Length.ShouldBe(1);
        var patient = serializer.Parse<Patient>(lines[0]);
        patient.Name.ShouldBeEmpty();        // identifying name stripped
        patient.Identifier.ShouldBeEmpty();  // MRN stripped
        patient.BirthDate.ShouldBe("1980");  // generalized to year
    }

    [Fact]
    public async Task Fails_Closed_When_De_Identification_Requested_But_Unavailable_Async()
    {
        await using var provider = BuildProvider(includeDeIdentifier: false);
        var store = provider.GetRequiredService<IExportJobStore>();
        var runner = ActivatorUtilities.CreateInstance<ExportJobRunner>(provider);

        var job = await store.CreateAsync(NewJob("SafeHarbor"), CancellationToken.None);
        await runner.RunAsync(job.Id, CancellationToken.None);

        var failed = await store.GetAsync(job.Id, CancellationToken.None);
        failed!.Status.ShouldBe(ExportJobStatus.Failed);
        failed.Error.ShouldNotBeNull();
        failed.Error!.ShouldContain("de-identification", Case.Insensitive);

        // No identified PHI was written — the job failed before any file was opened.
        Directory.Exists(Path.Combine(_root, job.Id)).ShouldBeFalse();
    }

    private ExportJob NewJob(string? profile) => new(
        Id: Guid.NewGuid().ToString("N"),
        Scope: ExportScope.System,
        GroupId: null,
        ResourceTypes: ["Patient"],
        Since: null,
        DeIdentificationProfile: profile,
        RequestorId: "tester",
        Status: ExportJobStatus.Queued,
        CreatedAt: DateTimeOffset.UtcNow,
        CompletedAt: null,
        Outputs: [],
        Error: null);

    private ServiceProvider BuildProvider(bool includeDeIdentifier)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<FhirJsonSerializerProvider>();
        services.AddSingleton<IExportJobStore, InMemoryExportJobStore>();
        services.AddSingleton<IBulkDataStorage>(new LocalFileBulkDataStorage(_root));
        services.AddSingleton<NdjsonFeederBinder>();
        services.AddFhirBulkDataFeeder<OnePatientFeeder, Patient>();
        if (includeDeIdentifier)
        {
            services.AddFhirDeIdentification();
        }
        return services.BuildServiceProvider();
    }

    private sealed class OnePatientFeeder : INdjsonResourceFeeder<Patient>
    {
        public async IAsyncEnumerable<Patient> StreamAsync(ExportJob job, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new Patient
            {
                Id = "p1",
                Name = [new HumanName { Family = "Doe", Given = ["Jane"] }],
                Identifier = [new Identifier("urn:dialysis:mrn", "MRN-123")],
                BirthDate = "1980-07-14",
            };
        }
    }
}
