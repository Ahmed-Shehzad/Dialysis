using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

/// <summary>
/// Prescribed treatment parameters for a session: dialyzer, blood/dialysate flow,
/// dialysate composition (K+, Ca++, Na+), anticoagulation, and target ultrafiltration.
/// </summary>
public sealed class SessionPrescription : ValueObject
{
    public string DialyzerModel { get; }

    public int PrescribedDurationMinutes { get; }

    public int BloodFlowRateMlPerMin { get; }

    public int DialysateFlowRateMlPerMin { get; }

    public decimal DialysatePotassiumMmolPerL { get; }

    public decimal DialysateCalciumMmolPerL { get; }

    public decimal DialysateSodiumMmolPerL { get; }

    public decimal TargetUfVolumeLiters { get; }

    public string AnticoagulationProtocolCode { get; }

    public SessionPrescription(
        string dialyzerModel,
        int prescribedDurationMinutes,
        int bloodFlowRateMlPerMin,
        int dialysateFlowRateMlPerMin,
        decimal dialysatePotassiumMmolPerL,
        decimal dialysateCalciumMmolPerL,
        decimal dialysateSodiumMmolPerL,
        decimal targetUfVolumeLiters,
        string anticoagulationProtocolCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dialyzerModel);
        ArgumentException.ThrowIfNullOrWhiteSpace(anticoagulationProtocolCode);
        if (prescribedDurationMinutes is < 60 or > 480)
            throw new ArgumentOutOfRangeException(nameof(prescribedDurationMinutes), "Duration must be 60–480 minutes.");
        if (bloodFlowRateMlPerMin is < 100 or > 600)
            throw new ArgumentOutOfRangeException(nameof(bloodFlowRateMlPerMin));
        if (targetUfVolumeLiters < 0)
            throw new ArgumentOutOfRangeException(nameof(targetUfVolumeLiters));

        DialyzerModel = dialyzerModel.Trim();
        PrescribedDurationMinutes = prescribedDurationMinutes;
        BloodFlowRateMlPerMin = bloodFlowRateMlPerMin;
        DialysateFlowRateMlPerMin = dialysateFlowRateMlPerMin;
        DialysatePotassiumMmolPerL = dialysatePotassiumMmolPerL;
        DialysateCalciumMmolPerL = dialysateCalciumMmolPerL;
        DialysateSodiumMmolPerL = dialysateSodiumMmolPerL;
        TargetUfVolumeLiters = targetUfVolumeLiters;
        AnticoagulationProtocolCode = anticoagulationProtocolCode.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DialyzerModel;
        yield return PrescribedDurationMinutes;
        yield return BloodFlowRateMlPerMin;
        yield return DialysateFlowRateMlPerMin;
        yield return DialysatePotassiumMmolPerL;
        yield return DialysateCalciumMmolPerL;
        yield return DialysateSodiumMmolPerL;
        yield return TargetUfVolumeLiters;
        yield return AnticoagulationProtocolCode;
    }
}
