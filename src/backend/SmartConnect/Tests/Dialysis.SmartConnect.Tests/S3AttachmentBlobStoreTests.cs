using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Dialysis.SmartConnect.Persistence.ObjectStorage.S3;
using Microsoft.Extensions.Options;
using Testcontainers.Minio;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// S3 integration tests against a MinIO container. MinIO is S3-API-compatible, so the same client
/// exercises the same code paths the production AWS backend will hit. Tests share one container
/// per fixture instance to keep startup cost bounded (~2s per container vs. ~12s per test).
/// </summary>
public sealed class S3AttachmentBlobStoreTests : IAsyncLifetime
{
    // Pinned image — Testcontainers' parameterless MinioBuilder() is [Obsolete] in 4.x.
    private const string MinioImage = "minio/minio:RELEASE.2024-12-18T13-15-44Z";
    private const string Bucket = "smartconnect-attachments-test";
    private MinioContainer? _minio;

    public async Task InitializeAsync()
    {
        _minio = new MinioBuilder(MinioImage).Build();
        await _minio.StartAsync().ConfigureAwait(false);
        await EnsureBucketAsync(_minio).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_minio is not null)
            await _minio.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task Write_Then_Read_Round_Trips_Bytes_Async()
    {
        using var store = BuildStore();
        var id = Guid.CreateVersion7();
        var bytes = Encoding.UTF8.GetBytes("hello-s3");

        await store.WriteAsync(id, bytes, CancellationToken.None);
        var read = await store.ReadAsync(id, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal("hello-s3", Encoding.UTF8.GetString(read!.Value.Span));
    }

    [Fact]
    public async Task Read_Missing_Returns_Null_Async()
    {
        using var store = BuildStore();
        var read = await store.ReadAsync(Guid.CreateVersion7(), CancellationToken.None);
        Assert.Null(read);
    }

    [Fact]
    public async Task Delete_Removes_The_Object_Async()
    {
        using var store = BuildStore();
        var id = Guid.CreateVersion7();
        await store.WriteAsync(id, Encoding.UTF8.GetBytes("x"), CancellationToken.None);

        await store.DeleteAsync(id, CancellationToken.None);

        Assert.Null(await store.ReadAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Enumerate_Lists_Every_Written_Blob_Async()
    {
        using var store = BuildStore(keyPrefix: $"test-{Guid.NewGuid():N}/");
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
        using var store = BuildStore();
        Assert.False(store.StoresBytesInRow);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Sync_Write_Throws_Because_S3_Sdk_Is_Async_Only_Async()
    {
        using var store = BuildStore();
        Assert.Throws<NotSupportedException>(() =>
            store.Write(Guid.CreateVersion7(), Encoding.UTF8.GetBytes("x"), CancellationToken.None));
        await Task.CompletedTask;
    }

    private S3AttachmentBlobStore BuildStore(string keyPrefix = "")
    {
        var minio = _minio ?? throw new InvalidOperationException("InitializeAsync was not called.");
        return new S3AttachmentBlobStore(Options.Create(new S3AttachmentBlobOptions
        {
            BucketName = Bucket,
            ServiceUrl = minio.GetConnectionString(),
            ForcePathStyle = true,
            AccessKey = minio.GetAccessKey(),
            SecretKey = minio.GetSecretKey(),
            KeyPrefix = keyPrefix,
        }));
    }

    private static async Task EnsureBucketAsync(MinioContainer minio)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = minio.GetConnectionString(),
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        using var client = new AmazonS3Client(minio.GetAccessKey(), minio.GetSecretKey(), config);
        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = Bucket }).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            // Idempotent: re-running locally reuses the container.
        }
    }
}
