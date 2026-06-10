using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Transforms;

/// <summary>
/// Transform stage that extracts or restructures XML payloads using XPath.
/// Parameters JSON: { "xpath": "/root/element" } extracts element text,
/// or { "mode": "hl7v2-to-xml" [, "xpath": "..."] } parses an HL7 v2 payload into a structured XML
/// document and (optionally) drills into it with XPath.
/// </summary>
public sealed class XmlTransformStage : ITransformStage
{
    public string Kind => "xml-transform";

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var parametersJson = message.Metadata.TryGetValue("smartconnect.transform.parameters", out var p) ? p : null;
        if (string.IsNullOrWhiteSpace(parametersJson))
            return Task.FromResult(message);

        // Parse parameters as JSON
        JsonNode? parameters;
        try
        {
            parameters = JsonNode.Parse(parametersJson);
        }
        catch
        {
            return Task.FromResult(message);
        }

        var mode = parameters?["mode"]?.GetValue<string>();
        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);

        XDocument doc;
        if (string.Equals(mode, "hl7v2-to-xml", StringComparison.OrdinalIgnoreCase))
        {
            // hl7v2-to-xml: parse the HL7 v2 payload and emit a structured XML document. Optional
            // xpath can then drill into the generated XML, otherwise the full document is the
            // output.
            if (string.IsNullOrEmpty(payloadText) || !payloadText.StartsWith("MSH", StringComparison.Ordinal))
            {
                return Task.FromResult(message);
            }
            Hl7V2Message parsed;
            try
            {
                parsed = Hl7V2Message.Parse(payloadText);
            }
            catch (FormatException)
            {
                return Task.FromResult(message);
            }
            doc = Hl7V2XmlSerializer.ToXml(parsed);

            var hl7Xpath = parameters?["xpath"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(hl7Xpath))
            {
                // No XPath drill — emit the full HL7-XML document.
                var xmlBytes = Encoding.UTF8.GetBytes(doc.ToString());
                return Task.FromResult(message.CloneWithPayload(xmlBytes));
            }

            // Fall through to the shared XPath block below with the freshly-built document.
            return Task.FromResult(EvaluateXPath(message, doc, hl7Xpath));
        }

        try
        {
            doc = XDocument.Parse(payloadText);
        }
        catch (XmlException)
        {
            return Task.FromResult(message);
        }

        var xpath = parameters?["xpath"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(xpath))
            return Task.FromResult(message);

        return Task.FromResult(EvaluateXPath(message, doc, xpath));
    }

    private static IntegrationMessage EvaluateXPath(IntegrationMessage message, XDocument doc, string xpath)
    {
        var result = doc.XPathEvaluate(xpath);
        string outputText;

        if (result is IEnumerable<object> elements)
        {
            var list = elements.ToList();
            if (list.Count == 1 && list[0] is XElement singleEl)
            {
                outputText = singleEl.ToString();
            }
            else if (list.Count == 1 && list[0] is XAttribute attr)
            {
                outputText = attr.Value;
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var item in list)
                {
                    if (item is XElement el) sb.Append(el);
                    else if (item is XAttribute a) sb.Append(a.Value);
                    else sb.Append(item);
                }

                outputText = sb.ToString();
            }
        }
        else
        {
            outputText = result?.ToString() ?? "";
        }

        var resultBytes = Encoding.UTF8.GetBytes(outputText);
        return message.CloneWithPayload(resultBytes);
    }
}

/// <summary>
/// Lightweight HL7 v2.x → XML projection used by the <c>xml-transform</c> stage's
/// <c>hl7v2-to-xml</c> mode. Emits one top-level <c>HL7Message</c> root with one child element
/// per segment occurrence; each segment's fields become <c>F1</c>, <c>F2</c>, etc., and field
/// values are dropped in as text. Optimised for the common case (XPath consumers want to walk
/// segments by name and read field text) rather than fidelity to the HL7 XML standard.
/// </summary>
internal static class Hl7V2XmlSerializer
{
    public static XDocument ToXml(Hl7V2Message message)
    {
        var root = new XElement("HL7Message");
        foreach (var seg in message.Segments)
        {
            var segEl = new XElement(seg.Name);
            for (var f = 0; f < seg.Fields.Count; f++)
            {
                // Match the same 1-based numbering Hl7V2Message.GetValue uses (MSH offset +2,
                // other segments offset +1).
                var label = seg.Name == "MSH" ? f + 2 : f + 1;
                var fieldEl = new XElement($"F{label.ToString(CultureInfo.InvariantCulture)}");
                var repeats = seg.Fields[f];
                if (repeats.Length == 1)
                {
                    fieldEl.Value = JoinComponents(repeats[0]);
                }
                else
                {
                    foreach (var repeat in repeats)
                    {
                        fieldEl.Add(new XElement("R", JoinComponents(repeat)));
                    }
                }
                segEl.Add(fieldEl);
            }
            root.Add(segEl);
        }
        return new XDocument(root);
    }

    private static string JoinComponents(string[] components) =>
        components.Length switch
        {
            0 => string.Empty,
            1 => components[0],
            _ => string.Join('^', components),
        };
}
