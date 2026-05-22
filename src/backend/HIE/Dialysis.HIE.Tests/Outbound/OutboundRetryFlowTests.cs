using Dialysis.CQRS;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Features.ListOutboundBundles;
using Dialysis.HIE.Outbound.Features.RetryOutboundBundle;
using Dialysis.HIE.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

/// <summary>
/// Operator-dashboard slice — list + retry over outbound bundles. Targets the new
/// <c>HieOpsAdminController</c> indirectly through the CQRS gateway, which is what the
/// controller does internally too. Each step (seed / dispatch / verify) runs in its own
/// service scope so the test sees what the dispatcher's scope actually persisted, not a
/// tracked-from-seed copy of the entity.
/// </summary>
public sealed class OutboundRetryFlowTests
{
    [Fact]
    public async Task Listoutboundbundles_Returns_All_Statuses_When_Filter_Omitted_Async()
    {
        await using var factory = new HieWebApplicationFactory();

        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HieDbContext>();
            var nowUtc = DateTime.UtcNow;

            var delivered = new OutboundBundle(Guid.NewGuid(), "Patient", "p-1", "default", "{}", nowUtc);
            delivered.MarkDelivered(nowUtc);

            var failed = new OutboundBundle(Guid.NewGuid(), "Patient", "p-2", "default", "{}", nowUtc.AddSeconds(1));
            failed.MarkAttemptFailed("503", nowUtc.AddSeconds(60), maxAttempts: 1);

            var pending = new OutboundBundle(Guid.NewGuid(), "Patient", "p-3", "default", "{}", nowUtc.AddSeconds(2));

            await db.OutboundBundles.AddRangeAsync(delivered, failed, pending);
            await db.SaveChangesAsync();
        }

        await using var queryScope = factory.Services.CreateAsyncScope();
        var gateway = queryScope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var all = await gateway.SendQueryAsync<ListOutboundBundlesQuery, IReadOnlyList<OutboundBundleDto>>(
            new ListOutboundBundlesQuery(StatusFilter: null), CancellationToken.None);

        all.Count.ShouldBe(3);
        all.Select(b => b.Status).ShouldContain((int)OutboundBundleStatus.Delivered);
        all.Select(b => b.Status).ShouldContain((int)OutboundBundleStatus.Failed);
        all.Select(b => b.Status).ShouldContain((int)OutboundBundleStatus.Pending);
    }

    [Fact]
    public async Task Listoutboundbundles_Filters_By_Status_Async()
    {
        await using var factory = new HieWebApplicationFactory();

        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HieDbContext>();
            var nowUtc = DateTime.UtcNow;
            var failed = new OutboundBundle(Guid.NewGuid(), "Patient", "p-2", "default", "{}", nowUtc);
            failed.MarkAttemptFailed("503", nowUtc.AddSeconds(60), maxAttempts: 1);
            var pending = new OutboundBundle(Guid.NewGuid(), "Patient", "p-3", "default", "{}", nowUtc.AddSeconds(2));
            await db.OutboundBundles.AddRangeAsync(failed, pending);
            await db.SaveChangesAsync();
        }

        await using var queryScope = factory.Services.CreateAsyncScope();
        var gateway = queryScope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var onlyFailed = await gateway.SendQueryAsync<ListOutboundBundlesQuery, IReadOnlyList<OutboundBundleDto>>(
            new ListOutboundBundlesQuery(StatusFilter: (int)OutboundBundleStatus.Failed), CancellationToken.None);

        onlyFailed.Count.ShouldBe(1);
        onlyFailed[0].Status.ShouldBe((int)OutboundBundleStatus.Failed);
    }

    [Fact]
    public async Task Retryoutboundbundle_Resets_Failed_To_Pending_With_Immediate_Nextattempt_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        Guid bundleId;

        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HieDbContext>();
            var nowUtc = DateTime.UtcNow;
            var failed = new OutboundBundle(Guid.NewGuid(), "Patient", "p-x", "default", "{}", nowUtc);
            failed.MarkAttemptFailed("503", nowUtc.AddHours(1), maxAttempts: 1);
            failed.Status.ShouldBe(OutboundBundleStatus.Failed);
            bundleId = failed.Id;
            db.OutboundBundles.Add(failed);
            await db.SaveChangesAsync();
        }

        await using (var cmdScope = factory.Services.CreateAsyncScope())
        {
            var gateway = cmdScope.ServiceProvider.GetRequiredService<ICqrsGateway>();
            await gateway.SendCommandAsync<RetryOutboundBundleCommand, Unit>(
                new RetryOutboundBundleCommand(bundleId), CancellationToken.None);
        }

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HieDbContext>();
        var reloaded = await verifyDb.OutboundBundles.AsNoTracking().SingleAsync(b => b.Id == bundleId);
        reloaded.Status.ShouldBe(OutboundBundleStatus.Pending);
        reloaded.NextAttemptAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        reloaded.Attempts.ShouldBe(1, "Retry preserves the previous attempt count as audit history.");
    }

    [Fact]
    public async Task Retryoutboundbundle_Is_NoOp_For_Delivered_Bundles_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        Guid bundleId;

        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<HieDbContext>();
            var nowUtc = DateTime.UtcNow;
            var delivered = new OutboundBundle(Guid.NewGuid(), "Patient", "p-y", "default", "{}", nowUtc);
            delivered.MarkDelivered(nowUtc);
            bundleId = delivered.Id;
            db.OutboundBundles.Add(delivered);
            await db.SaveChangesAsync();
        }

        await using (var cmdScope = factory.Services.CreateAsyncScope())
        {
            var gateway = cmdScope.ServiceProvider.GetRequiredService<ICqrsGateway>();
            await gateway.SendCommandAsync<RetryOutboundBundleCommand, Unit>(
                new RetryOutboundBundleCommand(bundleId), CancellationToken.None);
        }

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HieDbContext>();
        var reloaded = await verifyDb.OutboundBundles.AsNoTracking().SingleAsync(b => b.Id == bundleId);
        reloaded.Status.ShouldBe(OutboundBundleStatus.Delivered, "Delivered bundles are immutable terminal state.");
    }
}
