using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests;

/// <summary>
/// Regression for the transactional-outbox wiring. PDMS aggregates raise integration events via
/// <c>RaiseIntegrationEvent</c>; persisting an aggregate MUST drain those events into the Transponder
/// outbox in the SAME transaction as the state change, so the relay can publish them and the
/// reporting / HIE consumers can react.
///
/// Before <see cref="Dialysis.DomainDrivenDesign.Persistence.IntegrationEventOutboxSaveChangesInterceptor"/>
/// was wired into <c>PdmsDbContext</c>, these events were only appended to the aggregate's in-memory
/// list and silently dropped — never outboxed, relayed, or consumed (so e.g. completing a session
/// never produced its discharge letter / billing document). This test drives a session through its
/// full lifecycle (Schedule → Start → Complete) at the aggregate level and asserts the
/// <c>DialysisSessionCompletedIntegrationEvent</c> lands in the outbox on SaveChanges.
/// </summary>
[Collection(nameof(PdmsPostgresFixtureCollection))]
public sealed class SessionCompletionOutboxTests
{
    private readonly PdmsApiWebApplicationFactory _factory;
    public SessionCompletionOutboxTests(PdmsApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Completing_A_Session_Drains_The_Integration_Event_To_The_Outbox_Async()
    {
        var startUtc = DateTime.UtcNow;

        var prescription = new SessionPrescription(
            dialyzerModel: "FX CorDiax 80",
            prescribedDurationMinutes: 240,
            bloodFlowRateMlPerMin: 400,
            dialysateFlowRateMlPerMin: 500,
            dialysatePotassiumMmolPerL: 2.0m,
            dialysateCalciumMmolPerL: 1.5m,
            dialysateSodiumMmolPerL: 138m,
            targetUfVolumeLiters: 3.0m,
            anticoagulationProtocolCode: "HEPARIN");

        var access = new VascularAccess(
            VascularAccessKind.ArteriovenousFistula,
            "Left upper arm",
            DateOnly.FromDateTime(startUtc.AddYears(-1)));

        var session = DialysisSession.Schedule(
            id: Guid.CreateVersion7(),
            patientId: Guid.CreateVersion7(),
            scheduledStartUtc: startUtc,
            prescription: prescription,
            access: access);

        session.Start(startUtc);
        session.Complete(startUtc.AddHours(4), achievedUfVolumeLiters: 2.9m);

        using (var writeScope = _factory.Services.CreateScope())
        {
            var db = writeScope.ServiceProvider.GetRequiredService<PdmsDbContext>();
            db.Sessions.Add(session);
            // The outbox interceptor runs here, draining session.IntegrationEvents (including
            // DialysisSessionCompletedIntegrationEvent) into the outbox table in the same transaction.
            await db.SaveChangesAsync(CancellationToken.None);
        }

        using var assertScope = _factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<PdmsDbContext>();
        var completedEventRows = await assertDb.OutboxMessages.AsNoTracking()
            .CountAsync(
                m => m.AssemblyQualifiedEventType.Contains("DialysisSessionCompletedIntegrationEvent"),
                CancellationToken.None);

        completedEventRows.ShouldBeGreaterThanOrEqualTo(1);
    }
}
