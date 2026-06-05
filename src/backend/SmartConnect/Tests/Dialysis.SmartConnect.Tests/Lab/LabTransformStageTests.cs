using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Lab;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Lab;

/// <summary>
/// End-to-end coverage for the outbound lab transform stages (LabOrderPlacedIntegrationEvent JSON
/// in → HL7 v2.5 ORM^O01 / FHIR ServiceRequest bundle out) and the inbound ORU^R01 → LabResultFrame
/// mapper, with fail-soft pass-through for unexpected payloads.
/// </summary>
public sealed class LabTransformStageTests
{
    private static readonly FixedClock _clock =
        new(new DateTime(2026, 6, 5, 9, 0, 0, DateTimeKind.Utc));

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow);
    }

    private static IntegrationMessage Message(string json, string correlationId = "LAB-1") => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = correlationId,
        Payload = Encoding.UTF8.GetBytes(json),
        PayloadFormat = PayloadFormat.Json,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

    private static readonly Guid _patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static string OrderJson(object priority) => JsonSerializer.Serialize(new
    {
        EventId = Guid.NewGuid(),
        OccurredOn = new DateTime(2026, 6, 5, 8, 55, 0, DateTimeKind.Utc),
        SchemaVersion = 1,
        OrderId = Guid.NewGuid(),
        PatientId = _patientId,
        PlacerOrderNumber = "LAB-ABC123",
        Priority = priority,
        Specimen = "Serum",
        Tests = new[]
        {
            new { LoincCode = "2160-0", Display = "Creatinine" },
            new { LoincCode = "2823-3", Display = "Potassium" },
        },
        PlacedAtUtc = new DateTime(2026, 6, 5, 8, 55, 0, DateTimeKind.Utc),
    });

    [Fact]
    public async Task Orm_Stage_Builds_O01_From_Placed_Order_Async()
    {
        var stage = new LabOrderPlacedToHl7OrmTransformStage(_clock);

        // Priority serialised as the numeric enum ordinal (Stat = 1), as the outbox emits it.
        var result = await stage.TransformAsync(Message(OrderJson(1), "LAB-CTRL-9"), CancellationToken.None);
        var wire = Encoding.UTF8.GetString(result.Payload.Span);

        Assert.Equal(PayloadFormat.Utf8Text, result.PayloadFormat);
        Assert.Contains("|ORM^O01^ORM_O01|LAB-CTRL-9|P|2.5|", wire);
        Assert.Contains($"PID|||{_patientId}^^^^MR", wire);
        Assert.Contains("ORC|NW|LAB-ABC123|", wire);
        Assert.Contains("^^^^^S", wire); // STAT priority on ORC-7
        Assert.Contains("2160-0^Creatinine^LN", wire);
        Assert.Contains("2823-3^Potassium^LN", wire);
        Assert.Contains("Serum", wire); // OBR-15 specimen
    }

    [Fact]
    public async Task Orm_Stage_Marks_Routine_Priority_From_String_Enum_Async()
    {
        var stage = new LabOrderPlacedToHl7OrmTransformStage(_clock);

        // Priority serialised as the string enum name ("Routine"), as a re-serialisation might emit.
        var result = await stage.TransformAsync(Message(OrderJson("Routine")), CancellationToken.None);
        var wire = Encoding.UTF8.GetString(result.Payload.Span);

        Assert.Contains("^^^^^R", wire); // routine priority
        Assert.DoesNotContain("^^^^^S", wire);
    }

    [Fact]
    public async Task Fhir_Stage_Builds_Service_Request_Bundle_From_Placed_Order_Async()
    {
        var stage = new LabOrderPlacedToFhirServiceRequestStage(_clock);

        var result = await stage.TransformAsync(Message(OrderJson("Stat")), CancellationToken.None);
        var json = Encoding.UTF8.GetString(result.Payload.Span);

        Assert.Equal(PayloadFormat.Json, result.PayloadFormat);
        Assert.Contains("\"resourceType\":\"Bundle\"", json);
        Assert.Contains("\"resourceType\":\"ServiceRequest\"", json);
        Assert.Contains("\"priority\":\"stat\"", json);
        Assert.Contains("2160-0", json);
        Assert.Contains("2823-3", json);
        Assert.Contains($"Patient/{_patientId}", json);
        Assert.Contains("LAB-ABC123", json); // placer order number identifier
    }

    [Fact]
    public async Task Orm_Stage_Passes_Through_When_No_Tests_Async()
    {
        var stage = new LabOrderPlacedToHl7OrmTransformStage(_clock);
        var json = JsonSerializer.Serialize(new
        {
            PatientId = _patientId,
            PlacerOrderNumber = "LAB-EMPTY",
            Priority = 0,
            Tests = Array.Empty<object>(),
        });

        var result = await stage.TransformAsync(Message(json), CancellationToken.None);
        var payload = Encoding.UTF8.GetString(result.Payload.Span);
        Assert.DoesNotContain("ORM^O01", payload);
        Assert.Contains("LAB-EMPTY", payload); // unchanged input
    }

    [Fact]
    public async Task Orm_Stage_Passes_Through_Non_Json_Payload_Async()
    {
        var stage = new LabOrderPlacedToHl7OrmTransformStage(_clock);
        var result = await stage.TransformAsync(Message("not-json"), CancellationToken.None);
        Assert.Equal("not-json", Encoding.UTF8.GetString(result.Payload.Span));
    }

    [Fact]
    public void Oru_Mapper_Projects_Result_Frame_From_R01()
    {
        const string oru =
            "MSH|^~\\&|LIS||DIALYSIS||20260605101500||ORU^R01^ORU_R01|MSG-7|P|2.5\r" +
            "PID|||MRN-555^^^^MR\r" +
            "ORC|RE|LAB-ABC123|FILL-7788\r" +
            "OBR|1|LAB-ABC123|FILL-7788|2160-0^Creatinine^LN|||20260605101000\r" +
            "OBX|1|NM|2160-0^Creatinine^LN||1.1|mg/dL|0.6-1.3|N|||F\r" +
            "OBX|2|NM|2823-3^Potassium^LN||5.9|mmol/L|3.5-5.1|H|||F\r";

        var frame = Hl7V2OruToLabResultMapper.TryMap(Hl7V2Message.Parse(oru));

        Assert.NotNull(frame);
        Assert.Equal("LAB-ABC123", frame!.PlacerOrderNumber);
        Assert.Equal("FILL-7788", frame.FillerOrderNumber);
        Assert.Equal("MRN-555", frame.PatientIdentifier);
        Assert.True(frame.IsFinal);
        Assert.Equal(2, frame.Observations.Count);
        Assert.Equal(new DateTime(2026, 6, 5, 10, 10, 0, DateTimeKind.Utc), frame.ResultedAtUtc);

        var potassium = frame.Observations[1];
        Assert.Equal("2823-3", potassium.Code);
        Assert.Equal("Potassium", potassium.Display);
        Assert.Equal("5.9", potassium.Value);
        Assert.Equal("mmol/L", potassium.Unit);
        Assert.Equal("3.5-5.1", potassium.ReferenceRange);
        Assert.Equal("H", potassium.Interpretation);
    }

    [Fact]
    public void Oru_Mapper_Returns_Null_Without_Observations()
    {
        const string noObx =
            "MSH|^~\\&|LIS||DIALYSIS||20260605101500||ORU^R01^ORU_R01|MSG-8|P|2.5\r" +
            "PID|||MRN-555^^^^MR\r" +
            "ORC|RE|LAB-XYZ\r" +
            "OBR|1|LAB-XYZ\r";

        Assert.Null(Hl7V2OruToLabResultMapper.TryMap(Hl7V2Message.Parse(noObx)));
    }
}
