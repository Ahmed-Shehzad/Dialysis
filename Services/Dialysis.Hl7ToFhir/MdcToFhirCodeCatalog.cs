using System.Collections.Frozen;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps IEEE 11073 MDC codes to FHIR-compatible coding: display name + optional LOINC.
/// MDC system: urn:iso:std:iso:11073:10101.
/// LOINC system: http://loinc.org.
/// </summary>
public static class MdcToFhirCodeCatalog
{
    private static readonly FrozenDictionary<string, MdcFhirDescriptor> Catalog = BuildCatalog();

    public static bool TryGet(string mdcCode, out MdcFhirDescriptor descriptor) =>
        Catalog.TryGetValue(mdcCode, out descriptor!);

    public static MdcFhirDescriptor? Get(string mdcCode) =>
        Catalog.GetValueOrDefault(mdcCode);

    private static FrozenDictionary<string, MdcFhirDescriptor> BuildCatalog()
    {
        var d = new Dictionary<string, MdcFhirDescriptor>(StringComparer.OrdinalIgnoreCase);

        Add(d, "MDC_PRESS_BLD_SYS", "Systolic Blood Pressure", "8480-6");
        Add(d, "MDC_PRESS_BLD_DIA", "Diastolic Blood Pressure", "8462-4");
        Add(d, "MDC_PRESS_BLD_MEAN", "Mean Arterial Pressure", "8472-3");
        Add(d, "MDC_PRESS_BLD_ART", "Arterial Pressure", "99717-1");
        Add(d, "MDC_PRESS_BLD_VEN", "Venous Pressure", "99717-1");
        Add(d, "MDC_DIA_PRESS_TRANSMEMBRANE", "Transmembrane Pressure", "99720-5");
        Add(d, "MDC_PULS_RATE", "Heart Rate", "8867-4");
        Add(d, "MDC_DIA_BLD_FLOW_RATE", "Blood Flow Rate", null);
        Add(d, "MDC_DIA_DIALYSATE_FLOW_RATE", "Dialysate Flow Rate", "99712-2");
        Add(d, "MDC_DIA_UF_RATE", "Ultrafiltration Rate", null);
        Add(d, "MDC_DIA_UF_VOL_TOTAL", "UF Volume Removed", null);
        Add(d, "MDC_DIA_TEMP_DIALYSATE", "Dialysate Temperature", null);
        Add(d, "MDC_TEMP_BLD", "Blood Temperature", null);
        Add(d, "MDC_DIA_COND_TOTAL", "Conductivity", null);
        Add(d, "MDC_CONC_NA", "Sodium Concentration", null);
        Add(d, "MDC_CONC_HCO3", "Bicarbonate Concentration", null);
        Add(d, "MDC_DIA_WGT_PREDIAL", "Pre-Dialysis Weight", "29463-7");
        Add(d, "MDC_DIA_WGT_POSTDIAL", "Post-Dialysis Weight", "3108-1");
        Add(d, "MDC_DIA_WGT_TARGET", "Target Weight", null);
        Add(d, "MDC_DIA_THERAPY_TIME_ACTUAL", "Actual Therapy Time", null);
        Add(d, "MDC_DIA_THERAPY_TIME_REMAIN", "Remaining Therapy Time", null);

        return d.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static void Add(Dictionary<string, MdcFhirDescriptor> d, string mdc, string display, string? loinc) => d[mdc] = new MdcFhirDescriptor(display, loinc);
}

public sealed record MdcFhirDescriptor(string DisplayName, string? LoincCode);
