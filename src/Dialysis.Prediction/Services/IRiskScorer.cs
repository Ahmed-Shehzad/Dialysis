namespace Dialysis.Prediction.Services;

public interface IRiskScorer
{
    double CalculateRisk(string patientId, IReadOnlyList<VitalSnapshot> recentVitals);
}

public sealed record VitalSnapshot(string Code, double Value, DateTimeOffset Effective);

public static class VitalCodes
{
    public const string SystolicBp = "8480-6";
    public const string SystolicBpAlt = "blood-pressure-systolic";
    public const string DiastolicBp = "8462-4";
    public const string HeartRate = "8867-4";
    public const string HeartRateAlt = "heart-rate";
    public const string SpO2 = "59408-5";
}
