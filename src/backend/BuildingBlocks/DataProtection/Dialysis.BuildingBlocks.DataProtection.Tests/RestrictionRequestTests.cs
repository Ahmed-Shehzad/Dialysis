using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.DataProtection.Restriction;
using Xunit;

namespace Dialysis.BuildingBlocks.DataProtection.Tests;

/// <summary>
/// Covers the GDPR Art. 18 restriction-of-processing lifecycle on
/// <see cref="DefaultDataSubjectRightsService"/>: a filed request is persisted as an audit row
/// (no longer a discarded synthetic id), surfaces in the operator list, and transitions to
/// <see cref="RestrictionRequestStatus.Lifted"/> exactly once.
/// </summary>
public sealed class RestrictionRequestTests
{
    private static DefaultDataSubjectRightsService NewService(IRestrictionRequestStore store) =>
        new(
            extractors: [],
            erasers: [],
            requestStore: new InMemoryErasureRequestStore(),
            restrictionStore: store,
            clock: TimeProvider.System);

    [Fact]
    public async Task Request_Restriction_Persists_An_Active_Audit_Row_Async()
    {
        var store = new InMemoryRestrictionRequestStore();
        var service = NewService(store);
        var patientId = Guid.NewGuid();

        var id = await service.RequestRestrictionAsync(patientId, "subject@example.com", "Disputes accuracy", default);

        var saved = await store.FindAsync(id, default);
        Assert.NotNull(saved);
        Assert.Equal(patientId, saved!.PatientId);
        Assert.Equal(RestrictionRequestStatus.Active, saved.Status);
        Assert.Equal("subject@example.com", saved.RequestedBy);
        Assert.Equal("Disputes accuracy", saved.Reason);
    }

    [Fact]
    public async Task Active_Restrictions_Are_Listed_For_The_Operator_Async()
    {
        var store = new InMemoryRestrictionRequestStore();
        var service = NewService(store);
        await service.RequestRestrictionAsync(Guid.NewGuid(), "a@example.com", null, default);
        await service.RequestRestrictionAsync(Guid.NewGuid(), "b@example.com", null, default);

        var active = await service.ListActiveRestrictionsAsync(10, default);

        Assert.Equal(2, active.Count);
        Assert.All(active, r => Assert.Equal(RestrictionRequestStatus.Active, r.Status));
    }

    [Fact]
    public async Task Lift_Restriction_Marks_The_Row_Lifted_And_Records_Who_And_Why_Async()
    {
        var store = new InMemoryRestrictionRequestStore();
        var service = NewService(store);
        var id = await service.RequestRestrictionAsync(Guid.NewGuid(), "subject@example.com", null, default);

        var lifted = await service.LiftRestrictionAsync(id, "dpo@example.com", "Dispute resolved", default);

        Assert.Equal(RestrictionRequestStatus.Lifted, lifted.Status);
        Assert.Equal("dpo@example.com", lifted.LiftedBy);
        Assert.Equal("Dispute resolved", lifted.LiftReason);
        Assert.NotNull(lifted.LiftedAtUtc);
        Assert.Empty(await service.ListActiveRestrictionsAsync(10, default));
    }

    [Fact]
    public async Task Lifting_An_Already_Lifted_Restriction_Throws_Async()
    {
        var store = new InMemoryRestrictionRequestStore();
        var service = NewService(store);
        var id = await service.RequestRestrictionAsync(Guid.NewGuid(), "subject@example.com", null, default);
        await service.LiftRestrictionAsync(id, "dpo@example.com", "Dispute resolved", default);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LiftRestrictionAsync(id, "dpo@example.com", "again", default));
    }
}
