using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Documents;

/// <summary>A converted, downloadable representation of a ledger message payload.</summary>
public sealed record ExportedDocument(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Converts a captured message payload into a downloadable clinical document. HL7 v2 payloads are
/// parsed and re-expressed as HL7 v2 XML, a C-CDA R2.1 Continuity of Care Document, or a minimal
/// FHIR R4 transaction Bundle. Non-HL7 payloads degrade gracefully (raw is always available; XML
/// wraps the text, FHIR/CDA emit a document carrying the original text).
/// </summary>
public static class MessageDocumentExporter
{
    /// <summary>Export formats accepted by <see cref="Export"/> (lower-case, case-insensitive).</summary>
    public static IReadOnlyList<string> Formats { get; } = ["raw", "hl7", "xml", "cda", "fhir"];

    private const string CdaNs = "urn:hl7-org:v3";

    public static ExportedDocument Export(ReadOnlySpan<byte> payload, string? format, string? correlationId)
    {
        var id = SanitizeId(correlationId);
        var bytes = payload.ToArray();
        var text = Encoding.UTF8.GetString(bytes);

        return (format ?? "raw").Trim().ToLowerInvariant() switch
        {
            "raw" => new ExportedDocument($"message-{id}.txt", "text/plain; charset=utf-8", bytes),
            "hl7" => new ExportedDocument($"message-{id}.hl7", "application/hl7-v2+er7; charset=utf-8", bytes),
            "xml" => new ExportedDocument($"message-{id}.xml", "application/xml; charset=utf-8", Utf8(ToHl7Xml(text))),
            "cda" => new ExportedDocument($"message-{id}.cda.xml", "application/xml; charset=utf-8", Utf8(ToCcd(text, correlationId))),
            "fhir" => new ExportedDocument($"message-{id}.fhir.json", "application/fhir+json; charset=utf-8", Utf8(ToFhirBundle(text, correlationId))),
            var other => throw new ArgumentException(
                $"Unsupported export format '{other}'. Supported: {string.Join(", ", Formats)}.", nameof(format)),
        };
    }

    // ---- HL7 v2 → HL7 v2 XML ---------------------------------------------------------------

    private static string ToHl7Xml(string raw)
    {
        var lines = raw.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var root = new XElement("HL7v2Message");

        if (lines.Length == 0 || !lines[0].StartsWith("MSH", StringComparison.Ordinal))
        {
            root.Add(new XElement("RawMessage", raw));
            return Declare(new XDocument(root));
        }

        var fieldSep = lines[0].Length > 3 ? lines[0][3] : '|';
        var encoding = lines[0].Length > 7 ? lines[0].Substring(4, 4) : "^~\\&";
        var compSep = encoding.Length > 0 ? encoding[0] : '^';
        var repSep = encoding.Length > 1 ? encoding[1] : '~';
        var subSep = encoding.Length > 3 ? encoding[3] : '&';

        foreach (var line in lines)
        {
            var fields = line.Split(fieldSep);
            var segName = fields[0];
            var segEl = new XElement(segName);

            if (segName == "MSH")
            {
                segEl.Add(new XElement("MSH.1", fieldSep.ToString()));
                segEl.Add(new XElement("MSH.2", fields.Length > 1 ? fields[1] : encoding));
                for (var f = 2; f < fields.Length; f++)
                    AddField(segEl, segName, f + 1, fields[f], repSep, compSep, subSep);
            }
            else
            {
                for (var f = 1; f < fields.Length; f++)
                    AddField(segEl, segName, f, fields[f], repSep, compSep, subSep);
            }

            root.Add(segEl);
        }

        return Declare(new XDocument(root));
    }

    private static void AddField(
        XElement segEl, string segName, int hl7Index, string value, char repSep, char compSep, char subSep)
    {
        if (string.IsNullOrEmpty(value))
            return;

        foreach (var rep in value.Split(repSep))
        {
            var fieldEl = new XElement($"{segName}.{hl7Index}");
            var components = rep.Split(compSep);
            if (components.Length == 1 && !rep.Contains(subSep))
            {
                fieldEl.Value = rep;
            }
            else
            {
                for (var c = 0; c < components.Length; c++)
                {
                    if (string.IsNullOrEmpty(components[c]))
                        continue;
                    fieldEl.Add(new XElement($"{segName}.{hl7Index}.{c + 1}", components[c].Replace(subSep, ' ')));
                }
            }

            segEl.Add(fieldEl);
        }
    }

    // ---- HL7 v2 → C-CDA R2.1 CCD -----------------------------------------------------------

    private static string ToCcd(string raw, string? correlationId)
    {
        XNamespace v3 = CdaNs;
        var msg = TryParse(raw);
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        var doc = new XElement(v3 + "ClinicalDocument",
            new XAttribute("xmlns", CdaNs),
            new XElement(v3 + "realmCode", new XAttribute("code", "US")),
            new XElement(v3 + "typeId",
                new XAttribute("root", "2.16.840.1.113883.1.3"),
                new XAttribute("extension", "POCD_HD000040")),
            // US Realm Header + CCD templateIds (C-CDA R2.1).
            TemplateId(v3, "2.16.840.1.113883.10.20.22.1.1", "2015-08-01"),
            TemplateId(v3, "2.16.840.1.113883.10.20.22.1.2", "2015-08-01"),
            new XElement(v3 + "id", new XAttribute("root", SanitizeId(correlationId))),
            new XElement(v3 + "code",
                new XAttribute("code", "34133-9"),
                new XAttribute("codeSystem", "2.16.840.1.113883.6.1"),
                new XAttribute("codeSystemName", "LOINC"),
                new XAttribute("displayName", "Summarization of Episode Note")),
            new XElement(v3 + "title", "Continuity of Care Document"),
            new XElement(v3 + "effectiveTime", new XAttribute("value", now)),
            new XElement(v3 + "confidentialityCode",
                new XAttribute("code", "N"),
                new XAttribute("codeSystem", "2.16.840.1.113883.5.25")),
            new XElement(v3 + "languageCode", new XAttribute("code", "en-US")),
            RecordTarget(v3, msg),
            Author(v3, now),
            Custodian(v3),
            new XElement(v3 + "component",
                new XElement(v3 + "structuredBody",
                    ResultsComponent(v3, msg, raw))));

        return Declare(new XDocument(doc));
    }

    private static XElement RecordTarget(XNamespace v3, Hl7V2Message? msg)
    {
        var mrn = msg?.GetValue("PID.3.1") ?? "UNKNOWN";
        var family = msg?.GetValue("PID.5.1") ?? "UNKNOWN";
        var given = msg?.GetValue("PID.5.2") ?? "";
        var gender = (msg?.GetValue("PID.8") ?? "").ToUpperInvariant() switch
        {
            "M" => "M",
            "F" => "F",
            _ => "UN",
        };
        var dob = NormalizeTs(msg?.GetValue("PID.7"));

        return new XElement(v3 + "recordTarget",
            new XElement(v3 + "patientRole",
                new XElement(v3 + "id",
                    new XAttribute("root", "2.16.840.1.113883.19.5"),
                    new XAttribute("extension", mrn)),
                new XElement(v3 + "addr", new XAttribute("use", "HP"),
                    new XElement(v3 + "country", "US")),
                new XElement(v3 + "patient",
                    new XElement(v3 + "name", new XAttribute("use", "L"),
                        new XElement(v3 + "given", given),
                        new XElement(v3 + "family", family)),
                    new XElement(v3 + "administrativeGenderCode",
                        new XAttribute("code", gender),
                        new XAttribute("codeSystem", "2.16.840.1.113883.5.1")),
                    new XElement(v3 + "birthTime",
                        dob is null ? new XAttribute("nullFlavor", "UNK") : new XAttribute("value", dob)))));
    }

    private static XElement Author(XNamespace v3, string now) =>
        new(v3 + "author",
            new XElement(v3 + "time", new XAttribute("value", now)),
            new XElement(v3 + "assignedAuthor",
                new XElement(v3 + "id", new XAttribute("root", "2.16.840.1.113883.19.5")),
                new XElement(v3 + "assignedAuthoringDevice",
                    new XElement(v3 + "manufacturerModelName", "Dialysis SmartConnect"),
                    new XElement(v3 + "softwareName", "SmartConnect Integration Engine"))));

    private static XElement Custodian(XNamespace v3) =>
        new(v3 + "custodian",
            new XElement(v3 + "assignedCustodian",
                new XElement(v3 + "representedCustodianOrganization",
                    new XElement(v3 + "id", new XAttribute("root", "2.16.840.1.113883.19.5")),
                    new XElement(v3 + "name", "Dialysis Care Network"))));

    private static XElement ResultsComponent(XNamespace v3, Hl7V2Message? msg, string raw)
    {
        var section = new XElement(v3 + "section",
            TemplateId(v3, "2.16.840.1.113883.10.20.22.2.3.1", "2015-08-01"),
            new XElement(v3 + "code",
                new XAttribute("code", "30954-2"),
                new XAttribute("codeSystem", "2.16.840.1.113883.6.1"),
                new XAttribute("codeSystemName", "LOINC"),
                new XAttribute("displayName", "Relevant diagnostic tests and/or laboratory data")),
            new XElement(v3 + "title", "Results"));

        var obx = ExtractObservations(msg);
        if (obx.Count == 0)
        {
            section.Add(new XElement(v3 + "text",
                "No structured laboratory results were present in the source message. " +
                "Original payload preserved below.\n\n" + raw));
            return new XElement(v3 + "component", section);
        }

        var table = new XElement(v3 + "table", new XAttribute("border", "1"),
            new XElement(v3 + "thead",
                new XElement(v3 + "tr",
                    new XElement(v3 + "th", "Test"),
                    new XElement(v3 + "th", "Value"),
                    new XElement(v3 + "th", "Units"))));
        var tbody = new XElement(v3 + "tbody");
        var organizer = new XElement(v3 + "organizer",
            new XAttribute("classCode", "BATTERY"),
            new XAttribute("moodCode", "EVN"),
            TemplateId(v3, "2.16.840.1.113883.10.20.22.4.1", "2015-08-01"),
            new XElement(v3 + "statusCode", new XAttribute("code", "completed")));

        foreach (var o in obx)
        {
            tbody.Add(new XElement(v3 + "tr",
                new XElement(v3 + "td", o.Name),
                new XElement(v3 + "td", o.Value),
                new XElement(v3 + "td", o.Units)));

            organizer.Add(new XElement(v3 + "component",
                new XElement(v3 + "observation",
                    new XAttribute("classCode", "OBS"),
                    new XAttribute("moodCode", "EVN"),
                    TemplateId(v3, "2.16.840.1.113883.10.20.22.4.2", "2015-08-01"),
                    new XElement(v3 + "code",
                        new XAttribute("code", o.Code),
                        new XAttribute("codeSystem", "2.16.840.1.113883.6.1"),
                        new XAttribute("codeSystemName", "LOINC"),
                        new XAttribute("displayName", o.Name)),
                    new XElement(v3 + "statusCode", new XAttribute("code", "completed")),
                    new XElement(v3 + "effectiveTime",
                        o.Time is null ? new XAttribute("nullFlavor", "UNK") : new XAttribute("value", o.Time)),
                    BuildValue(v3, o))));
        }

        table.Add(tbody);
        section.Add(new XElement(v3 + "text", table));
        section.Add(new XElement(v3 + "entry", organizer));
        return new XElement(v3 + "component", section);
    }

    private static XElement BuildValue(XNamespace v3, Observation o)
    {
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        if (decimal.TryParse(o.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
        {
            return new XElement(v3 + "value",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
                new XAttribute(xsi + "type", "PQ"),
                new XAttribute("value", num.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("unit", string.IsNullOrWhiteSpace(o.Units) ? "1" : o.Units));
        }

        return new XElement(v3 + "value",
            new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
            new XAttribute(xsi + "type", "ST"),
            o.Value);
    }

    // ---- HL7 v2 → FHIR R4 transaction Bundle ----------------------------------------------

    private static string ToFhirBundle(string raw, string? correlationId)
    {
        var msg = TryParse(raw);
        var entries = new List<object>();
        var patientUuid = $"urn:uuid:{DeterministicGuid("patient", correlationId)}";

        if (msg is not null)
        {
            var mrn = msg.GetValue("PID.3.1") ?? "UNKNOWN";
            var family = msg.GetValue("PID.5.1") ?? "UNKNOWN";
            var given = msg.GetValue("PID.5.2") ?? "";
            var gender = (msg.GetValue("PID.8") ?? "").ToUpperInvariant() switch
            {
                "M" => "male",
                "F" => "female",
                _ => "unknown",
            };
            var birthDate = NormalizeDate(msg.GetValue("PID.7"));

            entries.Add(new
            {
                fullUrl = patientUuid,
                resource = new
                {
                    resourceType = "Patient",
                    identifier = new[] { new { system = "urn:oid:2.16.840.1.113883.19.5", value = mrn } },
                    name = new[] { new { family, given = new[] { given } } },
                    gender,
                    birthDate,
                },
                request = new { method = "POST", url = "Patient" },
            });

            foreach (var o in ExtractObservations(msg))
            {
                var hasNumber = decimal.TryParse(
                    o.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num);
                entries.Add(new
                {
                    fullUrl = $"urn:uuid:{Guid.NewGuid()}",
                    resource = new
                    {
                        resourceType = "Observation",
                        status = "final",
                        code = new
                        {
                            coding = new[]
                            {
                                new { system = "http://loinc.org", code = o.Code, display = o.Name },
                            },
                        },
                        subject = new { reference = patientUuid },
                        effectiveDateTime = o.Iso,
                        valueQuantity = hasNumber
                            ? new
                            {
                                value = num,
                                unit = o.Units,
                                system = "http://unitsofmeasure.org",
                            }
                            : null,
                        valueString = hasNumber ? null : o.Value,
                    },
                    request = new { method = "POST", url = "Observation" },
                });
            }
        }
        else
        {
            entries.Add(new
            {
                fullUrl = $"urn:uuid:{Guid.NewGuid()}",
                resource = new
                {
                    resourceType = "Binary",
                    contentType = "text/plain",
                    data = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw)),
                },
                request = new { method = "POST", url = "Binary" },
            });
        }

        var bundle = new
        {
            resourceType = "Bundle",
            type = "transaction",
            timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            entry = entries,
        };

        return JsonSerializer.Serialize(bundle, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
    }

    // ---- shared helpers --------------------------------------------------------------------

    private sealed record Observation(string Code, string Name, string Value, string Units, string? Time, string? Iso);

    private static List<Observation> ExtractObservations(Hl7V2Message? msg)
    {
        var list = new List<Observation>();
        if (msg is null)
            return list;

        string? lastObrTime = null;
        foreach (var seg in msg.Segments)
        {
            if (seg.Name == "OBR")
                lastObrTime = NormalizeTs(Comp(seg, 7, 1));

            if (seg.Name != "OBX")
                continue;

            var code = Comp(seg, 3, 1);
            var name = Comp(seg, 3, 2);
            var value = Comp(seg, 5, 1);
            var units = Comp(seg, 6, 1);
            var time = NormalizeTs(Comp(seg, 14, 1)) ?? lastObrTime;
            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(value))
                continue;

            list.Add(new Observation(
                string.IsNullOrEmpty(code) ? "00000-0" : code,
                string.IsNullOrEmpty(name) ? code : name,
                value,
                units,
                time,
                ToIso(time)));
        }

        return list;
    }

    /// <summary>Reads a component from a non-MSH segment using HL7 1-based field/component indices.</summary>
    private static string Comp(Hl7Segment seg, int field, int component)
    {
        var fi = field - 1;
        if (fi < 0 || fi >= seg.Fields.Count)
            return "";
        var reps = seg.Fields[fi];
        if (reps.Length == 0 || reps[0].Length == 0)
            return "";
        var comps = reps[0];
        var ci = component - 1;
        return ci >= 0 && ci < comps.Length ? comps[ci] : "";
    }

    private static Hl7V2Message? TryParse(string raw)
    {
        try
        {
            return Hl7V2Message.Parse(raw);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return null;
        }
    }

    private static XElement TemplateId(XNamespace v3, string root, string extension) =>
        new(v3 + "templateId", new XAttribute("root", root), new XAttribute("extension", extension));

    /// <summary>HL7 v2 TS (yyyyMMdd[HHmmss]) → HL7 v3 TS (digits only), or null when absent.</summary>
    private static string? NormalizeTs(string? hl7)
    {
        if (string.IsNullOrWhiteSpace(hl7))
            return null;
        var digits = new string([.. hl7.Where(char.IsDigit)]);
        return digits.Length >= 8 ? digits : null;
    }

    private static string? NormalizeDate(string? hl7)
    {
        var ts = NormalizeTs(hl7);
        return ts is null ? null : $"{ts[..4]}-{ts.Substring(4, 2)}-{ts.Substring(6, 2)}";
    }

    private static string? ToIso(string? ts)
    {
        if (ts is null || ts.Length < 8)
            return null;
        var date = $"{ts[..4]}-{ts.Substring(4, 2)}-{ts.Substring(6, 2)}";
        if (ts.Length >= 14)
            return $"{date}T{ts.Substring(8, 2)}:{ts.Substring(10, 2)}:{ts.Substring(12, 2)}Z";
        return date;
    }

    private static Guid DeterministicGuid(string scope, string? seed)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(
            Encoding.UTF8.GetBytes($"{scope}:{seed ?? "anon"}"));
        return new Guid(bytes);
    }

    private static string SanitizeId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "message";
        var clean = new string([.. value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')]);
        return string.IsNullOrEmpty(clean) ? "message" : clean[..Math.Min(clean.Length, 64)];
    }

    private static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

    private static string Declare(XDocument doc)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
        });
        doc.Save(writer);
        writer.Flush();
        return sb.ToString().Replace("utf-16", "utf-8", StringComparison.OrdinalIgnoreCase);
    }
}
