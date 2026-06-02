using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.OnCall.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.OnCall;

/// <summary>
/// Aggregate-only tests on <see cref="AlarmDispatch"/>. The audit page reads the attempt
/// timeline straight from the aggregate, so the state transitions and attempt recording
/// must round-trip cleanly.
/// </summary>
public sealed class AlarmDispatchTests
{
    private static AlarmDispatch Fresh() => new(
        id: Guid.NewGuid(),
        infusionId: Guid.NewGuid(),
        sessionId: Guid.NewGuid(),
        chairId: Guid.NewGuid(),
        alarmCode: "OCCLUSION_DISTAL",
        severity: IvPumpAlarmSeverity.Critical,
        startedAtUtc: new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        rotationId: Guid.NewGuid(),
        policyId: Guid.NewGuid());

    [Fact]
    public void Recording_A_Successful_Attempt_Moves_To_Awaiting_Acknowledgement()
    {
        var dispatch = Fresh();

        dispatch.RecordAttempt(NotificationChannel.Sms, "+1234", delivered: true, failureReason: null,
            attemptedAtUtc: new DateTime(2026, 6, 1, 12, 0, 1, DateTimeKind.Utc));

        dispatch.Status.ShouldBe(AlarmDispatchStatus.AwaitingAcknowledgement);
        dispatch.Attempts.Count.ShouldBe(1);
    }

    [Fact]
    public void Recording_A_Failed_Attempt_Stays_Pending()
    {
        var dispatch = Fresh();

        dispatch.RecordAttempt(NotificationChannel.Sms, "+1234", delivered: false, failureReason: "HTTP 503",
            attemptedAtUtc: new DateTime(2026, 6, 1, 12, 0, 1, DateTimeKind.Utc));

        dispatch.Status.ShouldBe(AlarmDispatchStatus.Pending);
        dispatch.Attempts.Single().FailureReason.ShouldBe("HTTP 503");
    }

    [Fact]
    public void Acknowledgement_Terminates_The_Dispatch()
    {
        var dispatch = Fresh();
        dispatch.RecordAttempt(NotificationChannel.Sms, "+1234", true, null, new DateTime(2026, 6, 1, 12, 0, 1, DateTimeKind.Utc));

        dispatch.Acknowledge("nurse-1", new DateTime(2026, 6, 1, 12, 0, 30, DateTimeKind.Utc));

        dispatch.Status.ShouldBe(AlarmDispatchStatus.Acknowledged);
        dispatch.AcknowledgedBySub.ShouldBe("nurse-1");
        dispatch.ResolvedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Escalation_Increments_The_Chain_Index()
    {
        var dispatch = Fresh();

        dispatch.EscalateToNextLink();

        dispatch.CurrentLinkIndex.ShouldBe(1);
    }

    [Fact]
    public void Exhausted_Dispatch_Will_Not_Escalate_Further()
    {
        var dispatch = Fresh();
        dispatch.MarkExhausted(new DateTime(2026, 6, 1, 12, 5, 0, DateTimeKind.Utc));

        dispatch.EscalateToNextLink();

        dispatch.CurrentLinkIndex.ShouldBe(0);
        dispatch.Status.ShouldBe(AlarmDispatchStatus.Exhausted);
    }
}
