using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Dialysis.SmartConnect.Documents;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class MessageDocumentExporterTests
{
    private const string Oru =
        "MSH|^~\\&|LAB|HOSPITAL|EHR|HOSPITAL|20260501120000||ORU^R01^ORU_R01|LAB00000001|P|2.5.1\r" +
        "PID|1||MRN-1042^^^HOSPITAL^MR||DEMO^PATIENT001||19700101|M\r" +
        "OBR|1|ORD000001||718-7^Hemoglobin^LN|||20260501115500\r" +
        "OBX|1|NM|718-7^Hemoglobin^LN||9.4|g/dL|||||F\r";

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Raw_Returns_Original_Bytes()
    {
        var doc = MessageDocumentExporter.Export(Bytes(Oru), "raw", "sim-1");

        Assert.Equal(Oru, Encoding.UTF8.GetString(doc.Content));
        Assert.EndsWith(".txt", doc.FileName);
    }

    [Fact]
    public void Hl7Xml_Is_Well_Formed_And_Carries_Pid()
    {
        var doc = MessageDocumentExporter.Export(Bytes(Oru), "xml", "sim-1");

        var xml = XDocument.Parse(Encoding.UTF8.GetString(doc.Content));
        Assert.Equal("HL7v2Message", xml.Root!.Name.LocalName);
        Assert.Contains(xml.Descendants(), e => e.Name.LocalName == "PID");
        Assert.EndsWith(".xml", doc.FileName);
    }

    [Fact]
    public void Ccd_Is_A_ClinicalDocument_With_Patient_And_Result()
    {
        var doc = MessageDocumentExporter.Export(Bytes(Oru), "cda", "sim-42");

        var xml = XDocument.Parse(Encoding.UTF8.GetString(doc.Content));
        XNamespace v3 = "urn:hl7-org:v3";
        Assert.Equal(v3 + "ClinicalDocument", xml.Root!.Name);
        // recordTarget carries the parsed MRN.
        Assert.Contains(
            xml.Descendants(v3 + "id"),
            e => e.Attribute("extension")?.Value == "MRN-1042");
        // The Results section surfaces the Hemoglobin observation.
        Assert.Contains(
            xml.Descendants(v3 + "observation"),
            o => o.Element(v3 + "code")?.Attribute("code")?.Value == "718-7");
        Assert.EndsWith(".cda.xml", doc.FileName);
    }

    [Fact]
    public void Fhir_Bundle_Has_Patient_And_Observation_Entries()
    {
        var doc = MessageDocumentExporter.Export(Bytes(Oru), "fhir", "sim-7");

        using var json = JsonDocument.Parse(doc.Content);
        var root = json.RootElement;
        Assert.Equal("Bundle", root.GetProperty("resourceType").GetString());
        Assert.Equal("transaction", root.GetProperty("type").GetString());

        var types = root.GetProperty("entry").EnumerateArray()
            .Select(e => e.GetProperty("resource").GetProperty("resourceType").GetString())
            .ToList();
        Assert.Contains("Patient", types);
        Assert.Contains("Observation", types);
    }

    [Fact]
    public void Non_Hl7_Payload_Degrades_Gracefully()
    {
        var doc = MessageDocumentExporter.Export(Bytes("not an hl7 message"), "cda", "x");

        // Still well-formed XML carrying the original text rather than throwing.
        var xml = XDocument.Parse(Encoding.UTF8.GetString(doc.Content));
        Assert.Contains("not an hl7 message", xml.ToString());
    }

    [Fact]
    public void Unknown_Format_Throws_ArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => MessageDocumentExporter.Export(Bytes(Oru), "pdf", "x"));
    }
}
