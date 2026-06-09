using Dialysis.CQRS;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Features.RequestAppointment;
using Dialysis.EHR.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.PatientPortal;

/// <summary>
/// Regression for the duplicate appointment-requests bug: submission must be idempotent. A patient
/// (or the dev data-simulator, or a double-tapping client) filing the same request again — same reason
/// and preferred window — while an earlier one is still Pending must return the existing request, not
/// stack a second identical row on the staff worklist. Duplicate rows were the root cause of the
/// approve/decline 409s (each duplicate booked the same demo provider + slot, so the second approval
/// overlapped).
/// </summary>
[Collection(nameof(EhrFixtureCollection))]
public sealed class PortalAppointmentRequestIdempotencyTests
{
    private readonly EhrApiWebApplicationFactory _factory;
    public PortalAppointmentRequestIdempotencyTests(EhrApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Resubmitting_The_Same_Open_Request_Returns_The_Existing_One_Async()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var patientId = Guid.NewGuid();
        // Fixed future, sub-second-clean instants so the dedup's exact-window match isn't at the mercy of
        // timestamp precision round-tripping.
        var earliest = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        var latest = earliest.AddDays(7);

        var first = await gateway.SendCommandAsync<RequestAppointmentCommand, Guid>(
            new RequestAppointmentCommand(patientId, "Routine follow-up", earliest, latest), CancellationToken.None);

        // Same patient, same reason, same window — must be deduped to the first request.
        var second = await gateway.SendCommandAsync<RequestAppointmentCommand, Guid>(
            new RequestAppointmentCommand(patientId, "  Routine follow-up  ", earliest, latest), CancellationToken.None);

        second.ShouldBe(first);

        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
        var rows = await db.PortalAppointmentRequests.AsNoTracking()
            .CountAsync(r => r.PatientId == patientId, CancellationToken.None);
        rows.ShouldBe(1);
    }

    [Fact]
    public async Task A_Different_Window_Is_Not_Treated_As_A_Duplicate_Async()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var patientId = Guid.NewGuid();
        var earliest = new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc);

        var first = await gateway.SendCommandAsync<RequestAppointmentCommand, Guid>(
            new RequestAppointmentCommand(patientId, "Routine follow-up", earliest, earliest.AddDays(7)), CancellationToken.None);
        // Same reason but a genuinely different preferred slot — a distinct request, not a duplicate.
        var second = await gateway.SendCommandAsync<RequestAppointmentCommand, Guid>(
            new RequestAppointmentCommand(patientId, "Routine follow-up", earliest.AddDays(1), earliest.AddDays(8)), CancellationToken.None);

        second.ShouldNotBe(first);

        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
        var rows = await db.PortalAppointmentRequests.AsNoTracking()
            .CountAsync(r => r.PatientId == patientId && r.Status == PortalAppointmentRequestStatus.Pending, CancellationToken.None);
        rows.ShouldBe(2);
    }
}
