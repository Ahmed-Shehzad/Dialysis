using System.Runtime.CompilerServices;

using Dialysis.Prescription.Application.Domain.ValueObjects;

namespace Dialysis.Prescription.Application.Domain.Services;

/// <summary>
/// Evaluates profile formulas at a given time.
/// CONSTANT: y = A
/// LINEAR: y = A + (B-A)*t/T
/// EXPONENTIAL: y = (A-B)*e^(-kt) + B
/// STEP: value at current step
/// VENDOR: not computable, returns first value or NaN
/// </summary>
public static class ProfileCalculator
{
    public static decimal Evaluate(ProfileDescriptor profile, decimal timeMinutes, decimal totalTreatmentMinutes)
    {
        return profile.Values.Count == 0
            ? 0
            : profile.Type.Value.ToUpperInvariant() switch
            {
                "CONSTANT" => EvaluateConstant(profile.Values),
                "LINEAR" => EvaluateLinear(profile.Values, profile.Times, timeMinutes, totalTreatmentMinutes),
                "EXPONENTIAL" => EvaluateExponential(profile.Values, profile.Times, profile.HalfTimeMinutes, timeMinutes, totalTreatmentMinutes),
                "STEP" => EvaluateStep(profile.Values, profile.Times, timeMinutes),
                _ => profile.Values[0]
            };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal EvaluateConstant(IReadOnlyList<decimal> values) => values[0];

    private static decimal EvaluateLinear(
        IReadOnlyList<decimal> values,
        IReadOnlyList<decimal>? times,
        decimal timeMinutes,
        decimal totalMinutes)
    {
        if (values.Count < 2) return values[0];
        decimal startValue = values[0];
        decimal endValue = values[^1];

        decimal normalizedTime = ComputeNormalizedTime(times, timeMinutes, totalMinutes);
        normalizedTime = Math.Clamp(normalizedTime, 0, 1);
        return startValue + (endValue - startValue) * normalizedTime;
    }

    private static decimal ComputeNormalizedTime(IReadOnlyList<decimal>? times, decimal timeMinutes, decimal totalMinutes)
    {
        if (times is { Count: > 0 })
            return NormalizeTime(timeMinutes, times);
        if (totalMinutes > 0)
            return timeMinutes / totalMinutes;
        return 0;
    }

    private static decimal EvaluateExponential(
        IReadOnlyList<decimal> values,
        IReadOnlyList<decimal>? times,
        decimal? halfTime,
        decimal timeMinutes,
        decimal totalMinutes)
    {
        if (values.Count < 2) return values[0];
        decimal startValue = values[0];
        decimal endValue = values[^1];

        decimal effectiveTotal = times is { Count: > 0 } ? times[^1] : totalMinutes;
        decimal decayConstant = ComputeDecayConstant(halfTime, effectiveTotal);

        double decayConstantDouble = (double)decayConstant;
        double timeDouble = (double)timeMinutes;
        double expTerm = Math.Exp(-decayConstantDouble * timeDouble);
        return (startValue - endValue) * (decimal)expTerm + endValue;
    }

    private static decimal ComputeDecayConstant(decimal? halfTime, decimal effectiveTotal)
    {
        if (halfTime is > 0)
            return (decimal)(0.693 / (double)halfTime.Value);
        if (effectiveTotal > 0)
            return (decimal)(0.003 / (double)effectiveTotal);
        return 0.003m;
    }

    private static decimal EvaluateStep(IReadOnlyList<decimal> values, IReadOnlyList<decimal>? times, decimal timeMinutes)
    {
        if (values.Count == 0) return 0;
        if (times is null || times.Count < 2) return values[0];

        for (int index = times.Count - 1; index >= 0; index--)
            if (timeMinutes >= times[index])
                return values[Math.Min(index, values.Count - 1)];

        return values[0];
    }

    private static decimal NormalizeTime(decimal timeMinutes, IReadOnlyList<decimal> times)
    {
        if (times.Count == 0) return 0;
        decimal totalMinutes = times[^1];
        return totalMinutes > 0 ? timeMinutes / totalMinutes : 0;
    }
}
