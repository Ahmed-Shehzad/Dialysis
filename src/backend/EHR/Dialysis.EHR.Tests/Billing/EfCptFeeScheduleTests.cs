using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Persistence.Billing;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Resolution-priority tests for <see cref="EfCptFeeSchedule"/>. The order — exact payer
/// before wildcard, then latest effective-from date — drives what amount a charge consumer
/// actually books; getting it wrong silently underbills the patient.
/// </summary>
public sealed class EfCptFeeScheduleTests
{
    private static readonly TimeProvider _clock =
        TimeProvider.System; // Wall-clock; tests pin effective dates to "before today".

    [Fact]
    public async Task Exact_Payer_Match_Wins_Over_Wildcard_Async()
    {
        await using var ctx = NewContext();
        await ctx.Set<CptFeeScheduleEntry>().AddRangeAsync(
            new CptFeeScheduleEntry(Guid.NewGuid(), "90935", "*", new Money(100m, "USD"),
                new DateOnly(2020, 1, 1)),
            new CptFeeScheduleEntry(Guid.NewGuid(), "90935", "MED01", new Money(250m, "USD"),
                new DateOnly(2020, 1, 1)));
        await ctx.SaveChangesAsync();

        var schedule = new EfCptFeeSchedule(ctx, _clock);
        var amount = await schedule.LookupAsync("90935", CancellationToken.None);

        amount.Amount.ShouldBe(250m);
    }

    [Fact]
    public async Task Latest_Effective_Date_Wins_Within_The_Same_Payer_Async()
    {
        await using var ctx = NewContext();
        await ctx.Set<CptFeeScheduleEntry>().AddRangeAsync(
            new CptFeeScheduleEntry(Guid.NewGuid(), "90935", "MED01", new Money(180m, "USD"),
                new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31)),
            new CptFeeScheduleEntry(Guid.NewGuid(), "90935", "MED01", new Money(220m, "USD"),
                new DateOnly(2025, 1, 1)));
        await ctx.SaveChangesAsync();

        var schedule = new EfCptFeeSchedule(ctx, _clock);
        var amount = await schedule.LookupAsync("90935", CancellationToken.None);

        amount.Amount.ShouldBe(220m);
    }

    [Fact]
    public async Task Missing_Row_Throws_Useful_Diagnostic_Async()
    {
        await using var ctx = NewContext();
        var schedule = new EfCptFeeSchedule(ctx, _clock);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => schedule.LookupAsync("99999", CancellationToken.None));
        ex.Message.ShouldContain("99999");
    }

    private static StubDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<StubDbContext>()
            .UseNpgsql(EhrTestPostgres.NewDatabaseConnectionString())
            .Options;
        var context = new StubDbContext(options);
        EhrTestPostgres.EnsureCreated(context.Database);
        return context;
    }

    private sealed class StubDbContext : DbContext
    {
        public StubDbContext(DbContextOptions<StubDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CptFeeScheduleEntry>(b =>
            {
                b.HasKey(e => e.Id);
                b.OwnsOne(e => e.Amount, m =>
                {
                    m.Property(x => x.Amount).HasColumnName("Amount");
                    m.Property(x => x.CurrencyCode).HasColumnName("CurrencyCode");
                });
            });
        }
    }
}
