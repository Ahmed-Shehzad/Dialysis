using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Batching;

/// <summary>
/// Slice D: batch context is convention over schema. Inbound transports stamp a
/// <see cref="BatchContext"/> onto each emitted message via the well-known
/// <see cref="BatchMetadataKeys"/>, and slice C's metadata persistence carries it
/// through the ledger so the operator dashboard can group, filter, and resume by batch
/// without any new database column.
/// </summary>
public sealed class BatchContextTests
{
    [Fact]
    public void With_Batch_Stamps_All_Four_Metadata_Keys()
    {
        var message = BuildMessage();
        var batchId = Guid.NewGuid().ToString();

        var tagged = message.WithBatch(batchId, sequence: 3, total: 10, source: "file:lab-results.csv");

        Assert.Equal(batchId, tagged.Metadata[BatchMetadataKeys.BatchId]);
        Assert.Equal("3", tagged.Metadata[BatchMetadataKeys.Sequence]);
        Assert.Equal("10", tagged.Metadata[BatchMetadataKeys.Total]);
        Assert.Equal("file:lab-results.csv", tagged.Metadata[BatchMetadataKeys.Source]);
    }

    [Fact]
    public void With_Batch_Omits_Source_When_Not_Provided()
    {
        var tagged = BuildMessage().WithBatch("b1", sequence: 1, total: 5);

        Assert.False(tagged.Metadata.ContainsKey(BatchMetadataKeys.Source));
    }

    [Fact]
    public void Try_Get_Batch_Round_Trips_Metadata_Into_Typed_Context()
    {
        var tagged = BuildMessage().WithBatch("batch-abc", sequence: 7, total: 12, source: "query:pending");

        Assert.True(tagged.TryGetBatch(out var ctx));
        Assert.Equal("batch-abc", ctx.BatchId);
        Assert.Equal(7, ctx.Sequence);
        Assert.Equal(12, ctx.Total);
        Assert.Equal("query:pending", ctx.Source);
        Assert.False(ctx.IsFirst);
        Assert.False(ctx.IsLast);
    }

    [Fact]
    public void Try_Get_Batch_Returns_False_When_Required_Keys_Are_Missing()
    {
        // Has a BatchId but no Sequence / Total — partial tagging means "not a batch row".
        var partial = BuildMessage().WithMetadata(BatchMetadataKeys.BatchId, "lone-id");

        Assert.False(partial.TryGetBatch(out var ctx));
        Assert.Equal(default, ctx);
    }

    [Fact]
    public void Is_First_And_Is_Last_Identify_Batch_Boundaries()
    {
        var first = BuildMessage().WithBatch("b1", sequence: 1, total: 3);
        var middle = BuildMessage().WithBatch("b1", sequence: 2, total: 3);
        var last = BuildMessage().WithBatch("b1", sequence: 3, total: 3);

        Assert.True(first.TryGetBatch(out var f) && f.IsFirst && !f.IsLast);
        Assert.True(middle.TryGetBatch(out var m) && !m.IsFirst && !m.IsLast);
        Assert.True(last.TryGetBatch(out var l) && !l.IsFirst && l.IsLast);
    }

    [Theory]
    [InlineData(0, 5)]      // sequence < 1
    [InlineData(6, 5)]      // sequence > total
    [InlineData(1, 0)]      // total < 1
    public void With_Batch_Rejects_Invalid_Sequence_Or_Total(int sequence, int total)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BuildMessage().WithBatch("b1", sequence, total));
    }

    [Fact]
    public async Task Ledger_Persists_Batch_Context_Across_Append_And_Query_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var flowId = Guid.CreateVersion7();
        var batchId = Guid.NewGuid().ToString();
        var tagged = BuildMessage(flowId).WithBatch(batchId, sequence: 2, total: 4, source: "file:orders.txt");

        await ledger.AppendAsync(
            new MessageLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                FlowId = flowId,
                IntegrationMessageId = tagged.Id,
                CorrelationId = tagged.CorrelationId,
                Status = MessageLedgerStatus.Received,
                Metadata = tagged.Metadata,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        var (items, _) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { FlowId = flowId },
            CancellationToken.None);
        var row = Assert.Single(items);

        // Reconstitute the batch context from the persisted ledger row to confirm slice
        // C's MetadataJson column is enough infrastructure for slice D's needs.
        Assert.Equal(batchId, row.Metadata[BatchMetadataKeys.BatchId]);
        Assert.Equal("2", row.Metadata[BatchMetadataKeys.Sequence]);
        Assert.Equal("4", row.Metadata[BatchMetadataKeys.Total]);
        Assert.Equal("file:orders.txt", row.Metadata[BatchMetadataKeys.Source]);
    }

    private static IntegrationMessage BuildMessage(Guid? flowId = null) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = flowId ?? Guid.CreateVersion7(),
        CorrelationId = "C",
        Payload = "payload"u8.ToArray(),
        PayloadFormat = PayloadFormat.Utf8Text,
        Metadata = [],
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

    private static ServiceProvider Build_Services()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        return services.BuildServiceProvider();
    }
}
