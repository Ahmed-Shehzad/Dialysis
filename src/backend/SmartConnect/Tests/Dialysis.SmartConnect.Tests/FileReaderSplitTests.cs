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
        var parameters = Build_Parameters(FileReaderSplitMode.Hl7V2);

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
        var parameters = Build_Parameters(FileReaderSplitMode.Hl7V2);

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

    [Fact]
    public void Split_Delimited_Text_Records_Drops_Header_And_Emits_One_Record_Per_Row()
    {
        var bytes = Encoding.UTF8.GetBytes(
            "PatientId,Analyte,Value\nMRN-1,Potassium,5.1\nMRN-2,Sodium,138\nMRN-3,Glucose,95\n");
        var parameters = Build_Delimited_Parameters(delimiter: ",", hasHeaderRow: true);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Equal(3, records.Count);
        Assert.Equal("MRN-1,Potassium,5.1", Encoding.UTF8.GetString(records[0]));
        Assert.Equal("MRN-2,Sodium,138", Encoding.UTF8.GetString(records[1]));
        Assert.Equal("MRN-3,Glucose,95", Encoding.UTF8.GetString(records[2]));
    }

    [Fact]
    public void Split_Delimited_Text_Records_Keeps_First_Row_When_Header_Disabled()
    {
        var bytes = Encoding.UTF8.GetBytes("MRN-1,Potassium,5.1\nMRN-2,Sodium,138\n");
        var parameters = Build_Delimited_Parameters(delimiter: ",", hasHeaderRow: false);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Equal(2, records.Count);
        Assert.Equal("MRN-1,Potassium,5.1", Encoding.UTF8.GetString(records[0]));
    }

    [Fact]
    public void Split_Delimited_Text_Records_Honours_Tab_Delimiter_Synonym()
    {
        var bytes = Encoding.UTF8.GetBytes("PatientId\tValue\nMRN-1\t5.1\nMRN-2\t4.8\n");
        var parameters = Build_Delimited_Parameters(delimiter: "tab", hasHeaderRow: true);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Equal(2, records.Count);
        Assert.Equal("MRN-1\t5.1", Encoding.UTF8.GetString(records[0]));
    }

    [Fact]
    public void Split_Delimited_Text_Records_Falls_Back_To_Whole_File_On_Empty()
    {
        // Header-only file → no data rows; falls back to whole-file dispatch so the inbound
        // path stays predictable instead of swallowing the file silently.
        var bytes = Encoding.UTF8.GetBytes("PatientId,Value\n");
        var parameters = Build_Delimited_Parameters(delimiter: ",", hasHeaderRow: true);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Single(records);
        Assert.Equal("PatientId,Value\n", Encoding.UTF8.GetString(records[0]));
    }

    [Fact]
    public void Split_Delimited_Text_Records_Fans_1000_Row_Csv_Into_1000_Records()
    {
        // Regression guard for D2 + L2 composition: a 1000-row CSV must fan out into 1000
        // record byte arrays without the streaming reader regressing to an accumulating one.
        var sb = new StringBuilder();
        sb.Append("PatientId,Analyte,Value\n");
        for (var i = 0; i < 1000; i++)
        {
            sb.Append("MRN-").Append(i).Append(",Potassium,").Append(4 + (i % 5) * 0.1).Append('\n');
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var parameters = Build_Delimited_Parameters(delimiter: ",", hasHeaderRow: true);

        var records = FileReaderSourceConnector.SplitPayload(bytes, parameters);

        Assert.Equal(1000, records.Count);
        Assert.StartsWith("MRN-0,", Encoding.UTF8.GetString(records[0]));
        Assert.StartsWith("MRN-999,", Encoding.UTF8.GetString(records[^1]));
    }

    [Fact]
    public void Parameters_Parse_Reads_Delimited_Text_Options()
    {
        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Directory"] = "/tmp",
            ["SplitMode"] = "DelimitedTextRecords",
            ["DelimitedTextDelimiter"] = "|",
            ["DelimitedTextHasHeaderRow"] = "false",
        };

        var parsed = FileReaderParameters.Parse(raw);

        Assert.Equal(FileReaderSplitMode.DelimitedTextRecords, parsed.SplitMode);
        Assert.Equal("|", parsed.DelimitedTextDelimiter);
        Assert.False(parsed.DelimitedTextHasHeaderRow);
    }

    private static FileReaderParameters Build_Parameters(
        FileReaderSplitMode splitMode,
        string? splitPattern = null) => new()
        {
            Directory = "/tmp",
            SplitMode = splitMode,
            SplitPattern = splitPattern,
        };

    private static FileReaderParameters Build_Delimited_Parameters(string delimiter, bool hasHeaderRow) => new()
    {
        Directory = "/tmp",
        SplitMode = FileReaderSplitMode.DelimitedTextRecords,
        DelimitedTextDelimiter = delimiter,
        DelimitedTextHasHeaderRow = hasHeaderRow,
    };
}
