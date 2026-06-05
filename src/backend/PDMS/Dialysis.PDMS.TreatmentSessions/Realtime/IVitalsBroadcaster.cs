namespace Dialysis.PDMS.TreatmentSessions.Realtime;

public sealed record VitalsReadingSnapshot
{
    public VitalsReadingSnapshot(Guid ReadingId,
        Guid SessionId,
        DateTime ObservedAtUtc,
        int SystolicBloodPressure,
        int DiastolicBloodPressure,
        int HeartRateBpm,
        decimal ArterialPressureMmHg,
        decimal VenousPressureMmHg,
        decimal UltrafiltrationRateMlPerHour,
        decimal ConductivityMsPerCm,
        string? Notes)
    {
        this.ReadingId = ReadingId;
        this.SessionId = SessionId;
        this.ObservedAtUtc = ObservedAtUtc;
        this.SystolicBloodPressure = SystolicBloodPressure;
        this.DiastolicBloodPressure = DiastolicBloodPressure;
        this.HeartRateBpm = HeartRateBpm;
        this.ArterialPressureMmHg = ArterialPressureMmHg;
        this.VenousPressureMmHg = VenousPressureMmHg;
        this.UltrafiltrationRateMlPerHour = UltrafiltrationRateMlPerHour;
        this.ConductivityMsPerCm = ConductivityMsPerCm;
        this.Notes = Notes;
    }
    public Guid ReadingId { get; init; }
    public Guid SessionId { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public int SystolicBloodPressure { get; init; }
    public int DiastolicBloodPressure { get; init; }
    public int HeartRateBpm { get; init; }
    public decimal ArterialPressureMmHg { get; init; }
    public decimal VenousPressureMmHg { get; init; }
    public decimal UltrafiltrationRateMlPerHour { get; init; }
    public decimal ConductivityMsPerCm { get; init; }
    public string? Notes { get; init; }
    public void Deconstruct(out Guid ReadingId, out Guid SessionId, out DateTime ObservedAtUtc, out int SystolicBloodPressure, out int DiastolicBloodPressure, out int HeartRateBpm, out decimal ArterialPressureMmHg, out decimal VenousPressureMmHg, out decimal UltrafiltrationRateMlPerHour, out decimal ConductivityMsPerCm, out string? Notes)
    {
        ReadingId = this.ReadingId;
        SessionId = this.SessionId;
        ObservedAtUtc = this.ObservedAtUtc;
        SystolicBloodPressure = this.SystolicBloodPressure;
        DiastolicBloodPressure = this.DiastolicBloodPressure;
        HeartRateBpm = this.HeartRateBpm;
        ArterialPressureMmHg = this.ArterialPressureMmHg;
        VenousPressureMmHg = this.VenousPressureMmHg;
        UltrafiltrationRateMlPerHour = this.UltrafiltrationRateMlPerHour;
        ConductivityMsPerCm = this.ConductivityMsPerCm;
        Notes = this.Notes;
    }
}

/// <summary>One itemised line of a live cost snapshot (decoupled from EHR billing types).</summary>
public sealed record SessionCostLineSnapshot
{
    /// <summary>One itemised line of a live cost snapshot (decoupled from EHR billing types).</summary>
    public SessionCostLineSnapshot(string Label, decimal Quantity, string Unit, decimal UnitPrice, decimal Amount)
    {
        this.Label = Label;
        this.Quantity = Quantity;
        this.Unit = Unit;
        this.UnitPrice = UnitPrice;
        this.Amount = Amount;
    }
    public string Label { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public void Deconstruct(out string Label, out decimal Quantity, out string Unit, out decimal UnitPrice, out decimal Amount)
    {
        Label = this.Label;
        Quantity = this.Quantity;
        Unit = this.Unit;
        UnitPrice = this.UnitPrice;
        Amount = this.Amount;
    }
}

/// <summary>
/// Running cost estimate for an in-progress session, pushed to the chairside over the same
/// SignalR hub as vitals (message name <c>"cost"</c>). It is an <em>estimate</em>: UF volume
/// is prorated from the prescription until the session completes and EHR captures the
/// authoritative charge.
/// </summary>
public sealed record SessionCostSnapshot
{
    /// <summary>
    /// Running cost estimate for an in-progress session, pushed to the chairside over the same
    /// SignalR hub as vitals (message name <c>"cost"</c>).
    /// </summary>
    public SessionCostSnapshot(Guid SessionId,
        string CurrencyCode,
        decimal Total,
        int ElapsedMinutes,
        DateTime AsOfUtc,
        IReadOnlyList<SessionCostLineSnapshot> Lines)
    {
        this.SessionId = SessionId;
        this.CurrencyCode = CurrencyCode;
        this.Total = Total;
        this.ElapsedMinutes = ElapsedMinutes;
        this.AsOfUtc = AsOfUtc;
        this.Lines = Lines;
    }
    public Guid SessionId { get; init; }
    public string CurrencyCode { get; init; }
    public decimal Total { get; init; }
    public int ElapsedMinutes { get; init; }
    public DateTime AsOfUtc { get; init; }
    public IReadOnlyList<SessionCostLineSnapshot> Lines { get; init; }
    public void Deconstruct(out Guid SessionId, out string CurrencyCode, out decimal Total, out int ElapsedMinutes, out DateTime AsOfUtc, out IReadOnlyList<SessionCostLineSnapshot> Lines)
    {
        SessionId = this.SessionId;
        CurrencyCode = this.CurrencyCode;
        Total = this.Total;
        ElapsedMinutes = this.ElapsedMinutes;
        AsOfUtc = this.AsOfUtc;
        Lines = this.Lines;
    }
}

public interface IVitalsBroadcaster
{
    Task BroadcastAsync(VitalsReadingSnapshot reading, CancellationToken cancellationToken);

    /// <summary>Pushes a running cost estimate to the session's subscribers (message <c>"cost"</c>).</summary>
    Task BroadcastCostAsync(SessionCostSnapshot cost, CancellationToken cancellationToken);
}
