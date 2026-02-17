namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// Blood pressure value (systolic/diastolic). Avoids primitive obsession.
/// </summary>
public sealed record BloodPressure
{
    public int Systolic { get; }
    public int Diastolic { get; }

    public BloodPressure(int systolic, int diastolic)
    {
        if (systolic is < 0 or > 300)
            throw new ArgumentOutOfRangeException(nameof(systolic), "Systolic must be between 0 and 300.");
        if (diastolic is < 0 or > 200)
            throw new ArgumentOutOfRangeException(nameof(diastolic), "Diastolic must be between 0 and 200.");
        Systolic = systolic;
        Diastolic = diastolic;
    }

    public string Display => $"BP {Systolic}/{Diastolic} mmHg";
}
