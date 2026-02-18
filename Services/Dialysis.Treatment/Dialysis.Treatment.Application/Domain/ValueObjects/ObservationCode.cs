namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// Strongly-typed MDC observation code from IEEE 11073 nomenclature (e.g. MDC_PRESS_BLD_SYS).
/// Maps to OBX-3 (Observation Identifier) in HL7 v2.
/// </summary>
public readonly record struct ObservationCode
{
    public string Value { get; }

    public ObservationCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ObservationCode SystolicBp = new("MDC_PRESS_BLD_SYS");
    public static readonly ObservationCode DiastolicBp = new("MDC_PRESS_BLD_DIA");
    public static readonly ObservationCode HeartRate = new("MDC_PULS_RATE");
    public static readonly ObservationCode VenousPressure = new("MDC_PRESS_BLD_VEN");
    public static readonly ObservationCode ArterialPressure = new("MDC_PRESS_BLD_ART");

    public override string ToString() => Value;

    public static implicit operator string(ObservationCode code) => code.Value;
    public static explicit operator ObservationCode(string value) => new(value);
}
