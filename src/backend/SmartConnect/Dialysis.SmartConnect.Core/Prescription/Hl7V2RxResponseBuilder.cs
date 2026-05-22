using System.Globalization;
using System.Text;

namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// Builds the dialysis-prescription <c>RSP^K22^RSP_K21</c> response frame per IG §5.2.2
/// + §5.4 sample messages. The OBX hierarchy follows the IG §9.3 containment model:
/// MDS (1) → VMD (1.1) → Channels (1.1.x) → Metrics (1.1.x.y).
/// </summary>
/// <remarks>
/// Implemented to faithfully reproduce IG §5.4.2 (HD with basic constant-UF) and §5.4.4
/// (no-prescription response). HF / HDF wire variants + UF profiles (linear /
/// exponential / step per IG §5.3) are deliberately out of scope for this slice — the
/// responder logs a TODO marker if asked to emit a non-HD modality.
/// </remarks>
public static class Hl7V2RxResponseBuilder
{
    private const string MessageType = "RSP^K22^RSP_K21";
    private const string ProcessingId = "P";
    private const string VersionId = "2.6";
    private const string QueryNameCwe = "0^MDC_HDIALY_RX_QUERY^MDC";

    public static string Build(
        PrescriptionQuery inbound,
        PrescriptionDocument? document,
        string responseControlId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(inbound);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseControlId);

        var sb = new StringBuilder();
        AppendHeader(sb, responseControlId, nowUtc);
        AppendMsa(sb, inbound.MessageControlId);

        if (document is null)
        {
            AppendQak(sb, inbound.QueryTag, status: "NF", hits: 0);
            AppendQpdEcho(sb, inbound);
            return sb.ToString();
        }

