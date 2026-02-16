using System.Collections.Generic;
using System.Linq;
using Bogus;
using Dialysis.HisIntegration.Features.AdtSync;
using Shouldly;
using Xunit;

namespace Dialysis.HisIntegration.UnitTests;

public sealed class AdtMessageParserTests
{
    private readonly AdtMessageParser _parser = new();
    private readonly Faker _faker = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Parse_null_or_whitespace_returns_null(string? input)
    {
        _parser.Parse(input!).ShouldBeNull();
    }

    [Fact]
    public void Parse_no_MSH_segment_returns_null()
    {
        _parser.Parse("PID|1||MRN||Doe^John").ShouldBeNull();
        _parser.Parse("OBX|1||8480-6|120|mmHg").ShouldBeNull();
        _parser.Parse("not valid hl7").ShouldBeNull();
        _parser.Parse(_faker.Lorem.Sentence()).ShouldBeNull();
    }

    [Fact]
    public void Parse_MSH_only_returns_minimal_data()
    {
        var msg = "MSH|^~\\&|HIS|HOSP|PDMS|CLINIC|20240115120000||ADT^A01|MSG001|P|2.5";
        var result = _parser.Parse(msg);
        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe("ADT^A01");
        result.Mrn.ShouldBeNull();
        result.FamilyName.ShouldBeNull();
    }

    [Fact]
    public void Parse_full_ADT_with_PID_and_PV1_extracts_all_fields()
    {
        var mrn = "MRN" + _faker.Random.AlphaNumeric(8);
        var family = _faker.Name.LastName();
        var given = _faker.Name.FirstName();
        var dob = _faker.Date.Past(80).ToString("yyyyMMdd");
        var pv1Parts = new List<string> { "PV1", "1", "I", "^Ward^", "", "", "", "^Smith^Jane" };
        pv1Parts.AddRange(Enumerable.Repeat("", 11));
        pv1Parts.Add("EID123");
        var pv1Line = string.Join('|', pv1Parts);
        var lines = new[]
        {
            "MSH|^~\\&|HIS|HOSP|PDMS|CLINIC|20240115120000||ADT^A01|MSG001|P|2.5",
            $"PID|1||{mrn}^^^HOSP^MR||{family}^{given}||{dob}|M",
            pv1Line
        };
        var msg = string.Join("\r\n", lines);
        var result = _parser.Parse(msg);

        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe("ADT^A01");
        result.Mrn.ShouldBe(mrn);
        result.FamilyName.ShouldBe(family);
        result.GivenName.ShouldBe(given);
        result.BirthDate.ShouldBe(dob);
        result.Gender.ShouldBe("M");
        result.EncounterId.ShouldBe("EID123");
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Parse_handles_various_line_endings(string separator)
    {
        var lines = new[]
        {
            "MSH|^~\\&|HIS|HOSP|PDMS|CLINIC|20240115120000||ADT^A01|MSG001|P|2.5",
            "PID|1||MRN123^^^HOSP^MR||Doe^John||19800115|M"
        };
        var msg = string.Join(separator, lines);
        var result = _parser.Parse(msg);
        result.ShouldNotBeNull();
        result!.MessageType.ShouldBe("ADT^A01");
    }

    [Fact]
    public void Parse_PID_with_minimal_fields()
    {
        var msg = "MSH|^~\\&|HIS|HOSP|||20240115120000||ADT^A01|MSG001|P|2.5\r\nPID|1";
        var result = _parser.Parse(msg);
        result.ShouldNotBeNull();
        result!.Mrn.ShouldBeNull();
    }

    [Fact]
    public void Parse_PID_field_3_extracts_MRN_from_CX_format()
    {
        var mrn = _faker.Random.AlphaNumeric(10);
        var msg = $"MSH|^~\\&|HIS|HOSP|||20240115120000||ADT^A01|MSG001|P|2.5\r\nPID|1||{mrn}^^^HOSP^MR";
        var result = _parser.Parse(msg);
        result.ShouldNotBeNull();
        result!.Mrn.ShouldBe(mrn);
    }

    [Fact]
    public void Parse_PID_field_5_extracts_name_FamilyName_GivenName()
    {
        var family = _faker.Name.LastName();
        var given = _faker.Name.FirstName();
        var msg = $"MSH|^~\\&|HIS|HOSP|||20240115120000||ADT^A01|MSG001|P|2.5\r\nPID|1||||{family}^{given}||";
        var result = _parser.Parse(msg);
        result.ShouldNotBeNull();
        result!.FamilyName.ShouldBe(family);
        result.GivenName.ShouldBe(given);
    }

    [Fact]
    public void Parse_PV1_extracts_encounter_ward_attending()
    {
        var pv1Parts = new List<string> { "PV1", "1", "I", "ICU^Bed1^Room", "", "", "", "Smith^Jane^MD" };
        pv1Parts.AddRange(Enumerable.Repeat("", 11));
        pv1Parts.Add("ENC001");
        pv1Parts.AddRange(Enumerable.Repeat("", 24));
        pv1Parts.Add("202401150800");
        pv1Parts.Add("202401151600");
        var pv1 = string.Join('|', pv1Parts);
        var msg = "MSH|^~\\&|HIS|HOSP|||20240115120000||ADT^A01|MSG001|P|2.5\r\n" + pv1;
        var result = _parser.Parse(msg);
        result.ShouldNotBeNull();
        result!.EncounterId.ShouldBe("ENC001");
        result.Ward.ShouldBe("ICU^Bed1^Room");
        result.AdmitDateTime.ShouldBe("202401150800");
        result.DischargeDateTime.ShouldBe("202401151600");
    }

    [Fact]
    public void Parse_MSH_field_9_empty_when_short()
    {
        var msg = "MSH|^~\\&|HIS";
        var result = _parser.Parse(msg);
        result.ShouldNotBeNull();
        result!.MessageType.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_multiple_message_types()
    {
        foreach (var mType in new[] { "ADT^A01", "ADT^A02", "ADT^A03", "ADT^A08", "ADT^A11" })
        {
            var msg = $"MSH|^~\\&|HIS|HOSP|||20240115120000||{mType}|MSG001|P|2.5";
            var result = _parser.Parse(msg);
            result.ShouldNotBeNull();
            result!.MessageType.ShouldBe(mType);
        }
    }
}
