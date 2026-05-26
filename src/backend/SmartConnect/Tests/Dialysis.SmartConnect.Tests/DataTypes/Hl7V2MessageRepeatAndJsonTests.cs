using System.Text.Json;
using Dialysis.SmartConnect.DataTypes;
using Xunit;

namespace Dialysis.SmartConnect.Tests.DataTypes;

/// <summary>
/// Covers the two API additions on <see cref="Hl7V2Message"/> the HL7v2 tutorial leans on:
/// <c>GetRepeatCount</c> (used to iterate repeating fields from a transformer script) and
/// <c>ToJson</c> (used to hand a structured snapshot to downstream consumers).
/// </summary>
public sealed class Hl7V2MessageRepeatAndJsonTests
{
    [Fact]
    public void Get_Repeat_Count_Returns_One_For_Single_Value_Field()
    {
        var msg = Hl7V2Message.Parse(
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-1|P|2.5\r" +
            "PID|||MRN-12345");

        Assert.Equal(1, msg.GetRepeatCount("PID.3"));
    }

    [Fact]
    public void Get_Repeat_Count_Returns_Count_For_Repeating_Identifier_List()
    {
        var msg = Hl7V2Message.Parse(
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-2|P|2.5\r" +
            "PID|||MRN-12345^^^HOSPITAL^MR~SSN-987654321^^^USA^SS~MEDREC-001^^^HOSPITAL^MR");

        Assert.Equal(3, msg.GetRepeatCount("PID.3"));
    }

    [Fact]
    public void Get_Repeat_Count_Returns_Zero_When_Segment_Or_Field_Missing()
    {
        var msg = Hl7V2Message.Parse(
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-3|P|2.5\r" +
            "PID|||MRN-12345");

        Assert.Equal(0, msg.GetRepeatCount("ZZZ.1"));
        Assert.Equal(0, msg.GetRepeatCount("PID.99"));
    }

    [Fact]
    public void To_Json_Emits_Segment_Code_Keys_With_Repeating_Segment_Arrays()
    {
        var msg = Hl7V2Message.Parse(
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-4|P|2.5\r" +
            "PID|||MRN-12345||DOE^JOHN\r" +
            "NTE|1||First note\r" +
            "NTE|2||Second note");

        var json = msg.ToJson();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("MSH", out var mshArr));
        Assert.Equal(1, mshArr.GetArrayLength());

        Assert.True(doc.RootElement.TryGetProperty("NTE", out var nteArr));
        Assert.Equal(2, nteArr.GetArrayLength()); // two NTE segments preserved as separate entries
    }

    [Fact]
    public void To_Json_Preserves_Field_Index_Labels_Matching_Path_Syntax()
    {
        var msg = Hl7V2Message.Parse(
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-5|P|2.5\r" +
            "PID|||MRN-12345");

        var json = msg.ToJson();
        using var doc = JsonDocument.Parse(json);

        // PID-3 should appear under key "3" (1-based, matching the GetValue("PID.3") syntax).
        var pidSegment = doc.RootElement.GetProperty("PID")[0];
        Assert.True(pidSegment.TryGetProperty("3", out var pid3));
        Assert.Equal(JsonValueKind.Array, pid3.ValueKind);

        // MSH fields should start at index "3" (1 = field-sep, 2 = encoding-chars are not stored).
        // MSH stores encoding chars at MSH.2 and the rest of the fields start at MSH.3. MSH.1
        // (the field separator itself) is implicit — derived from the character at position 3 of
        // the raw segment line — and isn't stored in the segment field list.
        var mshSegment = doc.RootElement.GetProperty("MSH")[0];
        Assert.True(mshSegment.TryGetProperty("2", out _));  // encoding characters
        Assert.True(mshSegment.TryGetProperty("3", out _));  // SendingApp lives at MSH.3
        Assert.False(mshSegment.TryGetProperty("1", out _)); // field separator — not a stored field
    }
}