        AppendQak(sb, inbound.QueryTag, status: "OK", hits: 1);
        AppendQpdEcho(sb, inbound);
        AppendObc(sb, document);
        AppendObxHierarchy(sb, document);
        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, string controlId, DateTime nowUtc)
    {
        sb.Append("MSH|^~\\&|||||").Append(FormatTs(nowUtc)).Append("||").Append(MessageType)
            .Append('|').Append(controlId).Append('|').Append(ProcessingId).Append('|').Append(VersionId)
            .Append("|||NE|NE|||||\r");
    }

    private static void AppendMsa(StringBuilder sb, string inboundControlId) =>
        sb.Append("MSA|AA|").Append(inboundControlId).Append('\r');

    private static void AppendQak(StringBuilder sb, string queryTag, string status, int hits) =>
        sb.Append("QAK|").Append(queryTag).Append('|').Append(status).Append('|').Append(QueryNameCwe)
            .Append('|').Append(hits).Append('|').Append(hits).Append("|0\r");

    private static void AppendQpdEcho(StringBuilder sb, PrescriptionQuery inbound) =>
        sb.Append("QPD|").Append(QueryNameCwe).Append('|').Append(inbound.QueryTag)
            .Append('|').Append(EchoCriteria(inbound)).Append('\r');

    private static void AppendObc(StringBuilder sb, PrescriptionDocument doc)
    {
        // OBC|NW|<orderNumber>^PC||||N||||<orderingProvider>
        sb.Append("OBC|NW|").Append(Sanitize(doc.OrderNumber)).Append("^PC||||N||||");
        if (!string.IsNullOrWhiteSpace(doc.OrderingProviderId))
        {
            sb.Append(Sanitize(doc.OrderingProviderId)).Append('^')
              .Append(Sanitize(doc.OrderingProviderFamily ?? string.Empty)).Append('^')
              .Append(Sanitize(doc.OrderingProviderGiven ?? string.Empty)).Append("^^^^MD");
        }
        sb.Append('\r');
    }

    private static void AppendObxHierarchy(StringBuilder sb, PrescriptionDocument doc)
    {
        // Container chain: MDS (1) → VMD (1.1) → MachConfig channel (1.1.1).
        var setIdx = 1;
        Obx(sb, ref setIdx, "ST", "70929^MDC_DEV_HDIALY_MACHINE_MDS^MDC", "1");
        Obx(sb, ref setIdx, "ST", "70934^MDC_DEV_HDIALY_VMD^MDC", "1.1");
        Obx(sb, ref setIdx, "ST", "70939^MDC_DEV_HDIALY_MACH_CONFIG_CHAN^MDC", "1.1.1");
        Obx(sb, ref setIdx, "ST", "158598^MDC_HDIALY_MACH_TX_MODALITY^MDC", "1.1.1.1",
            value: doc.Modality switch
            {
                TherapyModality.Hd => "HD",
                TherapyModality.Hf => "HF",
                TherapyModality.Hdf => "HDF",
                _ => "HD",
            });

        // Therapy outcomes channel (1.1.2)
        Obx(sb, ref setIdx, "ST", "70967^MDC_DEV_HDIALY_THERAPY_OUTCOMES_CHAN^MDC", "1.1.2");
        Obx(sb, ref setIdx, "ST", "158618^MDC_HDIALY_THERAPY_COMPLETE_METHOD^MDC", "1.1.2.1",
            value: doc.TherapyCompletionMethod);

        // Blood pump channel (1.1.3)
        Obx(sb, ref setIdx, "ST", "70947^MDC_DEV_HDIALY_BLOOD_PUMP_CHAN^MDC", "1.1.3");
        Obx(sb, ref setIdx, "NM", "16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC", "1.1.3.1",
            value: doc.BloodPump.BloodFlowRateMlPerMin.ToString(CultureInfo.InvariantCulture),
            units: "ml/min^ml/min^UCUM");
        Obx(sb, ref setIdx, "ST", "158604^MDC_HDIALY_BLD_PUMP_MODE^MDC", "1.1.3.2",
            value: doc.BloodPump.PumpMode);

        // Fluid channel (1.1.4) for HD. HF / HDF use different OBXs — emitted below if Dialysate is null.
        if (doc.Dialysate is { } fluid)
        {
            Obx(sb, ref setIdx, "ST", "70951^MDC_DEV_HDIALY_FLUID_CHAN^MDC", "1.1.4");
            Obx(sb, ref setIdx, "ST", "158606^MDC_HDIALY_DIALYSATE_FLOW_MODE^MDC", "1.1.4.1",
                value: fluid.FlowMode);
            Obx(sb, ref setIdx, "NM", "16936008^MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING^MDC", "1.1.4.2",
                value: fluid.FlowRateMlPerMin.ToString(CultureInfo.InvariantCulture),
                units: "ml/min^ml/min^UCUM");
            Obx(sb, ref setIdx, "NM", "0^MDC_HDIALY_DIALYSATE_VOL_SETTING^MDC", "1.1.4.3",
                value: fluid.VolumeLiters.ToString(CultureInfo.InvariantCulture),
                units: "L^L^UCUM");
            Obx(sb, ref setIdx, "ST", "158608^MDC_HDIALY_DIALYSATE_NAME^MDC", "1.1.4.4",
                value: fluid.DialysateName);
        }

        // UF channel (1.1.5). Profile-driven modes (PRO-WT / PRO-WOT) need OBX-13/14
        // sub-children per IG §5.3; this slice only models the constant modes.
        Obx(sb, ref setIdx, "ST", "70971^MDC_DEV_HDIALY_UF_CHAN^MDC", "1.1.5");
        Obx(sb, ref setIdx, "ST", "158619^MDC_HDIALY_UF_MODE^MDC", "1.1.5.1",
            value: doc.Ultrafiltration.UfMode);
        Obx(sb, ref setIdx, "NM", "16936252^MDC_HDIALY_UF_RATE_SETTING^MDC", "1.1.5.2",
            value: doc.Ultrafiltration.UfRateMlPerHour.ToString(CultureInfo.InvariantCulture),
            units: "ml/h^ml/h^UCUM");
        Obx(sb, ref setIdx, "NM", "159028^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC", "1.1.5.3",
            value: doc.Ultrafiltration.TargetVolumeToRemoveMl.ToString(CultureInfo.InvariantCulture),
            units: "ml^ml^UCUM");
    }

    /// <summary>
    /// Emits one OBX line. Container OBXs (no value) are emitted with empty OBX-5; value
    /// OBXs include OBX-5 and optionally OBX-6 units. OBX-11 (result status) is always
    /// <c>F</c> per IG samples.
    /// </summary>
    private static void Obx(
        StringBuilder sb,
        ref int setIdx,
        string valueType,
        string observationId,
        string containmentPath,
        string? value = null,
        string? units = null)
    {
        sb.Append("OBX|").Append(setIdx).Append('|').Append(valueType).Append('|')
          .Append(observationId).Append('|').Append(containmentPath).Append('|')
          .Append(value ?? string.Empty).Append('|').Append(units ?? string.Empty)
          .Append("|||||F\r");
        setIdx += 1;
    }

    private static string EchoCriteria(PrescriptionQuery q)
    {
        return string.IsNullOrWhiteSpace(q.MedicalRecordNumber)
            ? string.Empty
            : $"@PID.3^{Sanitize(q.MedicalRecordNumber)}^^^^MR";
    }

    private static string FormatTs(DateTime utc) =>
        utc.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);

    private static string Sanitize(string value) =>
        value
            .Replace("|", string.Empty, StringComparison.Ordinal)
            .Replace("^", string.Empty, StringComparison.Ordinal)
            .Replace("~", string.Empty, StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal)
            .Replace("&", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
}
