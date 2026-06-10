using System.Runtime.CompilerServices;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.ObjectStorage.Replication;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Unit tests for the multi-region replication strategy. Verifies the quorum semantics — what
/// happens when 1 of 3 fails, when 2 of 3 fail, when fan-out is best-effort.
/// </summary>
public sealed class MultiRegionReplicationStrategyTests
{
    [Fact]
    public async Task Best_Effort_Does_Not_Throw_On_Secondary_Failure_Async()
    {
        var secondaries = new List<IAttachmentBlobStore> { new FailingStore(), new CountingStore() };
        var strategy = new MultiRegionReplicationStrategy(
            ReplicationMode.BestEffort,
            secondaries,
            NullLogger<MultiRegionReplicationStrategy>.Instance);

        // Should not throw even though one secondary fails; best-effort returns immediately.
        await strategy.ReplicateAsync(Guid.NewGuid(), new byte[] { 1, 2, 3 }, CancellationToken.None);
    }

    [Fact]
    public async Task Quorum_Succeeds_When_Majority_Replicates_Async()
    {
        var secondaries = new List<IAttachmentBlobStore> { new CountingStore(), new CountingStore(), new FailingStore() };
        var strategy = new MultiRegionReplicationStrategy(
            ReplicationMode.Quorum,
            secondaries,
            NullLogger<MultiRegionReplicationStrategy>.Instance);

        await strategy.ReplicateAsync(Guid.NewGuid(), new byte[] { 1 }, CancellationToken.None);
    }

    [Fact]
    public async Task Quorum_Throws_When_Majority_Fails_Async()
    {
        var secondaries = new List<IAttachmentBlobStore> { new FailingStore(), new FailingStore(), new CountingStore() };
        var strategy = new MultiRegionReplicationStrategy(
            ReplicationMode.Quorum,
            secondaries,
            NullLogger<MultiRegionReplicationStrategy>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => strategy.ReplicateAsync(Guid.NewGuid(), new byte[] { 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task All_Throws_If_Any_Secondary_Fails_Async()
    {
        var secondaries = new List<IAttachmentBlobStore> { new CountingStore(), new FailingStore() };
        var strategy = new MultiRegionReplicationStrategy(
            ReplicationMode.All,
            secondaries,
            NullLogger<MultiRegionReplicationStrategy>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => strategy.ReplicateAsync(Guid.NewGuid(), new byte[] { 1 }, CancellationToken.None));
    }

    private sealed class CountingStore : IAttachmentBlobStore
    {
        public bool StoresBytesInRow => false;
        public Task WriteAsync(Guid id, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Write(Guid id, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) { }
        public Task<ReadOnlyMemory<byte>?> ReadAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<ReadOnlyMemory<byte>?>(null);
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public async IAsyncEnumerable<BlobMetadata> EnumerateAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class FailingStore : IAttachmentBlobStore
    {
        public bool StoresBytesInRow => false;
        public Task WriteAsync(Guid id, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("simulated region outage"));
        public void Write(Guid id, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated region outage");
        public Task<ReadOnlyMemory<byte>?> ReadAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromException<ReadOnlyMemory<byte>?>(new InvalidOperationException("simulated region outage"));
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("simulated region outage"));
        public async IAsyncEnumerable<BlobMetadata> EnumerateAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
