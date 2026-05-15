using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Dialysis.SmartConnect.Transforms;

/// <summary>
/// Transform stage that extracts or restructures XML payloads using XPath.
/// Parameters JSON: { "xpath": "/root/element" } extracts element text,
/// or { "xslt": "&lt;inline-xslt/&gt;" } for full XSLT (not implemented here—just XPath extraction).
/// </summary>
public sealed class XmlTransformStage : ITransformStage
{
    public string Kind => "xml-transform";

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        var parametersJson = message.Metadata.TryGetValue("smartconnect.transform.parameters", out var p) ? p : null;
        if (string.IsNullOrWhiteSpace(parametersJson))
            return Task.FromResult(message);

        var payloadText = Encoding.UTF8.GetString(message.Payload.Span);
        XDocument doc;
        try
        {
            doc = XDocument.Parse(payloadText);
        }
        catch (XmlException)
        {
            return Task.FromResult(message);
        }

        // Parse parameters as JSON to get xpath
        System.Text.Json.Nodes.JsonNode? parameters;
        try
        {
            parameters = System.Text.Json.Nodes.JsonNode.Parse(parametersJson);
        }
        catch
        {
            return Task.FromResult(message);
        }

        var xpath = parameters?["xpath"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(xpath))
            return Task.FromResult(message);

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
        return Task.FromResult(message.CloneWithPayload(resultBytes));
    }
}
