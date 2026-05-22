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
    public void Transform_Advertises_Delimited_Text_Kind()
    {
        Assert.Equal("delimited-text", new DelimitedTextTransformStage().Kind);
    }

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
