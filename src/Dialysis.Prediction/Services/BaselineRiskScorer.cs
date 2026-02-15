namespace Dialysis.Prediction.Services;

public sealed class BaselineRiskScorer : IRiskScorer
{
    public double CalculateRisk(string patientId, IReadOnlyList<VitalSnapshot> recentVitals)
    {
        var systolicReadings = recentVitals
            .Where(v => v.Code == VitalCodes.SystolicBp || v.Code == VitalCodes.SystolicBpAlt)
            .Select(v => v.Value)
            .ToList();

        if (systolicReadings.Count == 0) return 0.0;

        var avgSystolic = systolicReadings.Average();
        if (avgSystolic < 90) return 0.85;
        if (avgSystolic < 100) return 0.6;
        return 0.2;
    }
}
