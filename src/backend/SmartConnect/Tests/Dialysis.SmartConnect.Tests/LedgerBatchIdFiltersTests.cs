using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Slice D2 endpoint binding: GET /api/v1/admin/messages must accept ?batchId= and
/// propagate it into MessageLedgerQueryCriteria. The EF filter logic itself is covered by
/// the searchable-columns tests; this fixture's job is to lock in the HTTP-to-criteria
/// binding so the operator dashboard's batch-id filter works end-to-end.
/// </summary>
public sealed class LedgerBatchIdFiltersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LedgerBatchIdFiltersTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Messages_Endpoint_Filters_By_BatchId_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        const string batchA = "file:/var/spool/hl7-in/labs-2026-06-01.csv";
        const string batchB = "file:/var/spool/hl7-in/labs-2026-06-02.csv";
        await Seed_Ledger_Async(ledger, flowId, batchA, sequence: 1, total: 3);
        await Seed_Ledger_Async(ledger, flowId, batchA, sequence: 2, total: 3);
        await Seed_Ledger_Async(ledger, flowId, batchA, sequence: 3, total: 3);
        await Seed_Ledger_Async(ledger, flowId, batchB, sequence: 1, total: 1);

        using var client = _factory.CreateClient();
        var encoded = Uri.EscapeDataString(batchA);
        var response = await client.GetAsync(
            $"/api/v1/admin/messages?flowId={flowId}&batchId={encoded}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (items, totalCount) = await ReadList_Async(response);
        Assert.Equal(3, totalCount);
        Assert.Equal(3, items.Count);
        Assert.All(items, e =>
            Assert.Equal(batchA, e.GetProperty("metadata").GetProperty(BatchMetadataKeys.BatchId).GetString()));
    }

    [Fact]
    public async Task Messages_Endpoint_Combines_BatchId_With_MessageType_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        const string batchA = "file:/csv/inflight.csv";
        await Seed_Ledger_Async(ledger, flowId, batchA, sequence: 1, total: 2, messageType: "ORU^R01");
        await Seed_Ledger_Async(ledger, flowId, batchA, sequence: 2, total: 2, messageType: "ADT^A01");

        using var client = _factory.CreateClient();
        var encoded = Uri.EscapeDataString(batchA);
        var response = await client.GetAsync(
            $"/api/v1/admin/messages?flowId={flowId}&batchId={encoded}&messageType=ORU%5ER01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, totalCount) = await ReadList_Async(response);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task Messages_Endpoint_Treats_Blank_BatchId_As_Unset_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        await Seed_Ledger_Async(ledger, flowId, "file:/csv/labs.csv", sequence: 1, total: 1);

        using var client = _factory.CreateClient();
        // The dashboard's "(any)" input renders as empty `batchId=` — blank must not collapse
        // to "match the literal empty string" (no rows would return).
        var response = await client.GetAsync(
            $"/api/v1/admin/messages?flowId={flowId}&batchId=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, totalCount) = await ReadList_Async(response);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task Messages_Endpoint_Returns_Empty_For_Unknown_BatchId_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var flowId = Guid.CreateVersion7();
        await Seed_Ledger_Async(ledger, flowId, "file:/csv/real.csv", sequence: 1, total: 1);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync(
            $"/api/v1/admin/messages?flowId={flowId}&batchId={Uri.EscapeDataString("file:/csv/missing.csv")}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, totalCount) = await ReadList_Async(response);
        Assert.Equal(0, totalCount);
    }

    private static async Task Seed_Ledger_Async(
        IMessageLedger ledger,
        Guid flowId,
        string batchId,
        int sequence,
        int total,
        string? messageType = null)
    {
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add(BatchMetadataKeys.BatchId, batchId)
            .Add(BatchMetadataKeys.Sequence, sequence.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Add(BatchMetadataKeys.Total, total.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(messageType))
        {
            metadata = metadata
                .Add(LedgerSearchKeys.MessageType, messageType)
                .Add(LedgerSearchKeys.SenderId, "test-sender");
        }
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
        var items = doc.RootElement.GetProperty("items").EnumerateArray().Select(e => e.Clone()).ToArray();
        var totalCount = doc.RootElement.GetProperty("totalCount").GetInt32();
        return (items, totalCount);
    }
}
