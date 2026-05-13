using Dialysis.SmartConnect.CodeTemplates;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class MirthXmlCodeTemplateImporterTests
{
    [Fact]
    public void Imports_one_library_with_two_templates()
    {
        const string xml = """
        <list>
          <codeTemplateLibrary>
            <id>11111111-1111-4111-8111-111111111111</id>
            <name>PatientHelpers</name>
            <description>HL7 patient utilities</description>
            <revision>3</revision>
            <enabledChannelIds>
              <string>22222222-2222-4222-8222-222222222222</string>
            </enabledChannelIds>
            <codeTemplates>
              <codeTemplate>
                <id>33333333-3333-4333-8333-333333333333</id>
                <name>extractMrn</name>
                <revision>1</revision>
                <properties class="com.mirth.connect.model.codetemplates.BasicCodeTemplateProperties">
                  <code><![CDATA[function extractMrn(msg){ return msg.PID && msg.PID.PID3; }]]></code>
                  <contextSet>
                    <contextType>SOURCE_TRANSFORMER</contextType>
                    <contextType>DESTINATION_TRANSFORMER</contextType>
                  </contextSet>
                </properties>
              </codeTemplate>
              <codeTemplate>
                <id>44444444-4444-4444-8444-444444444444</id>
                <name>formatPid</name>
                <revision>1</revision>
                <properties class="com.mirth.connect.model.codetemplates.BasicCodeTemplateProperties">
                  <code>function formatPid(p){ return p.toUpperCase(); }</code>
                  <contextSet>
                    <contextType>DESTINATION_TRANSFORMER</contextType>
                  </contextSet>
                </properties>
              </codeTemplate>
            </codeTemplates>
          </codeTemplateLibrary>
        </list>
        """;

        var importer = new MirthXmlCodeTemplateImporter();
        var libs = importer.Import(xml);

        Assert.Single(libs);
        var lib = libs[0];
        Assert.Equal(Guid.Parse("11111111-1111-4111-8111-111111111111"), lib.Id);
        Assert.Equal("PatientHelpers", lib.Name);
        Assert.Equal(2, lib.Templates.Count);
        Assert.Equal("extractMrn", lib.Templates[0].Name);
        Assert.Contains(CodeTemplateContext.SourceTransformer, lib.Templates[0].Contexts);
        Assert.Contains(CodeTemplateContext.DestinationTransformer, lib.Templates[0].Contexts);
        Assert.Single(lib.LinkedFlowIds);
    }

    [Fact]
    public void Imports_numeric_context_ordinals_as_fallback()
    {
        const string xml = """
        <list>
          <codeTemplateLibrary>
            <id>55555555-5555-4555-8555-555555555555</id>
            <name>NumericContextsLib</name>
            <codeTemplates>
              <codeTemplate>
                <id>66666666-6666-4666-8666-666666666666</id>
                <name>n</name>
                <properties>
                  <code>function n(){}</code>
                  <contextSet>
                    <int>11</int>
                    <int>14</int>
                  </contextSet>
                </properties>
              </codeTemplate>
            </codeTemplates>
          </codeTemplateLibrary>
        </list>
        """;

        var importer = new MirthXmlCodeTemplateImporter();
        var libs = importer.Import(xml);
        Assert.Contains(CodeTemplateContext.SourceTransformer, libs[0].Templates[0].Contexts);
        Assert.Contains(CodeTemplateContext.DestinationTransformer, libs[0].Templates[0].Contexts);
    }

    [Fact]
    public void Malformed_xml_throws_ArgumentException()
    {
        var importer = new MirthXmlCodeTemplateImporter();
        Assert.Throws<ArgumentException>(() => importer.Import("<not closed"));
    }

    [Fact]
    public void Empty_xml_throws_ArgumentException()
    {
        var importer = new MirthXmlCodeTemplateImporter();
        Assert.Throws<ArgumentException>(() => importer.Import(""));
    }
}
