using System.Text;
using Dialysis.SmartConnect.DataTypes;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class Hl7V2ParserTests
{
    private const string Sample_Message =
        "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20260501120000||ADT^A01|12345|P|2.5\r" +
        "PID|||123456^^^MRN||Doe^John^M||19800101|M\r" +
        "PV1||I|ICU^01^01";

    [Fact]
    public void Parse_Returns_Correct_Segment_Count()
    {
        var parser = new Hl7V2Parser();
        var parsed = parser.Parse(Encoding.UTF8.GetBytes(Sample_Message));

        var hl7 = Assert.IsType<Hl7V2Message>(parsed);
        Assert.Equal(3, hl7.Segments.Count);
    }

    [Fact]
    public void Get_Value_Msh9_Returns_Message_Type()
    {
        var hl7 = Hl7V2Message.Parse(Sample_Message);

        var msgType = hl7.GetValue("MSH.9");

        Assert.Equal("ADT^A01", msgType);
    }

    [Fact]
    public void Get_Value_Pid5_Component1_Returns_Last_Name()
    {
        var hl7 = Hl7V2Message.Parse(Sample_Message);

        var lastName = hl7.GetValue("PID.5.1");

        Assert.Equal("Doe", lastName);
    }

    [Fact]
    public void Get_Value_Pid5_Component2_Returns_First_Name()
    {
        var hl7 = Hl7V2Message.Parse(Sample_Message);

        var firstName = hl7.GetValue("PID.5.2");

        Assert.Equal("John", firstName);
    }

    [Fact]
    public void Get_Value_Nonexistent_Segment_Returns_Null()
    {
        var hl7 = Hl7V2Message.Parse(Sample_Message);

        var result = hl7.GetValue("OBR.1");

        Assert.Null(result);
    }

    [Fact]
    public void Set_Value_Modifies_Field()
    {
        var hl7 = Hl7V2Message.Parse(Sample_Message);

        hl7.SetValue("PID.5.1", "Smith");

        Assert.Equal("Smith", hl7.GetValue("PID.5.1"));
    }

    [Fact]
    public void Serialize_Round_Trips()
    {
        var hl7 = Hl7V2Message.Parse(Sample_Message);
        var serialized = hl7.Serialize();

        // Re-parse
        var reparsed = Hl7V2Message.Parse(serialized);
        Assert.Equal("ADT^A01", reparsed.GetValue("MSH.9"));
        Assert.Equal("Doe", reparsed.GetValue("PID.5.1"));
    }

    [Fact]
    public void Get_Value_Pv1_Location()
    {
        var hl7 = Hl7V2Message.Parse(Sample_Message);

        var loc = hl7.GetValue("PV1.3.1");

        Assert.Equal("ICU", loc);
    }

    [Fact]
    public void Parse_Invalid_Message_Throws()
    {
        var parser = new Hl7V2Parser();
        Assert.Throws<FormatException>(() => parser.Parse("NOT AN HL7 MESSAGE"u8));
    }
}
