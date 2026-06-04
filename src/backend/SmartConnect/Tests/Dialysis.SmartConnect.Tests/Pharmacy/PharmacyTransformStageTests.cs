using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Pharmacy;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Pharmacy;

/// <summary>
/// End-to-end coverage for the two outbound pharmacy transform stages: event JSON in,
/// HL7 v2.5 wire frame out, with fail-soft pass-through for unexpected payloads.
/// </summary>
public sealed class PharmacyTransformStageTests
{
    private static readonly FixedClock _clock =
        new(new DateTime(2026, 5, 22, 14, 30, 5, DateTimeKind.Utc));

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow);
    }

    private static IntegrationMessage Message(string json, string correlationId = "corr-1") => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = correlationId,
        Payload = Encoding.UTF8.GetBytes(json),
        PayloadFormat = PayloadFormat.Json,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Ras_Stage_Builds_O17_From_Administered_Event_Async()
    {
        var stage = new MedicationAdministeredToHl7RasTransformStage(_clock);
        var json = JsonSerializer.Serialize(new
        {
            EntryId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PatientId = "MRN-9001",
            MedicationCodeSystem = "RxNorm",
            MedicationCode = "855332",
            MedicationDisplay = "Warfarin 5 MG Oral Tablet",
            DoseQuantity = 5m,
            DoseUnit = "mg",
            Route = "PO",
            AdministeredAtUtc = new DateTime(2026, 5, 22, 14, 30, 0, DateTimeKind.Utc),
            AdministeredBySub = "nurse-jones",
        });

        var result = await stage.TransformAsync(Message(json, "MSG-100"), CancellationToken.None);
        var wire = Encoding.UTF8.GetString(result.Payload.Span);

        Assert.Equal(PayloadFormat.Utf8Text, result.PayloadFormat);
        Assert.Contains("|RAS^O17^RAS_O17|MSG-100|P|2.5|", wire);
        Assert.Contains("855332^Warfarin 5 MG Oral Tablet^RxNorm", wire);
        Assert.Contains("RXR|PO^PO^HL70162", wire);
    }

    [Fact]
    public async Task Rgv_Stage_Builds_O15_From_Declined_Event_Async()
    {
        var stage = new MedicationDeclinedToHl7RgvTransformStage(_clock);
        var json = JsonSerializer.Serialize(new
        {
            EntryId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PatientId = "MRN-42",
            MedicationCodeSystem = "RxNorm",
            MedicationCode = "310965",
            DeclinedAtUtc = new DateTime(2026, 5, 22, 9, 15, 0, DateTimeKind.Utc),
            DeclinedBySub = "nurse-lee",
            Reason = "Patient refused",
        });

        var result = await stage.TransformAsync(Message(json, "MSG-200"), CancellationToken.None);
        var wire = Encoding.UTF8.GetString(result.Payload.Span);

        Assert.Equal(PayloadFormat.Utf8Text, result.PayloadFormat);
        Assert.Contains("|RGV^O15^RGV_O15|MSG-200|P|2.5|", wire);
        Assert.Contains("ORC|DC|", wire);
        Assert.Contains("NTE|1|RE|Patient refused\r", wire);
    }

    [Fact]
    public async Task Rgv_Stage_Defaults_Reason_When_Missing_Async()
    {
        var stage = new MedicationDeclinedToHl7RgvTransformStage(_clock);
        var json = JsonSerializer.Serialize(new
        {
            PatientId = "MRN-42",
            MedicationCode = "310965",
            DeclinedAtUtc = new DateTime(2026, 5, 22, 9, 15, 0, DateTimeKind.Utc),
            DeclinedBySub = "nurse-lee",
        });

        var result = await stage.TransformAsync(Message(json), CancellationToken.None);
        var wire = Encoding.UTF8.GetString(result.Payload.Span);
        Assert.Contains("NTE|1|RE|Declined\r", wire);
    }

    [Fact]
    public async Task Ras_Stage_Passes_Through_Non_Json_Payload_Async()
    {
        var stage = new MedicationAdministeredToHl7RasTransformStage(_clock);
        var original = Message("not-json-at-all");

        var result = await stage.TransformAsync(original, CancellationToken.None);
        Assert.Equal("not-json-at-all", Encoding.UTF8.GetString(result.Payload.Span));
    }

    [Fact]
    public async Task Ras_Stage_Passes_Through_When_Medication_Code_Missing_Async()
    {
        var stage = new MedicationAdministeredToHl7RasTransformStage(_clock);
        var json = JsonSerializer.Serialize(new { PatientId = "MRN-1", DoseQuantity = 1m });

        var result = await stage.TransformAsync(Message(json), CancellationToken.None);
        // No medication code → pass through unchanged (still the input JSON).
        Assert.Contains("MRN-1", Encoding.UTF8.GetString(result.Payload.Span));
        Assert.DoesNotContain("RAS^O17", Encoding.UTF8.GetString(result.Payload.Span));
    }
}
