using System.Text;
using Dialysis.SmartConnect.DataTypes;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class Hl7V2ParserTests
{
    private const string SampleMessage =
        "MSH|^~\\&|SendingApp|SendingFac|ReceivingApp|ReceivingFac|20260501120000||ADT^A01|12345|P|2.5\r" +
        "PID|||123456^^^MRN||Doe^John^M||19800101|M\r" +
        "PV1||I|ICU^01^01";

    [Fact]
    public void Parse_returns_correct_segment_count()
    {
        var parser = new Hl7V2Parser();
        var parsed = parser.Parse(Encoding.UTF8.GetBytes(SampleMessage));

        var hl7 = Assert.IsType<Hl7V2Message>(parsed);
        Assert.Equal(3, hl7.Segments.Count);
    }

    [Fact]
    public void GetValue_MSH9_returns_message_type()
    {
        var hl7 = Hl7V2Message.Parse(SampleMessage);

        var msgType = hl7.GetValue("MSH.9");

        Assert.Equal("ADT^A01", msgType);
    }

    [Fact]
    public void GetValue_PID5_component1_returns_last_name()
    {
        var hl7 = Hl7V2Message.Parse(SampleMessage);

        var lastName = hl7.GetValue("PID.5.1");

        Assert.Equal("Doe", lastName);
    }

    [Fact]
    public void GetValue_PID5_component2_returns_first_name()
    {
        var hl7 = Hl7V2Message.Parse(SampleMessage);

        var firstName = hl7.GetValue("PID.5.2");

        Assert.Equal("John", firstName);
    }

    [Fact]
    public void GetValue_nonexistent_segment_returns_null()
    {
        var hl7 = Hl7V2Message.Parse(SampleMessage);

        var result = hl7.GetValue("OBR.1");

        Assert.Null(result);
    }

    [Fact]
    public void SetValue_modifies_field()
    {
        var hl7 = Hl7V2Message.Parse(SampleMessage);

        hl7.SetValue("PID.5.1", "Smith");

        Assert.Equal("Smith", hl7.GetValue("PID.5.1"));
    }

    [Fact]
    public void Serialize_round_trips()
    {
        var hl7 = Hl7V2Message.Parse(SampleMessage);
        var serialized = hl7.Serialize();

        // Re-parse
        var reparsed = Hl7V2Message.Parse(serialized);
        Assert.Equal("ADT^A01", reparsed.GetValue("MSH.9"));
        Assert.Equal("Doe", reparsed.GetValue("PID.5.1"));
    }

    [Fact]
    public void GetValue_PV1_location()
    {
        var hl7 = Hl7V2Message.Parse(SampleMessage);

        var loc = hl7.GetValue("PV1.3.1");

        Assert.Equal("ICU", loc);
    }

    [Fact]
    public void Parse_invalid_message_throws()
    {
        var parser = new Hl7V2Parser();
        Assert.Throws<FormatException>(() => parser.Parse("NOT AN HL7 MESSAGE"u8));
    }
}
