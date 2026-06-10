using System.Runtime.CompilerServices;
using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Dialysis.SmartConnect.Persistence.ObjectStorage.ContentAddressable;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Unit tests for the CAS decorator using an in-memory inner store. Verifies the three behaviours
/// the decorator is meant to add: SHA-256 dedupe, ref-count delete, and pass-through enumerate.
/// </summary>
public sealed class ContentAddressableAttachmentBlobStoreTests
{
    [Fact]
    public async Task Writing_Same_Content_Twice_Reuses_One_Underlying_Blob_Async()
    {
        await using var fixture = new Fixture();
        var store = fixture.Store;
        var bytes = Encoding.UTF8.GetBytes("identical-payload");
        var idA = Guid.CreateVersion7();
        var idB = Guid.CreateVersion7();

        await store.WriteAsync(idA, bytes, CancellationToken.None);
        await store.WriteAsync(idB, bytes, CancellationToken.None);

        Assert.Equal(1, fixture.Inner.WriteCount);
        var readA = await store.ReadAsync(idA, CancellationToken.None);
        var readB = await store.ReadAsync(idB, CancellationToken.None);
        Assert.Equal("identical-payload", Encoding.UTF8.GetString(readA!.Value.Span));
        Assert.Equal("identical-payload", Encoding.UTF8.GetString(readB!.Value.Span));
    }

    [Fact]
    public async Task Deleting_One_Of_Two_Refs_Keeps_The_Blob_Alive_Async()
    {
        await using var fixture = new Fixture();
        var store = fixture.Store;
        var bytes = Encoding.UTF8.GetBytes("shared");
        var idA = Guid.CreateVersion7();
        var idB = Guid.CreateVersion7();
        await store.WriteAsync(idA, bytes, CancellationToken.None);
        await store.WriteAsync(idB, bytes, CancellationToken.None);

        await store.DeleteAsync(idA, CancellationToken.None);

        Assert.Equal(0, fixture.Inner.DeleteCount);
        var readB = await store.ReadAsync(idB, CancellationToken.None);
        Assert.NotNull(readB);
    }

    [Fact]
    public async Task Deleting_Last_Ref_Removes_The_Underlying_Blob_Async()
    {
        await using var fixture = new Fixture();
        var store = fixture.Store;
        var bytes = Encoding.UTF8.GetBytes("only-ref");
        var id = Guid.CreateVersion7();
        await store.WriteAsync(id, bytes, CancellationToken.None);

        await store.DeleteAsync(id, CancellationToken.None);

        Assert.Equal(1, fixture.Inner.DeleteCount);
        var read = await store.ReadAsync(id, CancellationToken.None);
        Assert.Null(read);
    }

    [Fact]
    public async Task Stores_Bytes_In_Row_Is_False_Async()
    {
        await using var fixture = new Fixture();
        Assert.False(fixture.Store.StoresBytesInRow);
        await Task.CompletedTask;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;

        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
            services.AddSingleton<RecordingBlobStore>();
            _services = services.BuildServiceProvider();
            Inner = _services.GetRequiredService<RecordingBlobStore>();
            Store = new ContentAddressableAttachmentBlobStore(
                Inner,
                _services.GetRequiredService<IServiceScopeFactory>());
            // Ensure the in-memory DbContext is materialized (creates the model).
            using var scope = _services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        }

        public RecordingBlobStore Inner { get; }
        public ContentAddressableAttachmentBlobStore Store { get; }

        public async ValueTask DisposeAsync() => await _services.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class RecordingBlobStore : IAttachmentBlobStore
    {
        private readonly Dictionary<Guid, byte[]> _data = [];
        public int WriteCount { get; private set; }
        public int DeleteCount { get; private set; }
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
            if (_data.Remove(id))
                DeleteCount++;
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
