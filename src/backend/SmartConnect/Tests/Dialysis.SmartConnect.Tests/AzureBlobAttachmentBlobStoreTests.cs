using System.Text;
using Dialysis.SmartConnect.Persistence.ObjectStorage.AzureBlob;
using Microsoft.Extensions.Options;
using Testcontainers.Azurite;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Azure Blob integration tests against an Azurite container. Azurite emulates the Azure Storage
/// REST API faithfully enough that the same client exercises the same code paths the production
/// Azure backend will hit. Tests share one container per fixture instance to keep startup cost
/// bounded (~3s per container vs. ~12s per test).
/// </summary>
public sealed class AzureBlobAttachmentBlobStoreTests : IAsyncLifetime
{
    // Pinned image — Testcontainers' parameterless AzuriteBuilder() may move to [Obsolete] in
    // future releases, mirroring how MinioBuilder did. Explicit-image construction is stable.
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite:3.35.0";
    private const string Container = "smartconnect-attachments-test";
    private AzuriteContainer? _azurite;

    public async Task InitializeAsync()
    {
        _azurite = new AzuriteBuilder(AzuriteImage).Build();
        await _azurite.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_azurite is not null)
            await _azurite.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task Write_Then_Read_Round_Trips_Bytes_Async()
    {
        var store = BuildStore();
        var id = Guid.CreateVersion7();
        var bytes = Encoding.UTF8.GetBytes("hello-azure");

        await store.WriteAsync(id, bytes, CancellationToken.None);
        var read = await store.ReadAsync(id, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal("hello-azure", Encoding.UTF8.GetString(read!.Value.Span));
    }

    [Fact]
    public async Task Read_Missing_Returns_Null_Async()
    {
        var store = BuildStore();
        var read = await store.ReadAsync(Guid.CreateVersion7(), CancellationToken.None);
        Assert.Null(read);
    }

    [Fact]
    public async Task Delete_Removes_The_Blob_Async()
    {
        var store = BuildStore();
        var id = Guid.CreateVersion7();
        await store.WriteAsync(id, Encoding.UTF8.GetBytes("x"), CancellationToken.None);

        await store.DeleteAsync(id, CancellationToken.None);

        Assert.Null(await store.ReadAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Enumerate_Lists_Every_Written_Blob_Async()
    {
        var store = BuildStore(keyPrefix: $"test-{Guid.NewGuid():N}/");
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7()).ToList();
        foreach (var id in ids)
        {
            await store.WriteAsync(id, Encoding.UTF8.GetBytes(id.ToString()), CancellationToken.None);
        }

        var enumerated = new HashSet<Guid>();
        await foreach (var blob in store.EnumerateAsync(CancellationToken.None))
        {
            enumerated.Add(blob.Id);
            Assert.True(blob.SizeBytes > 0);
        }

        Assert.Equal([.. ids], enumerated);
    }

    [Fact]
    public async Task Stores_Bytes_In_Row_Is_False_So_Reaper_And_Blob_First_Ordering_Engage_Async()
    {
        var store = BuildStore();
        Assert.False(store.StoresBytesInRow);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Sync_Write_Round_Trips_Bytes_Because_Azure_Sdk_Supports_Sync_Async()
    {
        var store = BuildStore();
        var id = Guid.CreateVersion7();

        // Task.Run keeps VSTHRD103 happy — the sync code path is still exercised, just off the
        // test thread. The point of the test is that Write doesn't throw NotSupportedException
        // (unlike S3), proving the Jint sync-binding chain remains valid against Azure Blob.
        await Task.Run(() => store.Write(id, Encoding.UTF8.GetBytes("sync-azure"), CancellationToken.None));

        var read = await store.ReadAsync(id, CancellationToken.None);
        Assert.NotNull(read);
        Assert.Equal("sync-azure", Encoding.UTF8.GetString(read!.Value.Span));
    }

    private AzureBlobAttachmentBlobStore BuildStore(string keyPrefix = "")
    {
        var azurite = _azurite ?? throw new InvalidOperationException("InitializeAsync was not called.");
        return new AzureBlobAttachmentBlobStore(Options.Create(new AzureBlobAttachmentBlobOptions
        {
            ContainerName = Container,
            ConnectionString = azurite.GetConnectionString(),
            KeyPrefix = keyPrefix,
        }));
    }
}
