using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Transforms;

/// <summary>
/// Slice L: parse delimited text (CSV / TSV / pipe) into JSON so downstream JSONPath /
/// mapper / JavaScript transforms can address the cells by header name or position
/// without writing custom parsers. Covers the dialysis-relevant case of lab-result CSV
/// drops that aren't HL7v2.
/// </summary>
public sealed class DelimitedTextTransformStageTests
{
    [Fact]
    public async Task Transform_Emits_Array_Of_Objects_When_Header_Row_Present_Async()
    {
        var message = Build_Message("PatientId,Analyte,Value\nMRN-1,Potassium,5.1\nMRN-2,Sodium,138\n");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Equal(2, json.GetArrayLength());
        Assert.Equal("MRN-1", json[0].GetProperty("PatientId").GetString());
        Assert.Equal("Potassium", json[0].GetProperty("Analyte").GetString());
        Assert.Equal("5.1", json[0].GetProperty("Value").GetString());
        Assert.Equal("138", json[1].GetProperty("Value").GetString());
    }

    [Fact]
    public async Task Transform_Emits_Array_Of_Arrays_When_Header_Row_Disabled_Async()
    {
        var message = Build_Message("MRN-1,5.1\nMRN-2,4.8\n", """{"hasHeaderRow":false}""");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal(2, json.GetArrayLength());
        Assert.Equal("MRN-1", json[0][0].GetString());
        Assert.Equal("5.1", json[0][1].GetString());
    }

    [Fact]
    public async Task Transform_Honours_Tab_Delimiter_Synonym_Async()
    {
        var message = Build_Message("PatientId\tValue\nMRN-1\t5.1\n", """{"delimiter":"tab"}""");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal("MRN-1", json[0].GetProperty("PatientId").GetString());
        Assert.Equal("5.1", json[0].GetProperty("Value").GetString());
    }

    [Fact]
    public async Task Transform_Honours_Pipe_Delimiter_Async()
    {
        var message = Build_Message("PatientId|Value\nMRN-1|5.1\n", """{"delimiter":"|"}""");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal("MRN-1", json[0].GetProperty("PatientId").GetString());
        Assert.Equal("5.1", json[0].GetProperty("Value").GetString());
    }

    [Fact]
    public async Task Transform_Honours_Quoted_Fields_With_Embedded_Commas_Async()
    {
        // RFC 4180: a comma inside double quotes is part of the cell, not a delimiter.
        // The escaped "" should decode to a single quote inside the cell.
        var message = Build_Message("Name,Note\n\"Doe, John\",\"says \"\"hi\"\"\"\n");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal("Doe, John", json[0].GetProperty("Name").GetString());
        Assert.Equal("says \"hi\"", json[0].GetProperty("Note").GetString());
    }

    [Fact]
    public async Task Transform_Skips_Blank_Lines_By_Default_Async()
    {
        var message = Build_Message("PatientId,Value\nMRN-1,5.1\n\n\nMRN-2,4.8\n");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        // Two data rows despite three blank separator lines.
        Assert.Equal(2, json.GetArrayLength());
    }

    [Fact]
    public async Task Transform_Trims_Whitespace_By_Default_Async()
    {
        var message = Build_Message("PatientId, Value\nMRN-1,   5.1   \n");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal("5.1", json[0].GetProperty("Value").GetString());
    }

    [Fact]
    public void Transform_Advertises_Delimited_Text_Kind() => Assert.Equal("delimited-text", new DelimitedTextTransformStage().Kind);

    [Fact]
    public async Task Output_Format_Ndjson_Emits_One_Object_Per_Line_Async()
    {
        // Slice L2: NDJSON output mode for large-file streaming.
        var message = Build_Message(
            "PatientId,Value\nMRN-1,5.1\nMRN-2,4.8\n",
            """{"outputFormat":"ndjson"}""");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var payload = Encoding.UTF8.GetString(transformed.Payload.Span);
        // No enclosing array characters around the records.
        Assert.False(payload.TrimStart().StartsWith('['));
        var lines = payload.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("MRN-1", JsonDocument.Parse(lines[0]).RootElement.GetProperty("PatientId").GetString());
        Assert.Equal("4.8", JsonDocument.Parse(lines[1]).RootElement.GetProperty("Value").GetString());
    }

