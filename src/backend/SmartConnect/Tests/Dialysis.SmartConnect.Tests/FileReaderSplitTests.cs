using System.Text;
using Dialysis.SmartConnect.Inbound.FileReader;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Slice D2: <see cref="FileReaderSourceConnector.SplitPayload"/> turns a multi-record file
/// into one byte array per record, per the configured <see cref="FileReaderSplitMode"/>.
/// The dispatcher then tags each emitted message with slice D's batch context.
/// </summary>
public sealed class FileReaderSplitTests
{
    [Fact]
    public void Split_Mode_None_Returns_The_Whole_File_As_One_Record()
    {
        var bytes = "line one\nline two"u8.ToArray();
        var parameters = Build_Parameters(FileReaderSplitMode.None);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Single(records);
        Assert.Equal(bytes, records[0]);
    }

    [Fact]
    public void Line_Mode_Splits_On_Lf_And_Crlf_Skipping_Blank_Rows()
    {
        var bytes = "patient,value\nMRN-1,5.1\r\n\nMRN-2,4.8\n"u8.ToArray();
        var parameters = Build_Parameters(FileReaderSplitMode.Line);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Equal(3, records.Count);
        Assert.Equal("patient,value", Encoding.UTF8.GetString(records[0]));
        Assert.Equal("MRN-1,5.1", Encoding.UTF8.GetString(records[1]));
        Assert.Equal("MRN-2,4.8", Encoding.UTF8.GetString(records[2]));
    }

    [Fact]
    public void Hl_7v2_Mode_Splits_At_Each_Msh_Anchor()
    {
        const string a =
            "MSH|^~\\&|MachineA|FAC|||20260524100000||ORU^R40|MSG-1|P|2.6\r" +
            "PID|||MRN-1\r" +
            "OBX|1|NM|29463-7^Body weight^LN||72.4|kg\r";
        const string b =
            "MSH|^~\\&|MachineA|FAC|||20260524100100||ORU^R40|MSG-2|P|2.6\r" +
            "PID|||MRN-2\r";
        var bytes = Encoding.UTF8.GetBytes(a + b);
        var parameters = Build_Parameters(FileReaderSplitMode.Hl7v2);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Equal(2, records.Count);
        Assert.StartsWith("MSH|^~\\&|MachineA|FAC|||20260524100000", Encoding.UTF8.GetString(records[0]), StringComparison.Ordinal);
        Assert.Contains("MSG-2", Encoding.UTF8.GetString(records[1]));
    }

    [Fact]
    public void Hl_7v2_Mode_Returns_Whole_File_When_Only_One_Message()
    {
        const string single = "MSH|^~\\&|MachineA|FAC|||20260524100000||ORU^R40|MSG-1|P|2.6\rPID|||MRN-1\r";
        var bytes = Encoding.UTF8.GetBytes(single);
        var parameters = Build_Parameters(FileReaderSplitMode.Hl7v2);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Single(records);
    }

    [Fact]
    public void Regex_Mode_Splits_On_The_Configured_Pattern()
    {
        var bytes = "alpha---bravo---charlie"u8.ToArray();
        var parameters = Build_Parameters(FileReaderSplitMode.Regex, splitPattern: "-{3}");

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Equal(3, records.Count);
        Assert.Equal("alpha", Encoding.UTF8.GetString(records[0]));
        Assert.Equal("bravo", Encoding.UTF8.GetString(records[1]));
        Assert.Equal("charlie", Encoding.UTF8.GetString(records[2]));
    }

    [Fact]
    public void Parameters_Parse_Rejects_Regex_Mode_Without_Pattern()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Directory"] = "/tmp",
            ["SplitMode"] = "Regex",
        };

        Assert.Throws<ArgumentException>(() => FileReaderParameters.Parse(raw));
    }

    [Fact]
    public void Parameters_Parse_Defaults_Split_Mode_To_None()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Directory"] = "/tmp",
        };

        var parsed = FileReaderParameters.Parse(raw);

        Assert.Equal(FileReaderSplitMode.None, parsed.SplitMode);
        Assert.Null(parsed.SplitPattern);
    }

    private static FileReaderParameters Build_Parameters(
        FileReaderSplitMode splitMode,
        string? splitPattern = null) => new()
    {
        Directory = "/tmp",
        SplitMode = splitMode,
        SplitPattern = splitPattern,
    };
}
