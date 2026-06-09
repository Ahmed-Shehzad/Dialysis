using System.Collections.Immutable;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Ledger;

/// <summary>
/// Slice C2: the ledger derives <c>MessageType</c> and <c>SenderId</c> indexed columns
/// from each row's metadata on append, so the operator dashboard can filter without
/// scanning the JSON blob. Two derivation paths are tested:
/// (1) top-level <c>LedgerSearchKeys</c> entries (what slice C2-aware transports set),
/// (2) legacy <c>smartconnect.sourcemap.json</c> blob (what the pre-C2 MLLP listener
/// emitted).
/// </summary>
public sealed class MessageLedgerSearchableColumnsTests
{
    [Fact]
    public async Task Append_With_Top_Level_Keys_Populates_Searchable_Columns_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var flowId = Guid.CreateVersion7();
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add(LedgerSearchKeys.MessageType, "ORU^R40^ORU_R40")
            .Add(LedgerSearchKeys.SenderId, "MachineA@FACILITY");

        await ledger.AppendAsync(BuildEntry(flowId, metadata), CancellationToken.None);

        var (items, _) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { FlowId = flowId, MessageType = "ORU^R40^ORU_R40" },
            CancellationToken.None);
        var row = Assert.Single(items);
        Assert.Equal("ORU^R40^ORU_R40", row.Metadata[LedgerSearchKeys.MessageType]);
        Assert.Equal("MachineA@FACILITY", row.Metadata[LedgerSearchKeys.SenderId]);
    }

    [Fact]
    public async Task Query_Filters_By_Sender_Id_Across_Flows_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var flowA = Guid.CreateVersion7();
        var flowB = Guid.CreateVersion7();

        await ledger.AppendAsync(
            BuildEntry(flowA, ImmutableDictionary<string, string>.Empty.Add(LedgerSearchKeys.SenderId, "MachineA@FAC")),
            CancellationToken.None);
        await ledger.AppendAsync(
            BuildEntry(flowB, ImmutableDictionary<string, string>.Empty.Add(LedgerSearchKeys.SenderId, "MachineB@FAC")),
            CancellationToken.None);
        await ledger.AppendAsync(
            BuildEntry(flowA, ImmutableDictionary<string, string>.Empty.Add(LedgerSearchKeys.SenderId, "MachineA@FAC")),
            CancellationToken.None);

        var (items, total) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { SenderId = "MachineA@FAC" },
            CancellationToken.None);

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        Assert.All(items, row => Assert.Equal("MachineA@FAC", row.Metadata[LedgerSearchKeys.SenderId]));
    }

    [Fact]
    public async Task Append_Falls_Back_To_Sourcemap_Json_When_Top_Level_Keys_Missing_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var flowId = Guid.CreateVersion7();
        const string sourcemap = """
            {
              "hl7.sendingApplication": "MachineC",
              "hl7.sendingFacility": "RemoteSite",
              "hl7.messageType": "ADT^A01"
            }
            """;
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("smartconnect.sourcemap.json", sourcemap);

        await ledger.AppendAsync(BuildEntry(flowId, metadata), CancellationToken.None);

        // Even though we never set the top-level keys, the derived columns filter against
        // the values projected from the sourcemap blob.
        var (items, _) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { MessageType = "ADT^A01", SenderId = "MachineC@RemoteSite" },
            CancellationToken.None);
        Assert.Single(items);
    }

    [Fact]
    public async Task Append_With_No_Searchable_Metadata_Leaves_Filters_Empty_Async()
    {
        await using var sp = Build_Services();
        var ledger = sp.GetRequiredService<IMessageLedger>();
        var query = sp.GetRequiredService<IMessageLedgerQuery>();
        var flowId = Guid.CreateVersion7();

        // Metadata has unrelated keys only.
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("custom.tag", "value");
        await ledger.AppendAsync(BuildEntry(flowId, metadata), CancellationToken.None);

        var (typed, _) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { FlowId = flowId, MessageType = "anything" },
            CancellationToken.None);
        Assert.Empty(typed);

        // But the row itself does exist when queried without the searchable filter.
        var (all, _) = await query.QueryAsync(
            new MessageLedgerQueryCriteria { FlowId = flowId },
            CancellationToken.None);
        Assert.Single(all);
    }

    private static MessageLedgerEntry BuildEntry(Guid flowId, ImmutableDictionary<string, string> metadata) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = flowId,
        IntegrationMessageId = Guid.CreateVersion7(),
        CorrelationId = "C",
        Status = MessageLedgerStatus.Received,
        Metadata = metadata,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    private static ServiceProvider Build_Services()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        return services.BuildServiceProvider();
    }
}
