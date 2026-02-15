namespace Dialysis.Prediction.Services;

public sealed class EnhancedRiskScorer : IRiskScorer
{
    private readonly RiskScorerOptions _options;

    public EnhancedRiskScorer(Microsoft.Extensions.Options.IOptions<RiskScorerOptions>? options = null)
    {
        _options = options?.Value ?? new RiskScorerOptions();
    }

    public double CalculateRisk(string patientId, IReadOnlyList<VitalSnapshot> recentVitals)
    {
        if (recentVitals.Count == 0) return 0.0;

        var systolic = recentVitals
            .Where(v => v.Code == VitalCodes.SystolicBp || v.Code == VitalCodes.SystolicBpAlt)
            .OrderByDescending(v => v.Effective)
            .Select(v => v.Value)
            .ToList();

        var diastolic = recentVitals
            .Where(v => v.Code == VitalCodes.DiastolicBp)
            .OrderByDescending(v => v.Effective)
            .Select(v => v.Value)
            .ToList();

        var heartRate = recentVitals
            .Where(v => v.Code == VitalCodes.HeartRate || v.Code == VitalCodes.HeartRateAlt || v.Code == "8867-4")
            .OrderByDescending(v => v.Effective)
            .Select(v => v.Value)
            .ToList();

        var spO2 = recentVitals
            .Where(v => v.Code == VitalCodes.SpO2)
            .OrderByDescending(v => v.Effective)
            .Select(v => v.Value)
            .ToList();

        var score = 0.0;

        if (systolic.Count > 0)
        {
            var latest = systolic[0];
            var avg = systolic.Take(_options.SystolicWindow).Average();
            var trend = systolic.Count >= 2 ? systolic[0] - systolic[1] : 0;

            if (latest < _options.SystolicCriticalThreshold) score = Math.Max(score, 0.9);
            else if (latest < _options.SystolicWarningThreshold) score = Math.Max(score, 0.65);
            else if (avg < _options.SystolicWarningThreshold) score = Math.Max(score, 0.5);

            if (trend < -10) score = Math.Min(1.0, score + 0.15);
        }

        if (heartRate.Count > 0)
        {
            var hr = heartRate[0];
            if (hr > _options.TachycardiaThreshold) score = Math.Min(1.0, score + 0.1);
        }

        if (spO2.Count > 0)
        {
            var spo2 = spO2[0];
            if (spo2 < _options.Spo2CriticalThreshold) score = Math.Max(score, 0.7);
        }

        return Math.Clamp(score, 0, 1);
    }
}

public sealed class RiskScorerOptions
{
    public const string SectionName = "Prediction:RiskScorer";
    public int SystolicWindow { get; set; } = 5;
    public double SystolicCriticalThreshold { get; set; } = 90;
    public double SystolicWarningThreshold { get; set; } = 100;
    public double TachycardiaThreshold { get; set; } = 100;
    public double Spo2CriticalThreshold { get; set; } = 92;
}
