using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Slice C2 endpoint binding: GET /smartconnect/v1/admin/messages must accept ?messageType= and
/// ?senderId= query parameters and propagate them into MessageLedgerQueryCriteria. The EF filter
/// logic itself is already covered by MessageLedgerSearchableColumnsTests; this fixture's job is
/// to lock in the HTTP-to-criteria binding so the operator-dashboard filter UI works end-to-end.
/// </summary>
public sealed class LedgerMessageTypeSenderIdFiltersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LedgerMessageTypeSenderIdFiltersTests(WebApplicationFactory<Program> factory) =>
        _factory = factory;

    [Fact]
    public async Task Messages_Endpoint_Filters_By_MessageType_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        await Seed_Ledger_Async(ledger, flowId, messageType: "ADT^A01", senderId: "MachineA@FAC");
        await Seed_Ledger_Async(ledger, flowId, messageType: "ORU^R01", senderId: "MachineA@FAC");
        await Seed_Ledger_Async(ledger, flowId, messageType: "ADT^A01", senderId: "MachineB@FAC");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/smartconnect/v1/admin/messages?flowId={flowId}&messageType=ADT%5EA01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (items, totalCount) = await ReadList_Async(response);
        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
        Assert.All(items, e =>
            Assert.Equal("ADT^A01", e.GetProperty("metadata").GetProperty(LedgerSearchKeys.MessageType).GetString()));
    }

    [Fact]
    public async Task Messages_Endpoint_Filters_By_SenderId_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        await Seed_Ledger_Async(ledger, flowId, messageType: "ADT^A01", senderId: "MachineX@FAC");
        await Seed_Ledger_Async(ledger, flowId, messageType: "ADT^A01", senderId: "MachineY@FAC");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/smartconnect/v1/admin/messages?flowId={flowId}&senderId=MachineX%40FAC");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (items, totalCount) = await ReadList_Async(response);
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal(
            "MachineX@FAC",
            items[0].GetProperty("metadata").GetProperty(LedgerSearchKeys.SenderId).GetString());
    }

    [Fact]
    public async Task Messages_Endpoint_Combines_Filters_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        await Seed_Ledger_Async(ledger, flowId, messageType: "ADT^A01", senderId: "MachineA@FAC");
        await Seed_Ledger_Async(ledger, flowId, messageType: "ADT^A01", senderId: "MachineB@FAC");
        await Seed_Ledger_Async(ledger, flowId, messageType: "ORU^R01", senderId: "MachineA@FAC");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/smartconnect/v1/admin/messages?flowId={flowId}&messageType=ADT%5EA01&senderId=MachineA%40FAC");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, totalCount) = await ReadList_Async(response);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task Messages_Endpoint_Treats_Blank_Filters_As_Unset_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        await Seed_Ledger_Async(ledger, flowId, messageType: "ADT^A01", senderId: "MachineA@FAC");

        using var client = _factory.CreateClient();
        // Empty messageType / senderId must not collapse to "no rows match the empty string" —
        // the endpoint normalises blanks back to the unfiltered state so the dashboard's
        // "(any)" option works.
        var response = await client.GetAsync(
            $"/smartconnect/v1/admin/messages?flowId={flowId}&messageType=&senderId=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, totalCount) = await ReadList_Async(response);
        Assert.Equal(1, totalCount);
    }

    private static async Task Seed_Ledger_Async(
        IMessageLedger ledger,
        Guid flowId,
        string messageType,
        string senderId)
    {
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add(LedgerSearchKeys.MessageType, messageType)
            .Add(LedgerSearchKeys.SenderId, senderId);
        await ledger.AppendAsync(
            new MessageLedgerEntry
            {
                Id = Guid.CreateVersion7(),
                FlowId = flowId,
                IntegrationMessageId = Guid.CreateVersion7(),
                CorrelationId = "C",
                Status = MessageLedgerStatus.Received,
                Metadata = metadata,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);
    }

    private static async Task<(IReadOnlyList<JsonElement> Items, int TotalCount)> ReadList_Async(
        HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        // Clone before disposing the doc so the elements remain readable to assertions.
        var items = doc.RootElement.GetProperty("items").EnumerateArray().Select(e => e.Clone()).ToArray();
        var totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        return (items, totalCount);
    }
}
