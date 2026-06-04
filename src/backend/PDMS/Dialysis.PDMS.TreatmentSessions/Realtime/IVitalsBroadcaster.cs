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

public interface IVitalsBroadcaster
{
    Task BroadcastAsync(VitalsReadingSnapshot reading, CancellationToken cancellationToken);
}
