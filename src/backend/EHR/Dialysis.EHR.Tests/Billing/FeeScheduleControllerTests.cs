using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Api.Controllers.V1;
using Dialysis.EHR.Billing.Consumers;
using Dialysis.EHR.Billing.Ports;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Behavioural coverage for the operator fee-schedule admin surface: create → list → revise →
/// delete, plus validation guards. Uses the in-memory admin repository so no Postgres is needed.
/// </summary>
public sealed class FeeScheduleControllerTests
{
    private static (FeeScheduleController Controller, ICptFeeScheduleAdminRepository Repo) NewController()
    {
        var repo = new InMemoryCptFeeScheduleAdminRepository();
        return (new FeeScheduleController(repo, new FakeUnitOfWork()), repo);
    }

    private static UpsertFeeScheduleRequest Request(
        string cpt = "90935", string payer = "MED01", decimal amount = 250m,
        DateOnly? from = null, DateOnly? until = null) =>
        new(cpt, payer, amount, "USD", from ?? new DateOnly(2025, 1, 1), until);

    [Fact]
    public async Task Create_Then_List_Round_Trips_The_Row_Async()
    {
        var (controller, _) = NewController();

        var created = await controller.CreateAsync(Request(), CancellationToken.None);
        created.ShouldBeOfType<CreatedResult>();

        var list = await controller.ListAsync(null, null, CancellationToken.None);
        var rows = ((OkObjectResult)list).Value.ShouldBeAssignableTo<IReadOnlyList<FeeScheduleRow>>()!;
        rows.Count.ShouldBe(1);
        rows[0].CptCode.ShouldBe("90935");
        rows[0].PayerCode.ShouldBe("MED01");
        rows[0].Amount.ShouldBe(250m);
    }

    [Fact]
    public async Task List_Filters_By_Cpt_And_Payer_Async()
    {
        var (controller, _) = NewController();
        await controller.CreateAsync(Request(cpt: "90935", payer: "MED01"), CancellationToken.None);
        await controller.CreateAsync(Request(cpt: "90937", payer: "MED02"), CancellationToken.None);
        await controller.CreateAsync(Request(cpt: "90935", payer: "*"), CancellationToken.None);

        var byCpt = (IReadOnlyList<FeeScheduleRow>)((OkObjectResult)
            await controller.ListAsync("90935", null, CancellationToken.None)).Value!;
        byCpt.Count.ShouldBe(2);

        var byBoth = (IReadOnlyList<FeeScheduleRow>)((OkObjectResult)
            await controller.ListAsync("90935", "MED01", CancellationToken.None)).Value!;
        byBoth.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Revise_Updates_Amount_And_Window_Async()
    {
        var (controller, repo) = NewController();
        await controller.CreateAsync(Request(amount: 250m), CancellationToken.None);
        var id = (await repo.ListAsync(null, null, CancellationToken.None))[0].Id;

        var revised = await controller.ReviseAsync(
            id, Request(amount: 275m, until: new DateOnly(2026, 12, 31)), CancellationToken.None);

        var row = ((OkObjectResult)revised).Value.ShouldBeOfType<FeeScheduleRow>();
        row.Amount.ShouldBe(275m);
        row.EffectiveUntilUtc.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public async Task Revise_Missing_Row_Returns_Not_Found_Async()
    {
        var (controller, _) = NewController();
        var result = await controller.ReviseAsync(Guid.NewGuid(), Request(), CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_With_Inverted_Window_Returns_Bad_Request_Async()
    {
        var (controller, _) = NewController();
        var result = await controller.CreateAsync(
            Request(from: new DateOnly(2025, 6, 1), until: new DateOnly(2025, 1, 1)),
            CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_Removes_The_Row_Async()
    {
        var (controller, repo) = NewController();
        await controller.CreateAsync(Request(), CancellationToken.None);
        var id = (await repo.ListAsync(null, null, CancellationToken.None))[0].Id;

        var result = await controller.DeleteAsync(id, CancellationToken.None);
        result.ShouldBeOfType<NoContentResult>();

        (await repo.ListAsync(null, null, CancellationToken.None)).Count.ShouldBe(0);
    }

    [Fact]
    public async Task Delete_Missing_Row_Returns_Not_Found_Async()
    {
        var (controller, _) = NewController();
        var result = await controller.DeleteAsync(Guid.NewGuid(), CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
