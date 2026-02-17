using System.Text;
using System.Xml.Linq;
using NutrientPDF.Abstractions;
using NutrientPDF.Decorators;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NutrientPDF.Tests;

public sealed class XfdfExportTests
{
    [Fact]
    public async Task ExportPdfFormToXfdfAsync_delegates_and_produces_valid_xfdf_structure()
    {
        // Use a mock that simulates XFDF export to verify the decorator passes through and output is valid XML
        var inner = Substitute.For<INutrientPdfService>();
        inner.ExportPdfFormToXfdfAsync(Arg.Any<Stream>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var outputStream = (Stream)callInfo[1]!;
                var xfdf = """<?xml version="1.0" encoding="utf-8"?><xfdf xmlns="http://ns.adobe.com/xfdf/" xml:space="preserve"><fields><field name="field1"><value>value1</value></field><field name="field2"><value>Yes</value></field></fields></xfdf>""";
                await outputStream.WriteAsync(Encoding.UTF8.GetBytes(xfdf));
            });
        var sut = new ValidatingNutrientPdfService(inner);

        await using var output = new MemoryStream();
        await sut.ExportPdfFormToXfdfAsync(new MemoryStream(), output, false);
        output.Position = 0;

        var xml = XDocument.Load(output);
        xml.Root?.Name.LocalName.ShouldBe("xfdf");
        var fields = xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "fields");
        fields.ShouldNotBeNull();
        var fieldElements = fields.Elements().Where(e => e.Name.LocalName == "field").ToList();
        fieldElements.Count.ShouldBe(2);
        fieldElements[0].Attribute("name")?.Value.ShouldBe("field1");
        fieldElements[0].Elements().FirstOrDefault(e => e.Name.LocalName == "value")?.Value.ShouldBe("value1");
        fieldElements[1].Attribute("name")?.Value.ShouldBe("field2");
        fieldElements[1].Elements().FirstOrDefault(e => e.Name.LocalName == "value")?.Value.ShouldBe("Yes");
        await inner.Received().ExportPdfFormToXfdfAsync(Arg.Any<Stream>(), Arg.Any<Stream>(), false, Arg.Any<CancellationToken>());
    }
}
