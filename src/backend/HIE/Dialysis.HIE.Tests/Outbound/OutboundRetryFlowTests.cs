using Dialysis.CQRS;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Features.ListOutboundBundles;
using Dialysis.HIE.Outbound.Features.RetryOutboundBundle;
using Dialysis.HIE.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

/// <summary>
/// Operator-dashboard slice — list + retry over outbound bundles. Targets the new
/// <c>HieOpsAdminController</c> indirectly through the CQRS gateway, which is what the
/// controller does internally too.
/// </summary>
public sealed class OutboundRetryFlowTests
{
    [Fact]
    public async Task Listoutboundbundles_Returns_All_Statuses_When_Filter_Omitted_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<HieDbContext>();
        var nowUtc = DateTime.UtcNow;

        var delivered = new OutboundBundle(Guid.NewGuid(), "Patient", "p-1", "default", "{}", nowUtc);
        delivered.MarkDelivered(nowUtc);

        var failed = new OutboundBundle(Guid.NewGuid(), "Patient", "p-2", "default", "{}", nowUtc.AddSeconds(1));
        failed.MarkAttemptFailed("503", nowUtc.AddSeconds(60), maxAttempts: 1);

        var pending = new OutboundBundle(Guid.NewGuid(), "Patient", "p-3", "default", "{}", nowUtc.AddSeconds(2));

        db.OutboundBundles.AddRange(delivered, failed, pending);
        await db.SaveChangesAsync();

        var gateway = sp.GetRequiredService<ICqrsGateway>();
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
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<HieDbContext>();
        var nowUtc = DateTime.UtcNow;

        var failed = new OutboundBundle(Guid.NewGuid(), "Patient", "p-2", "default", "{}", nowUtc);
        failed.MarkAttemptFailed("503", nowUtc.AddSeconds(60), maxAttempts: 1);
        var pending = new OutboundBundle(Guid.NewGuid(), "Patient", "p-3", "default", "{}", nowUtc.AddSeconds(2));
        db.OutboundBundles.AddRange(failed, pending);
        await db.SaveChangesAsync();

        var gateway = sp.GetRequiredService<ICqrsGateway>();
        var onlyFailed = await gateway.SendQueryAsync<ListOutboundBundlesQuery, IReadOnlyList<OutboundBundleDto>>(
            new ListOutboundBundlesQuery(StatusFilter: (int)OutboundBundleStatus.Failed), CancellationToken.None);

        onlyFailed.Count.ShouldBe(1);
        onlyFailed[0].Status.ShouldBe((int)OutboundBundleStatus.Failed);
    }

    [Fact]
    public async Task Retryoutboundbundle_Resets_Failed_To_Pending_With_Immediate_Nextattempt_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<HieDbContext>();
        var nowUtc = DateTime.UtcNow;

        var failed = new OutboundBundle(Guid.NewGuid(), "Patient", "p-x", "default", "{}", nowUtc);
        failed.MarkAttemptFailed("503", nowUtc.AddHours(1), maxAttempts: 1);
        failed.Status.ShouldBe(OutboundBundleStatus.Failed);
        var bundleId = failed.Id;
        db.OutboundBundles.Add(failed);
        await db.SaveChangesAsync();

        var gateway = sp.GetRequiredService<ICqrsGateway>();
        await gateway.SendCommandAsync<RetryOutboundBundleCommand, Unit>(
            new RetryOutboundBundleCommand(bundleId), CancellationToken.None);

        var reloaded = db.OutboundBundles.Single(b => b.Id == bundleId);
        reloaded.Status.ShouldBe(OutboundBundleStatus.Pending);
        reloaded.NextAttemptAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        reloaded.Attempts.ShouldBe(1, "Retry preserves the previous attempt count as audit history.");
    }

    [Fact]
    public async Task Retryoutboundbundle_Is_NoOp_For_Delivered_Bundles_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<HieDbContext>();
        var nowUtc = DateTime.UtcNow;

        var delivered = new OutboundBundle(Guid.NewGuid(), "Patient", "p-y", "default", "{}", nowUtc);
        delivered.MarkDelivered(nowUtc);
        var bundleId = delivered.Id;
        db.OutboundBundles.Add(delivered);
        await db.SaveChangesAsync();

        var gateway = sp.GetRequiredService<ICqrsGateway>();
        await gateway.SendCommandAsync<RetryOutboundBundleCommand, Unit>(
            new RetryOutboundBundleCommand(bundleId), CancellationToken.None);

        var reloaded = db.OutboundBundles.Single(b => b.Id == bundleId);
        reloaded.Status.ShouldBe(OutboundBundleStatus.Delivered, "Delivered bundles are immutable terminal state.");
    }
}
