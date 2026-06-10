using System.Runtime.CompilerServices;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Dicom;
using Dialysis.SmartConnect.Dicom.Persistence;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using FellowOakDicom;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Unit tests for the DICOM ingestion service. Builds in-memory DICOM datasets, hands them to the
/// service, and verifies the metadata row + blob bytes both land correctly.
/// </summary>
public sealed class DicomIngestionServiceTests
{
    [Fact]
    public async Task Ingest_Persists_Bytes_And_Metadata_Async()
    {
        var (services, blobs) = BuildServices();
        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ingestion = scope.ServiceProvider.GetRequiredService<IDicomIngestionService>();
        var store = scope.ServiceProvider.GetRequiredService<IDicomInstanceStore>();

        var dcm = BuildDicomFile("1.2.3", "1.2.3.4", "1.2.3.4.5", patientId: "P42", modality: "MR");
        var metadata = await ingestion.IngestAsync(dcm, CancellationToken.None);

        Assert.Equal("1.2.3", metadata.StudyInstanceUid);
        Assert.Equal("P42", metadata.PatientId);
        Assert.Equal("MR", metadata.Modality);
        Assert.True(blobs.WriteCount == 1);
        var fetched = await store.GetAsync("1.2.3.4.5", CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("1.2.3", fetched!.StudyInstanceUid);
    }

    [Fact]
    public async Task Search_Studies_Filters_By_Patient_Async()
    {
        var (services, _) = BuildServices();
        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ingestion = scope.ServiceProvider.GetRequiredService<IDicomIngestionService>();
        var store = scope.ServiceProvider.GetRequiredService<IDicomInstanceStore>();

        await ingestion.IngestAsync(BuildDicomFile("1.2.10", "1.2.10.1", "1.2.10.1.1", patientId: "P1"), CancellationToken.None);
        await ingestion.IngestAsync(BuildDicomFile("1.2.20", "1.2.20.1", "1.2.20.1.1", patientId: "P2"), CancellationToken.None);

        var p1Studies = await store.SearchStudiesAsync("P1", null, null, CancellationToken.None);
        var p2Studies = await store.SearchStudiesAsync("P2", null, null, CancellationToken.None);

        Assert.Single(p1Studies);
        Assert.Single(p2Studies);
        Assert.Equal("1.2.10", p1Studies[0].StudyInstanceUid);
    }

    private static (IServiceCollection Services, RecordingBlobStore Blobs) BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        var blobs = new RecordingBlobStore();
        services.AddSingleton<IAttachmentBlobStore>(blobs);
        services.AddSingleton(TimeProvider.System);
        services.AddDicomIngestion();
        return (services, blobs);
    }

    private static DicomFile BuildDicomFile(
        string studyUid, string seriesUid, string sopUid, string? patientId = null, string? modality = null)
    {
        var ds = new DicomDataset
        {
            { DicomTag.SOPClassUID, "1.2.840.10008.5.1.4.1.1.2" },
            { DicomTag.SOPInstanceUID, sopUid },
            { DicomTag.StudyInstanceUID, studyUid },
            { DicomTag.SeriesInstanceUID, seriesUid },
        };
        if (patientId is not null)
            ds.Add(DicomTag.PatientID, patientId);
        if (modality is not null)
            ds.Add(DicomTag.Modality, modality);
        return new DicomFile(ds);
    }

    private sealed class RecordingBlobStore : IAttachmentBlobStore
    {
        private readonly Dictionary<Guid, byte[]> _data = [];
        public int WriteCount { get; private set; }
        public bool StoresBytesInRow => false;

        public Task WriteAsync(Guid id, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            _data[id] = data.ToArray();
            WriteCount++;
            return Task.CompletedTask;
        }
        public void Write(Guid id, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            _data[id] = data.ToArray();
            WriteCount++;
        }
        public Task<ReadOnlyMemory<byte>?> ReadAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ReadOnlyMemory<byte>?>(_data.TryGetValue(id, out var b) ? new ReadOnlyMemory<byte>(b) : null);
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            _data.Remove(id);
            return Task.CompletedTask;
        }
        public async IAsyncEnumerable<BlobMetadata> EnumerateAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var kv in _data)
            {
                await Task.Yield();
                yield return new BlobMetadata(kv.Key, DateTimeOffset.UtcNow, kv.Value.LongLength);
            }
        }
    }
}
