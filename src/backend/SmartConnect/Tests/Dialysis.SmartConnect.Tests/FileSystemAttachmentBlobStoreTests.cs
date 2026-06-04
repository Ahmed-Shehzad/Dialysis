using System.Text;
using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Out-of-row blob store tests. Each test gets its own tmp dir under
/// <c>Path.GetTempPath()</c> and cleans it up via the <see cref="IAsyncDisposable"/> fixture so
/// parallel xunit collections don't collide on file paths.
/// </summary>
public sealed class FileSystemAttachmentBlobStoreTests
{
    [Fact]
    public async Task Write_Then_Read_Round_Trips_Bytes_Async()
    {
        await using var fx = new FsFixture();
        var bytes = Encoding.UTF8.GetBytes("hello-fs");
        var id = Guid.CreateVersion7();

        await fx.Store.WriteAsync(id, bytes, CancellationToken.None);
        var read = await fx.Store.ReadAsync(id, CancellationToken.None);

        Assert.NotNull(read);
        Assert.Equal("hello-fs", Encoding.UTF8.GetString(read!.Value.Span));
    }

    [Fact]
    public async Task Read_Missing_Returns_Null_Async()
    {
        await using var fx = new FsFixture();
        var read = await fx.Store.ReadAsync(Guid.CreateVersion7(), CancellationToken.None);
        Assert.Null(read);
    }

    [Fact]
    public async Task Delete_Removes_The_File_Async()
    {
        await using var fx = new FsFixture();
        var id = Guid.CreateVersion7();
        await fx.Store.WriteAsync(id, Encoding.UTF8.GetBytes("x"), CancellationToken.None);

        await fx.Store.DeleteAsync(id, CancellationToken.None);

        Assert.Null(await fx.Store.ReadAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task Enumerate_Lists_Every_Written_Blob_Async()
    {
        await using var fx = new FsFixture();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7()).ToList();
        foreach (var id in ids)
        {
            await fx.Store.WriteAsync(id, Encoding.UTF8.GetBytes(id.ToString()), CancellationToken.None);
        }

        var enumerated = new HashSet<Guid>();
        await foreach (var blob in fx.Store.EnumerateAsync(CancellationToken.None))
        {
            enumerated.Add(blob.Id);
            Assert.True(blob.SizeBytes > 0);
        }

        Assert.Equal([.. ids], enumerated);
    }

    [Fact]
    public void Sync_Write_From_Jint_Sync_Bridge_Round_Trips()
    {
        // Sync-bodied because the call under test is the sync `Write` overload — wrapping it in
        // a Task.Run just to keep the test method async would defeat the analyzer's whole point.
        // Verification reads back via plain File.ReadAllText, mirroring how the Jint binder
        // confirms persistence (no async machinery on the script path).
        using var fx = new FsFixture();
        var id = Guid.CreateVersion7();
        var bytes = Encoding.UTF8.GetBytes("sync-fs");

        fx.Store.Write(id, bytes, CancellationToken.None);

        var path = Path.Combine(fx.RootPath, id.ToString("N")[..2], id + ".bin");
        Assert.True(File.Exists(path));
        Assert.Equal("sync-fs", File.ReadAllText(path));
    }

    [Fact]
    public void StoresBytesInRow_Is_False_So_Reaper_And_Blob_First_Ordering_Engage()
    {
        using var fx = new FsFixture();
        Assert.False(fx.Store.StoresBytesInRow);
    }

    private sealed class FsFixture : IAsyncDisposable, IDisposable
    {
        public string RootPath { get; }
        public FileSystemAttachmentBlobStore Store { get; }

        public FsFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "sc-fs-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
            Store = new FileSystemAttachmentBlobStore(
                Options.Create(new FileSystemAttachmentBlobOptions { RootPath = RootPath }));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                    Directory.Delete(RootPath, recursive: true);
            }
            catch (IOException) { /* test cleanup — best effort */ }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
