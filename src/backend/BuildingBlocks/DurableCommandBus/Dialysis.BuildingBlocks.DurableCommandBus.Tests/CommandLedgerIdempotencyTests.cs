using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.DurableCommandBus.Tests;

/// <summary>
/// Locks down the idempotency contract that the rest of the durable-bus design rests on: a
/// command id maps to exactly one applied effect across any number of redeliveries.
/// </summary>
public sealed class CommandLedgerIdempotencyTests
{
    private static DurableCommandEnvelope MakeEnvelope(Guid? id = null) => new(
        CommandId: id ?? Guid.NewGuid(),
        CommandTypeKey: "TestCommand",
        SchemaVersion: 1,
        PayloadJson: "{}",
        CorrelationId: (id ?? Guid.NewGuid()).ToString("N"),
        EnqueuedAtUtc: DateTime.UtcNow,
        RequestedBySubject: "user1");

    [Fact]
    public async Task First_Claim_Returns_Firstclaim_With_Null_Existing_Async()
    {
        var ledger = new InMemoryCommandLedger();
        var envelope = MakeEnvelope();

        var result = await ledger.TryClaimAsync(envelope, CancellationToken.None);

        result.Outcome.ShouldBe(LedgerClaim.FirstClaim);
        result.Existing.ShouldBeNull();
    }

    [Fact]
    public async Task Second_Claim_Of_Same_Id_When_Applied_Returns_Alreadyapplied_Async()
    {
        var ledger = new InMemoryCommandLedger();
        var envelope = MakeEnvelope();
        await ledger.TryClaimAsync(envelope, CancellationToken.None);
        await ledger.MarkAppliedAsync(envelope.CommandId, "\"result\"", "test-consumer", CancellationToken.None);

        var secondClaim = await ledger.TryClaimAsync(envelope, CancellationToken.None);

        secondClaim.Outcome.ShouldBe(LedgerClaim.AlreadyApplied);
        secondClaim.Existing.ShouldNotBeNull();
        secondClaim.Existing.ResultJson.ShouldBe("\"result\"");
    }

    [Fact]
    public async Task Claim_While_Pending_Returns_Pendingretry_Async()
    {
        var ledger = new InMemoryCommandLedger();
        var envelope = MakeEnvelope();
        await ledger.TryClaimAsync(envelope, CancellationToken.None);

        var secondClaim = await ledger.TryClaimAsync(envelope, CancellationToken.None);

        secondClaim.Outcome.ShouldBe(LedgerClaim.PendingRetry);
        secondClaim.Existing.ShouldNotBeNull();
        secondClaim.Existing.Status.ShouldBe(CommandLedgerStatus.Pending);
    }

    [Fact]
    public async Task Findbycorrelation_Returns_The_Row_When_Known_Async()
    {
        var ledger = new InMemoryCommandLedger();
        var envelope = MakeEnvelope();
        await ledger.TryClaimAsync(envelope, CancellationToken.None);

        var entry = await ledger.FindByCorrelationAsync(envelope.CorrelationId, CancellationToken.None);

        entry.ShouldNotBeNull();
        entry.CommandId.ShouldBe(envelope.CommandId);
    }

    [Fact]
    public async Task Findbycorrelation_Returns_Null_When_Unknown_Async()
    {
        var ledger = new InMemoryCommandLedger();
        var entry = await ledger.FindByCorrelationAsync("does-not-exist", CancellationToken.None);
        entry.ShouldBeNull();
    }

    [Fact]
    public void Markapplied_From_Applied_State_Throws_Async()
    {
        var entry = new CommandLedgerEntry(
            commandId: Guid.NewGuid(),
            commandTypeKey: "T",
            correlationId: "c",
            enqueuedAtUtc: DateTime.UtcNow,
            requestedBySubject: null);
        entry.MarkApplied(DateTime.UtcNow, null, "c1");

        Should.Throw<InvalidOperationException>(() =>
            entry.MarkApplied(DateTime.UtcNow, null, "c2"));
    }
}