    [Fact]
    public async Task Output_Format_Ndjson_With_No_Header_Emits_Per_Row_Arrays_Async()
    {
        var message = Build_Message(
            "MRN-1,5.1\nMRN-2,4.8\n",
            """{"hasHeaderRow":false,"outputFormat":"ndjson"}""");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var lines = Encoding.UTF8.GetString(transformed.Payload.Span)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("MRN-1", JsonDocument.Parse(lines[0]).RootElement[0].GetString());
        Assert.Equal("4.8", JsonDocument.Parse(lines[1]).RootElement[1].GetString());
    }

    /// <summary>
    /// Slice L2 streaming pass: the parser must handle large inputs (≳ 5 MB / 50k rows)
    /// without OOM. The previous implementation materialised the full line array + a
    /// `List&lt;string[]&gt;` of every row before projection — peak working-set ≈ 3× file size.
    /// The streaming pass reads + projects row-by-row; peak should stay ≈ 1× file size.
    /// We don't measure allocation here (no benchmark harness in CI) — instead we assert
    /// correctness on a large input, which is the test that breaks if the row iterator
    /// regresses to an accumulating one and the GC pressure tips the runner over.
    /// </summary>
    [Fact]
    public async Task Transform_Handles_Large_Csv_Without_Materialising_All_Rows_Async()
    {
        const int rowCount = 50_000;
        var sb = new StringBuilder(rowCount * 50);
        sb.Append("PatientId,Analyte,Value\n");
        for (var i = 0; i < rowCount; i++)
        {
            sb.Append("MRN-").Append(i).Append(",Potassium,").Append(4 + (i % 5) * 0.1).Append('\n');
        }
        var message = Build_Message(sb.ToString(), """{"outputFormat":"ndjson"}""");
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        // NDJSON output is one row per line; counting them is the simplest correctness check.
        var output = Encoding.UTF8.GetString(transformed.Payload.Span);
        var newlineCount = output.Count(c => c == '\n');
        Assert.Equal(rowCount, newlineCount);
        // First and last rows survive end-to-end with the right PatientId.
        var firstNewline = output.IndexOf('\n');
        var firstRow = JsonDocument.Parse(output.AsSpan(0, firstNewline).ToString()).RootElement;
        Assert.Equal("MRN-0", firstRow.GetProperty("PatientId").GetString());
        var lastNewline = output.LastIndexOf('\n', output.Length - 2);
        var lastRow = JsonDocument.Parse(output.AsSpan(lastNewline + 1, output.Length - lastNewline - 2).ToString()).RootElement;
        Assert.Equal("MRN-49999", lastRow.GetProperty("PatientId").GetString());
    }

    [Fact]
    public async Task Transform_Streams_Array_Output_For_Large_Input_Async()
    {
        // Same input shape, default array output. Verifies the array-mode streaming pump
        // wraps the row sequence in a single JSON array and doesn't truncate or duplicate.
        const int rowCount = 20_000;
        var sb = new StringBuilder(rowCount * 50);
        sb.Append("PatientId,Value\n");
        for (var i = 0; i < rowCount; i++)
        {
            sb.Append("MRN-").Append(i).Append(',').Append(i % 100).Append('\n');
        }
        var message = Build_Message(sb.ToString());
        var stage = new DelimitedTextTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span));
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(rowCount, doc.RootElement.GetArrayLength());
        Assert.Equal("MRN-0", doc.RootElement[0].GetProperty("PatientId").GetString());
        Assert.Equal("MRN-19999", doc.RootElement[rowCount - 1].GetProperty("PatientId").GetString());
    }

    private static IntegrationMessage Build_Message(string payload, string? parametersJson = null)
    {
        var metadata = ImmutableDictionary<string, string>.Empty;
        if (!string.IsNullOrWhiteSpace(parametersJson))
            metadata = metadata.Add(DelimitedTextTransformStage.ParametersMetadataKey, parametersJson);
        return new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "C",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = metadata,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
