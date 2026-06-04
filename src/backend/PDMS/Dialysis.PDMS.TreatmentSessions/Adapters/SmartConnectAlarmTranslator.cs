using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.SmartConnect.Contracts.Integration;

namespace Dialysis.PDMS.TreatmentSessions.Adapters;

/// <summary>
/// Anticorruption Layer (Evans pp. 258–260) at the SmartConnect ↔ PDMS boundary: translates
/// <see cref="DialysisMachineAlarmIntegrationEvent"/> (Published Language from the upstream SmartConnect
/// context) into a PDMS-local <see cref="IncomingAlarm"/> intent that uses PDMS' own vocabulary
/// (<see cref="TreatmentAlarmState"/>). No downstream code touches the SmartConnect event type past this layer.
/// </summary>
public static class SmartConnectAlarmTranslator
{
    public static IncomingAlarm Translate(DialysisMachineAlarmIntegrationEvent message) =>
        new(
            MachineSerial: message.MachineSerial,
            AlarmCode: message.AlarmCode,
            AlarmSource: message.AlarmSource,
            AlarmPhase: message.AlarmPhase,
            State: MapState(message.State),
            ObservedAtUtc: message.ObservedAtUtc,
            MessageControlId: message.MessageControlId);

    private static TreatmentAlarmState MapState(DialysisMachineAlarmState state) =>
        state switch
        {
            DialysisMachineAlarmState.Present => TreatmentAlarmState.Present,
            DialysisMachineAlarmState.Inactivating => TreatmentAlarmState.Inactivating,
            DialysisMachineAlarmState.Resolved => TreatmentAlarmState.Resolved,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown SmartConnect alarm state."),
        };
}

/// <summary>
/// PDMS-local intent translated from a SmartConnect alarm event. Carries everything the alarm pipeline
/// needs to resolve a machine, find-or-raise a <see cref="TreatmentAlarm"/>, and persist the state change.
/// </summary>
public sealed record IncomingAlarm
{
    /// <summary>
    /// PDMS-local intent translated from a SmartConnect alarm event. Carries everything the alarm pipeline
    /// needs to resolve a machine, find-or-raise a <see cref="TreatmentAlarm"/>, and persist the state change.
    /// </summary>
    public IncomingAlarm(string MachineSerial,
        long AlarmCode,
        string? AlarmSource,
        string? AlarmPhase,
        TreatmentAlarmState State,
        DateTime ObservedAtUtc,
        string MessageControlId)
    {
        this.MachineSerial = MachineSerial;
        this.AlarmCode = AlarmCode;
        this.AlarmSource = AlarmSource;
        this.AlarmPhase = AlarmPhase;
        this.State = State;
        this.ObservedAtUtc = ObservedAtUtc;
        this.MessageControlId = MessageControlId;
    }
    public string MachineSerial { get; init; }
    public long AlarmCode { get; init; }
    public string? AlarmSource { get; init; }
    public string? AlarmPhase { get; init; }
    public TreatmentAlarmState State { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public string MessageControlId { get; init; }
    public void Deconstruct(out string MachineSerial, out long AlarmCode, out string? AlarmSource, out string? AlarmPhase, out TreatmentAlarmState State, out DateTime ObservedAtUtc, out string MessageControlId)
    {
        MachineSerial = this.MachineSerial;
        AlarmCode = this.AlarmCode;
        AlarmSource = this.AlarmSource;
        AlarmPhase = this.AlarmPhase;
        State = this.State;
        ObservedAtUtc = this.ObservedAtUtc;
        MessageControlId = this.MessageControlId;
    }
}
