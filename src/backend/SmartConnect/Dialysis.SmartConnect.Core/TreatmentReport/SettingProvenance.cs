namespace Dialysis.SmartConnect.TreatmentReport;

/// <summary>
/// Tracks how a single setting's <see cref="ObservationSource"/> evolves over the life
/// of a treatment session. Enforces the IG §5 one-way transition rule:
/// <c>RSET → MSET</c> or <c>RSET → ASET</c> are allowed; once a setting has left
/// <see cref="ObservationSource.RemoteSetting"/>, it never goes back, even if the user
/// restores the original value.
/// </summary>
/// <remarks>
/// Wire-out integrators construct one of these per setting field at the moment the
/// EMR prescription is applied (initial state = <c>RemoteSetting</c>), then call
/// <see cref="MarkManualOverride"/> or <see cref="MarkAutoOverride"/> when the machine
/// observes a local change. The current state drives OBX-17 on every subsequent
/// PCD-01 report — that's the round-trip §6 references.
/// </remarks>
public sealed class SettingProvenance
{
    private ObservationSource _current;

    public SettingProvenance(ObservationSource initial)
    {
        if (!initial.IsSetting())
            throw new ArgumentException(
                $"SettingProvenance is for control values; {initial} is a measurement source.",
                nameof(initial));
        _current = initial;
    }

    public ObservationSource Current => _current;

    /// <summary>
    /// Records a user-driven change. No-op if the state is already
    /// <see cref="ObservationSource.ManualSetting"/>; rejected on
    /// <see cref="ObservationSource.AutoSetting"/> per the one-way IG rule (once
    /// auto-control owns the setting, only auto-control can move it).
    /// </summary>
    public void MarkManualOverride()
    {
        switch (_current)
        {
            case ObservationSource.RemoteSetting:
            case ObservationSource.ManualSetting:
                _current = ObservationSource.ManualSetting;
                return;
            case ObservationSource.AutoSetting:
                // IG §5 closing paragraph: provenance flips RSET → {MSET,ASET} are one-way.
                // It does not specify {MSET ↔ ASET} transitions; the safest read is that
                // once ASET owns the setting, only the auto-controller can release it.
                throw new InvalidOperationException(
                    "Cannot transition from AutoSetting to ManualSetting — auto-controller owns the setting.");
        }
    }

    /// <summary>Records an auto-controller change. Idempotent on AutoSetting.</summary>
    public void MarkAutoOverride()
    {
        switch (_current)
        {
            case ObservationSource.RemoteSetting:
            case ObservationSource.ManualSetting:
            case ObservationSource.AutoSetting:
                _current = ObservationSource.AutoSetting;
                return;
        }
    }
}
