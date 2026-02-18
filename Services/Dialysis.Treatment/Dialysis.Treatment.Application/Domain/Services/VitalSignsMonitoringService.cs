using Dialysis.Treatment.Application.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Domain.Services;

public sealed class VitalSignsMonitoringService
{
    private const double HypotensionSystolicThreshold = 90.0;
    private const double TachycardiaThreshold = 100.0;
    private const double BradycardiaThreshold = 60.0;
    private const double VenousPressureHighThreshold = 200.0;

    private readonly ILogger<VitalSignsMonitoringService> _logger;

    public VitalSignsMonitoringService(ILogger<VitalSignsMonitoringService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<ThresholdBreach> Evaluate(ObservationCode code, string? value)
    {
        if (value is null || !double.TryParse(value, out double numericValue))
            return [];

        var breaches = new List<ThresholdBreach>();

        if (code == ObservationCode.SystolicBp && numericValue < HypotensionSystolicThreshold)
        {
            breaches.Add(new ThresholdBreach(code, BreachType.Hypotension, numericValue, HypotensionSystolicThreshold, ThresholdDirection.Below));
            _logger.LogWarning("Hypotension detected: systolic BP {Value} mmHg below threshold {Threshold}", numericValue, HypotensionSystolicThreshold);
        }
        else if (code == ObservationCode.HeartRate && numericValue > TachycardiaThreshold)
        {
            breaches.Add(new ThresholdBreach(code, BreachType.Tachycardia, numericValue, TachycardiaThreshold, ThresholdDirection.Above));
            _logger.LogWarning("Tachycardia detected: heart rate {Value} bpm above threshold {Threshold}", numericValue, TachycardiaThreshold);
        }
        else if (code == ObservationCode.HeartRate && numericValue < BradycardiaThreshold)
        {
            breaches.Add(new ThresholdBreach(code, BreachType.Bradycardia, numericValue, BradycardiaThreshold, ThresholdDirection.Below));
            _logger.LogWarning("Bradycardia detected: heart rate {Value} bpm below threshold {Threshold}", numericValue, BradycardiaThreshold);
        }
        else if (code == ObservationCode.VenousPressure && numericValue > VenousPressureHighThreshold)
        {
            breaches.Add(new ThresholdBreach(code, BreachType.HighVenousPressure, numericValue, VenousPressureHighThreshold, ThresholdDirection.Above));
            _logger.LogWarning("High venous pressure detected: {Value} mmHg above threshold {Threshold}", numericValue, VenousPressureHighThreshold);
        }

        return breaches;
    }
}

public readonly record struct BreachType
{
    public string Value { get; }
    public BreachType(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value; }
    public static readonly BreachType Hypotension = new("Hypotension");
    public static readonly BreachType Tachycardia = new("Tachycardia");
    public static readonly BreachType Bradycardia = new("Bradycardia");
    public static readonly BreachType HighVenousPressure = new("HighVenousPressure");
    public override string ToString() => Value;
    public static implicit operator string(BreachType t) => t.Value;
}

public readonly record struct ThresholdDirection
{
    public string Value { get; }
    public ThresholdDirection(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); Value = value; }
    public static readonly ThresholdDirection Above = new("above");
    public static readonly ThresholdDirection Below = new("below");
    public override string ToString() => Value;
    public static implicit operator string(ThresholdDirection d) => d.Value;
}

public sealed record ThresholdBreach(
    ObservationCode ObservationCode,
    BreachType BreachType,
    double ObservedValue,
    double ThresholdValue,
    ThresholdDirection Direction);
