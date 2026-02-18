using System.Globalization;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Domain.ValueObjects;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Infrastructure.Hl7;

/// <summary>
/// Builds HL7 RSP^K22 prescription response messages.
/// </summary>
public sealed class RspK22Builder : IRspK22Builder
{
    private const char FieldSeparator = '|';
    private const char ComponentSeparator = '^';
    private const char RepeatSeparator = '~';

    private const string PrescriptionQueryName = "MDC_HDIALY_RX_QUERY";
    private const string QueryDisplayName = "Hemodialysis Prescription Query";

    private static readonly IReadOnlyDictionary<string, string> CodeToUnits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING"] = "ml/min",
        ["MDC_HDIALY_UF_RATE_SETTING"] = "mL/h",
        ["MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE"] = "mL",
    };

    public string BuildFromPrescription(PrescriptionEntity prescription, RspK22ValidationContext context)
    {
        string messageControlId = context.MessageControlId ?? Ulid.NewUlid().ToString();
        string queryTag = context.QueryTag ?? messageControlId;

        var segments = new List<string>
        {
            BuildMsh(messageControlId),
            BuildMsa("AA", messageControlId),
            BuildQak(queryTag, PrescriptionQueryName, "OK"),
            BuildQpd(queryTag, prescription.PatientMrn.Value),
            BuildOrc(prescription),
            BuildPid(prescription.PatientMrn.Value),
        };

        int obxSetId = 0;
        foreach (var setting in prescription.Settings)
        {
            var obxSegments = BuildObxForSetting(setting, ref obxSetId);
            segments.AddRange(obxSegments);
        }

        return string.Join("\r\n", segments) + "\r\n";
    }

    public string BuildNoDataFound(RspK22ValidationContext context, string mrn)
    {
        string messageControlId = context.MessageControlId ?? Ulid.NewUlid().ToString();
        string queryTag = context.QueryTag ?? messageControlId;

        var segments = new List<string>
        {
            BuildMsh(messageControlId),
            BuildMsa("AA", messageControlId),
            BuildQak(queryTag, PrescriptionQueryName, "NF"),
            BuildQpdNoData(queryTag, mrn),
        };

        return string.Join("\r\n", segments) + "\r\n";
    }

    private static string BuildMsh(string messageControlId)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        return $"MSH{FieldSeparator}^~\\&{FieldSeparator}PDMS{FieldSeparator}FAC{FieldSeparator}MACH{FieldSeparator}FAC{FieldSeparator}{timestamp}{FieldSeparator}{FieldSeparator}RSP{ComponentSeparator}K22{ComponentSeparator}RSP_K21{FieldSeparator}{messageControlId}{FieldSeparator}P{FieldSeparator}2.6";
    }

    private static string BuildMsa(string ackCode, string messageControlId) =>
        $"MSA{FieldSeparator}{ackCode}{FieldSeparator}{messageControlId}";

    private static string BuildQak(string queryTag, string queryName, string status) =>
        $"QAK{FieldSeparator}{queryName}{FieldSeparator}{queryTag}{FieldSeparator}{status}";

    private static string BuildQpd(string queryTag, string mrn) =>
        $"QPD{FieldSeparator}{PrescriptionQueryName}{ComponentSeparator}{QueryDisplayName}{ComponentSeparator}MDC{FieldSeparator}{queryTag}{FieldSeparator}@PID.3{ComponentSeparator}{mrn}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}MR";

    private static string BuildQpdNoData(string queryTag, string mrn) =>
        $"QPD{FieldSeparator}{PrescriptionQueryName}{ComponentSeparator}{QueryDisplayName}{ComponentSeparator}MDC{FieldSeparator}{queryTag}{FieldSeparator}@PID.3{ComponentSeparator}{mrn}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}MR";

    private static string BuildOrc(PrescriptionEntity p)
    {
        string timestamp = (p.ReceivedAt ?? DateTimeOffset.UtcNow).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        string provider = p.OrderingProvider ?? "PROVIDER";
        string callback = p.CallbackPhone ?? "";
        return $"ORC{FieldSeparator}NW{FieldSeparator}{p.OrderId}^FAC{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{timestamp}{FieldSeparator}{FieldSeparator}{FieldSeparator}{provider}{FieldSeparator}{FieldSeparator}{callback}";
    }

    private static string BuildPid(string mrn) =>
        $"PID{FieldSeparator}{FieldSeparator}{FieldSeparator}{mrn}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}MR";

    private IEnumerable<string> BuildObxForSetting(ProfileSetting setting, ref int obxSetId)
    {
        string provenance = setting.Provenance ?? "RSET";
        string subIdBase = setting.SubId ?? "1.1.9.1";

        if (setting.Profile is not null) return BuildProfileFacetObxs(setting.Profile, subIdBase, provenance, ref obxSetId);

        if (setting.ConstantValue.HasValue)
        {
            string units = CodeToUnits.GetValueOrDefault(setting.Code, "");
            int refId = GetRefIdForCode(setting.Code);
            string obx3 = $"{refId}{ComponentSeparator}{setting.Code}{ComponentSeparator}MDC";
            return [BuildObxSegment(obx3, setting.SubId, setting.ConstantValue.Value.ToString(CultureInfo.InvariantCulture), units, provenance, ref obxSetId)];
        }

        return [];
    }

    private static IEnumerable<string> BuildProfileFacetObxs(ProfileDescriptor profile, string subIdBase, string provenance, ref int obxSetId)
    {
        var segments = new List<string>();

        string typeObx3 = $"{subIdBase}.1{ComponentSeparator}MDC_HDIALY_PROFILE_TYPE{ComponentSeparator}MDC";
        segments.Add(BuildObxSegment(typeObx3, $"{subIdBase}.1", profile.Type.Value.ToUpperInvariant(), "ml/h", provenance, ref obxSetId));

        string valueStr = string.Join(RepeatSeparator.ToString(), profile.Values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
        string valueObx3 = $"{subIdBase}.2{ComponentSeparator}MDC_HDIALY_PROFILE_VALUE{ComponentSeparator}MDC";
        segments.Add(BuildObxSegment(valueObx3, $"{subIdBase}.2", valueStr, "ml/h", provenance, ref obxSetId));

        if (profile.Times is { Count: > 0 })
        {
            string timeStr = string.Join(RepeatSeparator.ToString(), profile.Times.Select(t => t.ToString(CultureInfo.InvariantCulture)));
            string timeObx3 = $"{subIdBase}.3{ComponentSeparator}MDC_HDIALY_PROFILE_TIME{ComponentSeparator}MDC";
            segments.Add(BuildObxSegment(timeObx3, $"{subIdBase}.3", timeStr, "min", provenance, ref obxSetId));
        }

        if (profile.HalfTimeMinutes.HasValue && profile.HalfTimeMinutes.Value > 0)
        {
            string htObx3 = $"{subIdBase}.4{ComponentSeparator}MDC_HDIALY_PROFILE_EXP_HALF_TIME{ComponentSeparator}MDC";
            segments.Add(BuildObxSegment(htObx3, $"{subIdBase}.4", profile.HalfTimeMinutes.Value.ToString(CultureInfo.InvariantCulture), "min", provenance, ref obxSetId));
        }

        if (!string.IsNullOrEmpty(profile.VendorName))
        {
            string nameObx3 = $"{subIdBase}.5{ComponentSeparator}MDC_HDIALY_PROFILE_NAME{ComponentSeparator}MDC";
            segments.Add(BuildObxSegment(nameObx3, $"{subIdBase}.5", profile.VendorName, "", provenance, ref obxSetId));
        }

        return segments;
    }

    private static string BuildObxSegment(string obx3, string? subId, string value, string units, string provenance, ref int obxSetId)
    {
        int setId = ++obxSetId;
        string obx4 = subId ?? "";
        return $"OBX{FieldSeparator}{setId}{FieldSeparator}NM{FieldSeparator}{obx3}{FieldSeparator}{obx4}{FieldSeparator}{value}{FieldSeparator}{units}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{FieldSeparator}{provenance}";
    }

    private static int GetRefIdForCode(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING" => 16935956,
            "MDC_HDIALY_UF_RATE_SETTING" => 16936252,
            "MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE" => 159028,
            _ => Math.Abs(code.GetHashCode() % 100000) + 10000,
        };
    }
}
