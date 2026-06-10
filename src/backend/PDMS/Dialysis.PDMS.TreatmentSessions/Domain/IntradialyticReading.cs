using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

/// <summary>Periodic intradialytic observation (BP, pulse, machine pressures, UF rate).</summary>
public sealed class IntradialyticReading : Entity<Guid>
{
    private IntradialyticReading()
    {
    }

    public IntradialyticReading(Guid id) : base(id)
    {
    }

    public Guid SessionId { get; private set; }

    public DateTime ObservedAtUtc { get; private set; }

    public int SystolicBloodPressure { get; private set; }

    public int DiastolicBloodPressure { get; private set; }

    public int HeartRateBpm { get; private set; }

    public decimal ArterialPressureMmHg { get; private set; }

    public decimal VenousPressureMmHg { get; private set; }

    public decimal UltrafiltrationRateMlPerHour { get; private set; }

    public decimal ConductivityMsPerCm { get; private set; }

    public string? Notes { get; private set; }

    public static IntradialyticReading Record(
        Guid id,
        Guid sessionId,
        DateTime observedAtUtc,
        int systolic,
        int diastolic,
        int heartRateBpm,
        decimal arterialPressureMmHg,
        decimal venousPressureMmHg,
        decimal ultrafiltrationRateMlPerHour,
        decimal conductivityMsPerCm,
        string? notes = null)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Session required.", nameof(sessionId));
        if (systolic is < 40 or > 260)
            throw new ArgumentOutOfRangeException(nameof(systolic));
        if (diastolic is < 20 or > 180)
            throw new ArgumentOutOfRangeException(nameof(diastolic));
        if (heartRateBpm is < 20 or > 250)
            throw new ArgumentOutOfRangeException(nameof(heartRateBpm));

        return new IntradialyticReading(id)
        {
            SessionId = sessionId,
            ObservedAtUtc = observedAtUtc,
            SystolicBloodPressure = systolic,
            DiastolicBloodPressure = diastolic,
            HeartRateBpm = heartRateBpm,
            ArterialPressureMmHg = arterialPressureMmHg,
            VenousPressureMmHg = venousPressureMmHg,
            UltrafiltrationRateMlPerHour = ultrafiltrationRateMlPerHour,
            ConductivityMsPerCm = conductivityMsPerCm,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
    }
}
