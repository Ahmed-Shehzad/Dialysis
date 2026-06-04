namespace Dialysis.SmartConnect.TreatmentReport;

/// <summary>
/// One observation row for a PCD-01 treatment report (ORU^R40). Carries the data the
/// machine reports back to the EMR with its provenance.
/// </summary>
/// <remarks>
/// Field map onto OBX (IG §8.2.5):
/// <list type="bullet">
///   <item>OBX-2 = <see cref="ValueType"/> (<c>NM</c> for numeric, <c>ST</c> for
///         string-coded).</item>
///   <item>OBX-3 = <see cref="ObservationId"/> (CWE like
///         <c>16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC</c>).</item>
///   <item>OBX-4 = <see cref="ContainmentPath"/> (e.g. <c>1.1.3.1</c>).</item>
///   <item>OBX-5 = <see cref="Value"/>.</item>
///   <item>OBX-6 = <see cref="Units"/> (UCUM CWE like
///         <c>ml/min^ml/min^UCUM</c>; null/empty for ST values).</item>
///   <item>OBX-11 = result status — always <c>F</c> for final per IG samples.</item>
///   <item>OBX-17 = <see cref="Source"/> rendered via
///         <see cref="ObservationSourceExtensions.ToObx17Cwe"/>. REQUIRED for
///         settings, optional for measurements — the wire serialiser respects that.</item>
/// </list>
/// </remarks>
public sealed record ObservationFrame
{
    /// <summary>
    /// One observation row for a PCD-01 treatment report (ORU^R40). Carries the data the
    /// machine reports back to the EMR with its provenance.
    /// </summary>
    /// <remarks>
    /// Field map onto OBX (IG §8.2.5):
    /// <list type="bullet">
    ///   <item>OBX-2 = <see cref="ValueType"/> (<c>NM</c> for numeric, <c>ST</c> for
    ///         string-coded).</item>
    ///   <item>OBX-3 = <see cref="ObservationId"/> (CWE like
    ///         <c>16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC</c>).</item>
    ///   <item>OBX-4 = <see cref="ContainmentPath"/> (e.g. <c>1.1.3.1</c>).</item>
    ///   <item>OBX-5 = <see cref="Value"/>.</item>
    ///   <item>OBX-6 = <see cref="Units"/> (UCUM CWE like
    ///         <c>ml/min^ml/min^UCUM</c>; null/empty for ST values).</item>
    ///   <item>OBX-11 = result status — always <c>F</c> for final per IG samples.</item>
    ///   <item>OBX-17 = <see cref="Source"/> rendered via
    ///         <see cref="ObservationSourceExtensions.ToObx17Cwe"/>. REQUIRED for
    ///         settings, optional for measurements — the wire serialiser respects that.</item>
    /// </list>
    /// </remarks>
    public ObservationFrame(string ValueType,
        string ObservationId,
        string ContainmentPath,
        string Value,
        string? Units,
        ObservationSource? Source)
    {
        this.ValueType = ValueType;
        this.ObservationId = ObservationId;
        this.ContainmentPath = ContainmentPath;
        this.Value = Value;
        this.Units = Units;
        this.Source = Source;
    }
    public string ValueType { get; init; }
    public string ObservationId { get; init; }
    public string ContainmentPath { get; init; }
    public string Value { get; init; }
    public string? Units { get; init; }
    public ObservationSource? Source { get; init; }
    public void Deconstruct(out string ValueType, out string ObservationId, out string ContainmentPath, out string Value, out string? Units, out ObservationSource? Source)
    {
        ValueType = this.ValueType;
        ObservationId = this.ObservationId;
        ContainmentPath = this.ContainmentPath;
        Value = this.Value;
        Units = this.Units;
        Source = this.Source;
    }
}
