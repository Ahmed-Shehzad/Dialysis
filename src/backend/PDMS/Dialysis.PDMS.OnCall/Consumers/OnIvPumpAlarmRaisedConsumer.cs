using Dialysis.BuildingBlocks.ClinicianNotification;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.OnCall.Domain;
using Dialysis.PDMS.OnCall.Dispatch;

namespace Dialysis.PDMS.OnCall.Consumers;

/// <summary>
/// Listens for <see cref="IvPumpAlarmRaisedIntegrationEvent"/> and walks the on-call chain
/// for the affected chair. First chain link gets paged immediately; subsequent links escalate
/// per the active <see cref="EscalationPolicy"/> if the alarm stays unacknowledged.
///
/// Persists an <see cref="AlarmDispatch"/> aggregate that records every send + outcome so the
/// operator audit page (and the GDPR audit gate) can show who got paged, on which channel,
/// and how the chain progressed.
/// </summary>
public sealed class OnIvPumpAlarmRaisedConsumer : IConsumer<IvPumpAlarmRaisedIntegrationEvent>
{
    private readonly IOnCallRotationLookup _rotations;
    private readonly IEscalationPolicyLookup _policies;
    private readonly IClinicianNotificationDispatcher _dispatcher;
    private readonly IAlarmDispatchRepository _dispatches;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    /// <summary>
    /// Listens for <see cref="IvPumpAlarmRaisedIntegrationEvent"/> and walks the on-call chain
    /// for the affected chair. First chain link gets paged immediately; subsequent links escalate
    /// per the active <see cref="EscalationPolicy"/> if the alarm stays unacknowledged.
    ///
    /// Persists an <see cref="AlarmDispatch"/> aggregate that records every send + outcome so the
    /// operator audit page (and the GDPR audit gate) can show who got paged, on which channel,
    /// and how the chain progressed.
    /// </summary>
    public OnIvPumpAlarmRaisedConsumer(IOnCallRotationLookup rotations,
        IEscalationPolicyLookup policies,
        IClinicianNotificationDispatcher dispatcher,
        IAlarmDispatchRepository dispatches,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _rotations = rotations;
        _policies = policies;
        _dispatcher = dispatcher;
        _dispatches = dispatches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task HandleAsync(ConsumeContext<IvPumpAlarmRaisedIntegrationEvent> context)
    {
        var alarm = context.Message;
        var rotation = await _rotations.FindActiveAsync(alarm.ChairId, alarm.RaisedAtUtc, context.CancellationToken)
            .ConfigureAwait(false);
        if (rotation is null)
        {
            // No rotation configured — fall back to the operator's out-of-band paging tree.
            return;
        }
        var policy = await _policies.FindActiveAsync(context.CancellationToken).ConfigureAwait(false)
            ?? EscalationPolicy.CreateDefault(Guid.CreateVersion7());

        var now = _clock.GetUtcNow().UtcDateTime;
        var dispatch = new AlarmDispatch(
            id: Guid.CreateVersion7(),
            infusionId: alarm.InfusionId,
            sessionId: alarm.SessionId,
            chairId: alarm.ChairId,
            alarmCode: alarm.AlarmCode,
            severity: alarm.Severity,
            startedAtUtc: now,
            rotationId: rotation.Id,
            policyId: policy.Id);

        var primary = rotation.LinkForAttempt(0);
        if (primary is null)
        {
            dispatch.MarkExhausted(now);
            await _dispatches.AddAsync(dispatch, context.CancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            return;
        }

        var requests = primary.Channels.Select(target => new ClinicianNotificationRequest(
            Channel: ChannelCodeFor(target.Channel),
            Address: target.Address,
            Subject: $"Pump alarm: {alarm.AlarmCode}",
            // PHI-minimised body per GDPR Art. 5(1)(c) — no patient name / MRN.
            Body: $"Chair alarm: {alarm.AlarmText}. Acknowledge in the app.",
            DeepLink: null,
            Priority: alarm.Severity == IvPumpAlarmSeverity.Critical
                ? NotificationPriority.Critical
                : alarm.Severity == IvPumpAlarmSeverity.Warning
                    ? NotificationPriority.High
                    : NotificationPriority.Normal,
            Metadata: new Dictionary<string, string>
            {
                ["chairId"] = alarm.ChairId.ToString(),
                ["alarmCode"] = alarm.AlarmCode,
                ["severity"] = alarm.Severity.ToString(),
            })).ToArray();

        var outcomes = await _dispatcher.DispatchAsync(requests, context.CancellationToken).ConfigureAwait(false);
        foreach (var outcome in outcomes)
        {
            dispatch.RecordAttempt(
                channel: ChannelEnumFromCode(outcome.Channel),
                address: outcome.Address,
                delivered: outcome.Result.Delivered,
                failureReason: outcome.Result.FailureReason,
                attemptedAtUtc: _clock.GetUtcNow().UtcDateTime);
        }

        await _dispatches.AddAsync(dispatch, context.CancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }

    private static string ChannelCodeFor(NotificationChannel channel) => channel switch
    {
        NotificationChannel.Sms => "sms",
        NotificationChannel.PushApns => "push.apns",
        NotificationChannel.PushFcm => "push.fcm",
        NotificationChannel.Email => "email",
        NotificationChannel.Voice => "voice",
        _ => "webhook",
    };

    private static NotificationChannel ChannelEnumFromCode(string code) => code switch
    {
        "sms" => NotificationChannel.Sms,
        "push.apns" => NotificationChannel.PushApns,
        "push.fcm" => NotificationChannel.PushFcm,
        "email" => NotificationChannel.Email,
        "voice" => NotificationChannel.Voice,
        _ => NotificationChannel.Email,
    };
}
