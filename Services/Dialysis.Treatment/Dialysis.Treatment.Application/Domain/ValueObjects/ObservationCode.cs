namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// Strongly-typed MDC observation code from IEEE 11073 nomenclature (e.g. MDC_PRESS_BLD_SYS).
/// Maps to OBX-3 (Observation Identifier) in HL7 v2.
/// </summary>
public readonly record struct ObservationCode
{
    public string Value { get; }

    public ObservationCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    // ─── MDS ─────────────────────────────────────────────────────────────────
    public static readonly ObservationCode DialysisMds = new("MDC_DEV_SPEC_PROFILE_DIALYSIS");

    // ─── General ─────────────────────────────────────────────────────────────
    public static readonly ObservationCode ModeOfOperation = new("MDC_DIA_MODE_OP");
    public static readonly ObservationCode Modality = new("MDC_DIA_MODALITY");
    public static readonly ObservationCode TherapyTimePrescribed = new("MDC_DIA_THERAPY_TIME_PRES");
    public static readonly ObservationCode TherapyTimeRemaining = new("MDC_DIA_THERAPY_TIME_REMAIN");
    public static readonly ObservationCode TherapyTimeActual = new("MDC_DIA_THERAPY_TIME_ACTUAL");
    public static readonly ObservationCode CompletionMethod = new("MDC_DIA_COMPLETION_METHOD");
    public static readonly ObservationCode WeightPreDialysis = new("MDC_DIA_WGT_PREDIAL");
    public static readonly ObservationCode WeightPostDialysis = new("MDC_DIA_WGT_POSTDIAL");
    public static readonly ObservationCode WeightTarget = new("MDC_DIA_WGT_TARGET");

    // ─── Blood Pump ──────────────────────────────────────────────────────────
    public static readonly ObservationCode BloodPumpMode = new("MDC_DIA_BLD_PUMP_MODE");
    public static readonly ObservationCode BloodFlowRatePrescribed = new("MDC_DIA_BLD_FLOW_RATE_PRES");
    public static readonly ObservationCode BloodFlowRate = new("MDC_DIA_BLD_FLOW_RATE");
    public static readonly ObservationCode BloodVolumeProcessed = new("MDC_DIA_BLD_VOL_PROCESSED");
    public static readonly ObservationCode ArterialPressure = new("MDC_PRESS_BLD_ART");
    public static readonly ObservationCode VenousPressure = new("MDC_PRESS_BLD_VEN");
    public static readonly ObservationCode TransmembranePressure = new("MDC_DIA_PRESS_TRANSMEMBRANE");

    // ─── Dialysate ───────────────────────────────────────────────────────────
    public static readonly ObservationCode DialysateFlowMode = new("MDC_DIA_DIALYSATE_FLOW_MODE");
    public static readonly ObservationCode DialysateFlowRatePrescribed = new("MDC_DIA_DIALYSATE_FLOW_RATE_PRES");
    public static readonly ObservationCode DialysateFlowRate = new("MDC_DIA_DIALYSATE_FLOW_RATE");
    public static readonly ObservationCode DialysatePressure = new("MDC_DIA_DIALYSATE_PRESS");
    public static readonly ObservationCode SodiumConcentrationPrescribed = new("MDC_CONC_NA_PRES");
    public static readonly ObservationCode SodiumConcentration = new("MDC_CONC_NA");
    public static readonly ObservationCode BicarbConcentrationPrescribed = new("MDC_CONC_HCO3_PRES");
    public static readonly ObservationCode BicarbConcentration = new("MDC_CONC_HCO3");
    public static readonly ObservationCode TotalConductivity = new("MDC_DIA_COND_TOTAL");
    public static readonly ObservationCode TotalConductivityPrescribed = new("MDC_DIA_COND_TOTAL_PRES");

    // ─── HDF ─────────────────────────────────────────────────────────────────
    public static readonly ObservationCode HdfFlowMode = new("MDC_DIA_HDF_FLOW_MODE");
    public static readonly ObservationCode HdfFlowRatePrescribed = new("MDC_DIA_HDF_FLOW_RATE_PRES");
    public static readonly ObservationCode HdfFlowRate = new("MDC_DIA_HDF_FLOW_RATE");
    public static readonly ObservationCode HdfVolumeTotal = new("MDC_DIA_HDF_VOL_TOTAL");
    public static readonly ObservationCode HdfLocation = new("MDC_DIA_HDF_LOCATION");

    // ─── Ultrafiltration ─────────────────────────────────────────────────────
    public static readonly ObservationCode UfMode = new("MDC_DIA_UF_MODE");
    public static readonly ObservationCode UfRatePrescribed = new("MDC_DIA_UF_RATE_PRES");
    public static readonly ObservationCode UfRate = new("MDC_DIA_UF_RATE");
    public static readonly ObservationCode UfVolumeTarget = new("MDC_DIA_UF_VOL_TARGET");
    public static readonly ObservationCode UfVolumeTotal = new("MDC_DIA_UF_VOL_TOTAL");
    public static readonly ObservationCode UfVolumeRemaining = new("MDC_DIA_UF_VOL_REMAIN");

    // ─── Anticoagulation ─────────────────────────────────────────────────────
    public static readonly ObservationCode AnticoagMode = new("MDC_DIA_ANTICOAG_MODE");
    public static readonly ObservationCode AnticoagBolusPrescribed = new("MDC_DIA_ANTICOAG_BOLUS_PRES");
    public static readonly ObservationCode AnticoagBolus = new("MDC_DIA_ANTICOAG_BOLUS");
    public static readonly ObservationCode AnticoagRatePrescribed = new("MDC_DIA_ANTICOAG_RATE_PRES");
    public static readonly ObservationCode AnticoagRate = new("MDC_DIA_ANTICOAG_RATE");
    public static readonly ObservationCode AnticoagVolumeTotal = new("MDC_DIA_ANTICOAG_VOL_TOTAL");

    // ─── Blood Pressure ──────────────────────────────────────────────────────
    public static readonly ObservationCode SystolicBp = new("MDC_PRESS_BLD_SYS");
    public static readonly ObservationCode DiastolicBp = new("MDC_PRESS_BLD_DIA");
    public static readonly ObservationCode MeanArterialPressure = new("MDC_PRESS_BLD_MEAN");
    public static readonly ObservationCode HeartRate = new("MDC_PULS_RATE");

    // ─── Temperature ─────────────────────────────────────────────────────────
    public static readonly ObservationCode TempMode = new("MDC_DIA_TEMP_MODE");
    public static readonly ObservationCode TempDialysatePrescribed = new("MDC_DIA_TEMP_DIALYSATE_PRES");
    public static readonly ObservationCode TempDialysate = new("MDC_DIA_TEMP_DIALYSATE");
    public static readonly ObservationCode TempBlood = new("MDC_TEMP_BLD");
    public static readonly ObservationCode TempBody = new("MDC_TEMP_BODY");

    // ─── Adequacy ────────────────────────────────────────────────────────────
    public static readonly ObservationCode KtvOnline = new("MDC_DIA_KTV_ONLINE");
    public static readonly ObservationCode KtvPrescribed = new("MDC_DIA_KTV_PRES");
    public static readonly ObservationCode ClearanceUrea = new("MDC_DIA_CLEARANCE_UREA");
    public static readonly ObservationCode IonicDialysance = new("MDC_DIA_IONIC_DIALYSANCE");

    // ─── Vascular Access ─────────────────────────────────────────────────────
    public static readonly ObservationCode AccessFlow = new("MDC_DIA_ACCESS_FLOW");
    public static readonly ObservationCode AccessRecirculation = new("MDC_DIA_ACCESS_RECIRC");
    public static readonly ObservationCode BloodLeakDetection = new("MDC_DIA_BLD_LEAK_DETECT");

    public override string ToString() => Value;

    public static implicit operator string(ObservationCode code) => code.Value;
    public static explicit operator ObservationCode(string value) => new(value);
}
