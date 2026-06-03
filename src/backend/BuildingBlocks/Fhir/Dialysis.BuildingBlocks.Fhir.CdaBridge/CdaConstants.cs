using System.Xml.Linq;

namespace Dialysis.BuildingBlocks.Fhir.CdaBridge;

/// <summary>
/// C-CDA R2.1 template identifiers, section LOINC codes, and code-system OID ↔ FHIR URI
/// mappings shared by the inbound parser and outbound emitter. Keeping these in one place keeps
/// the two directions symmetric: a section the parser recognises is a section the emitter can
/// produce.
/// </summary>
internal static class CdaConstants
{
    public static readonly XNamespace Hl7 = "urn:hl7-org:v3";
    public static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    // Section LOINC codes (the <section><code> value) — the stable identifier we match on, with
    // the C-CDA templateId as a secondary signal.
    public const string ProblemsSectionLoinc = "11450-4";
    public const string AllergiesSectionLoinc = "48765-2";
    public const string MedicationsSectionLoinc = "10160-0";
    public const string ResultsSectionLoinc = "30954-2";
    public const string VitalSignsSectionLoinc = "8716-3";
    public const string ImmunizationsSectionLoinc = "11369-6";

    // Section templateIds (C-CDA R2.1, entries-required variants).
    public const string ProblemsTemplateId = "2.16.840.1.113883.10.20.22.2.5.1";
    public const string AllergiesTemplateId = "2.16.840.1.113883.10.20.22.2.6.1";
    public const string MedicationsTemplateId = "2.16.840.1.113883.10.20.22.2.1.1";
    public const string ResultsTemplateId = "2.16.840.1.113883.10.20.22.2.3.1";
    public const string VitalSignsTemplateId = "2.16.840.1.113883.10.20.22.2.4.1";
    public const string ImmunizationsTemplateId = "2.16.840.1.113883.10.20.22.2.2.1";

    public const string LoincOid = "2.16.840.1.113883.6.1";
    public const string LoincUri = "http://loinc.org";

    private static readonly Dictionary<string, string> _oidToUri = new(StringComparer.Ordinal)
    {
        [LoincOid] = LoincUri,
        ["2.16.840.1.113883.6.96"] = "http://snomed.info/sct",
        ["2.16.840.1.113883.6.88"] = "http://www.nlm.nih.gov/research/umls/rxnorm",
        ["2.16.840.1.113883.6.69"] = "http://hl7.org/fhir/sid/ndc",
        ["2.16.840.1.113883.12.292"] = "http://hl7.org/fhir/sid/cvx",
        ["2.16.840.1.113883.6.103"] = "http://hl7.org/fhir/sid/icd-9-cm",
        ["2.16.840.1.113883.6.90"] = "http://hl7.org/fhir/sid/icd-10-cm",
        ["2.16.840.1.113883.3.26.1.1"] = "http://ncimeta.nci.nih.gov",
        ["2.16.840.1.113883.6.1.11.20.9.39"] = "http://terminology.hl7.org/CodeSystem/v3-ActCode",
    };

    private static readonly Dictionary<string, string> _uriToOid =
        _oidToUri.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    /// <summary>Maps a CDA codeSystem OID to a FHIR system URI, falling back to <c>urn:oid:{oid}</c>.</summary>
    public static string OidToUri(string? oid) =>
        string.IsNullOrWhiteSpace(oid) ? string.Empty
        : _oidToUri.TryGetValue(oid, out var uri) ? uri
        : $"urn:oid:{oid}";

    /// <summary>Maps a FHIR system URI back to a CDA codeSystem OID, unwrapping <c>urn:oid:</c> forms.</summary>
    public static string UriToOid(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri)) return string.Empty;
        if (_uriToOid.TryGetValue(uri, out var oid)) return oid;
        return uri.StartsWith("urn:oid:", StringComparison.Ordinal) ? uri["urn:oid:".Length..] : uri;
    }
}
