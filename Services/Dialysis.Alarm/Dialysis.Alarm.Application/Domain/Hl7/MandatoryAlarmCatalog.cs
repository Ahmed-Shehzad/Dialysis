using System.Collections.Frozen;

namespace Dialysis.Alarm.Application.Domain.Hl7;

/// <summary>
/// PCD-04 Table 3 — mandatory alarms for dialysis machines.
/// Source + Event → Display Name.
/// </summary>
public static class MandatoryAlarmCatalog
{
    private static readonly FrozenDictionary<string, string> Catalog = BuildCatalog();

    public static bool TryGetDisplayName(string sourceCode, string eventCode, out string displayName)
    {
        string key = $"{sourceCode}|{eventCode}";
        return Catalog.TryGetValue(key, out displayName!);
    }

    public static string? GetDisplayName(string sourceCode, string eventCode) =>
        Catalog.GetValueOrDefault($"{sourceCode}|{eventCode}");

    private static FrozenDictionary<string, string> BuildCatalog()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Add(d, "MDC_HDIALY_BLD_PRESS_ART", "MDC_EVT_HI", "Arterial Pressure High");
        Add(d, "MDC_HDIALY_BLD_PRESS_ART", "MDC_EVT_LO", "Arterial Pressure Low");
        Add(d, "MDC_HDIALY_BLOOD_PUMP_CHAN", "MDC_EVT_HDIALY_BLD_PUMP_STOP", "Blood Pump Stop");
        Add(d, "MDC_HDIALY_BLD_PUMP_PRESS_VEN", "MDC_EVT_HI", "Venous Pressure High");
        Add(d, "MDC_HDIALY_BLD_PUMP_PRESS_VEN", "MDC_EVT_LO", "Venous Pressure Low");
        Add(d, "MDC_HDIALY_FLUID_CHAN", "MDC_EVT_HDIALY_BLOOD_LEAK", "Blood Leak");
        Add(d, "MDC_HDIALY_FILTER_TRANSMEMBRANE_PRESS", "MDC_EVT_HI", "TMP High");
        Add(d, "MDC_HDIALY_FILTER_TRANSMEMBRANE_PRESS", "MDC_EVT_LO", "TMP Low");
        Add(d, "MDC_HDIALY_SAFETY_SYSTEMS_CHAN", "MDC_EVT_HDIALY_SAFETY_ART_AIR_DETECT", "Arterial Air Detector");
        Add(d, "MDC_HDIALY_SAFETY_SYSTEMS_CHAN", "MDC_EVT_HDIALY_SAFETY_VEN_AIR_DETECT", "Venous Air Detector");
        Add(d, "MDC_HDIALY_SAFETY_SYSTEMS_CHAN", "MDC_EVT_HDIALY_SAFETY_SYSTEM_GENERAL", "General System");
        Add(d, "MDC_HDIALY_SAFETY_SYSTEMS_CHAN", "MDC_EVT_SELFTEST_FAILURE", "Self-Test Failure");
        Add(d, "MDC_HDIALY_UF_CHAN", "MDC_EVT_HDIALY_UF_RATE_RANGE", "UF Rate Out of Range");

        return d.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static void Add(Dictionary<string, string> d, string source, string evt, string name) => d[$"{source}|{evt}"] = name;
}
