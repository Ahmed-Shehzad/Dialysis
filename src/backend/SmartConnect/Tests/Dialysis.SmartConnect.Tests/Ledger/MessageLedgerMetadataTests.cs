using System.Collections.Immutable;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Ledger;

/// <summary>
/// Slice C of the SmartConnect ↔ Mirth alignment plan: the ledger persists every message's
/// in-flight <see cref="IntegrationMessage.Metadata"/> dictionary so the operator dashboard
/// can filter on sender id, message type, and trigger event without re-parsing the payload
/// snapshot.
/// </summary>
public sealed class MessageLedgerMetadataTests
{
    [Fact]
    public async Task Append_Then_Query_Round_Trips_Metadata_Json_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var flowId = Guid.CreateVersion7();
        var messageId = Guid.CreateVersion7();
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("smartconnect.sender", "MachineA")
            .Add("smartconnect.message-type", "ORU^R40")
            .Add("smartconnect.mrn", "MRN-12345");

        await ledger.AppendAsync(
            new MessageLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                FlowId = flowId,
                IntegrationMessageId = messageId,
                CorrelationId = "C1",
                Status = MessageLedgerStatus.Received,
                Metadata = metadata,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        var (items, _) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { FlowId = flowId },
            CancellationToken.None);

        var entry = Assert.Single(items);
        Assert.Equal("MachineA", entry.Metadata["smartconnect.sender"]);
        Assert.Equal("ORU^R40", entry.Metadata["smartconnect.message-type"]);
        Assert.Equal("MRN-12345", entry.Metadata["smartconnect.mrn"]);
    }

    [Fact]
    public async Task Append_With_Empty_Metadata_Stores_Null_Json_Column_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var flowId = Guid.CreateVersion7();

        await ledger.AppendAsync(
            new MessageLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                FlowId = flowId,
                IntegrationMessageId = Guid.CreateVersion7(),
                CorrelationId = "C2",
                Status = MessageLedgerStatus.Received,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        var (items, _) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { FlowId = flowId },
            CancellationToken.None);

        // Empty in → empty out (the EF write-side stores null to keep small ledger rows compact;
        // the read-side projects null/empty back to an empty immutable dictionary).
        Assert.Empty(Assert.Single(items).Metadata);
    }

    [Fact]
    public async Task Get_By_Id_Returns_Metadata_When_Present_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var ledgerEntryId = Guid.CreateVersion7();
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("smartconnect.trigger-event", "ORU^R40");

        await ledger.AppendAsync(
            new MessageLedgerEntry
            {
                Id = ledgerEntryId,
                FlowId = Guid.CreateVersion7(),
                IntegrationMessageId = Guid.CreateVersion7(),
                CorrelationId = "C3",
                Status = MessageLedgerStatus.Completed,
                Metadata = metadata,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        var found = await query.GetByIdAsync(ledgerEntryId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("ORU^R40", found!.Metadata["smartconnect.trigger-event"]);
    }

    private static ServiceProvider Build_Services()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        return services.BuildServiceProvider();
    }
}
