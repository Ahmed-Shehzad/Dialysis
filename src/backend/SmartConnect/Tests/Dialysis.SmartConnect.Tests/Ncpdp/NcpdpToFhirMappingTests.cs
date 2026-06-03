using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.DataTypes.Ncpdp;
using Dialysis.SmartConnect.Ncpdp;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit;

// Disambiguate System.Threading.Tasks.Task from Hl7.Fhir.Model.Task (FHIR resource type)
// — the FHIR namespace import pulls in a Task resource we don't use here, but it collides
// with the async return type on every [Fact] async method below.
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Tests.Ncpdp;

/// <summary>
/// Slice K2: NCPDP Telecom transactions map to FHIR R4 resources via per-transaction
/// mappers. Each test exercises one transaction-code path so a partner-driven regression
/// (e.g. NCPDP changes the meaning of D2) surfaces against exactly one mapper.
/// </summary>
public sealed class NcpdpToFhirMappingTests
{
    private const char Fs = NcpdpTelecomMessage.FieldSeparator;
    private const char Ss = NcpdpTelecomMessage.SegmentSeparator;

    private static readonly FhirJsonDeserializer _parser = new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    private static T ParseFhirJson<T>(string json) where T : Resource =>
        (T)_parser.DeserializeObject(typeof(T), json);

    [Fact]
    public void B1_Billing_Maps_To_Active_Claim_With_Ndc_And_Patient()
    {
        var parsed = ParseFixture("B1");
        var mapper = new NcpdpBillingToClaimMapper();

        var resource = mapper.Map(parsed);

        var claim = Assert.IsType<Claim>(resource);
        Assert.Equal(FinancialResourceStatusCodes.Active, claim.Status);
        Assert.Equal("Patient/PT-123", claim.Patient.Reference);
        Assert.Equal("Practitioner/1234567890", claim.Provider.Reference);
        Assert.Equal("RX-001", Assert.Single(claim.Identifier).Value);
        var item = Assert.Single(claim.Item);
        Assert.Equal("12345-6789-01", Assert.Single(item.ProductOrService!.Coding).Code);
        Assert.Equal(30m, item.Quantity!.Value);
    }

    [Fact]
    public void B2_Reversal_Maps_To_Cancelled_Claim()
    {
        var parsed = ParseFixture("B2");
        var mapper = new NcpdpReversalToClaimMapper();

        var resource = mapper.Map(parsed);

        var claim = Assert.IsType<Claim>(resource);
        Assert.Equal(FinancialResourceStatusCodes.Cancelled, claim.Status);
        Assert.Equal("RX-001", Assert.Single(claim.Identifier).Value);
    }

    [Fact]
    public void E1_Eligibility_Maps_To_Coverage_Eligibility_Request()
    {
        var parsed = ParseFixture("E1");
        var mapper = new NcpdpEligibilityToCoverageEligibilityRequestMapper();

        var resource = mapper.Map(parsed);

        var request = Assert.IsType<CoverageEligibilityRequest>(resource);
        Assert.Equal("Patient/PT-123", request.Patient.Reference);
        Assert.Equal(CoverageEligibilityRequest.EligibilityRequestPurpose.Validation, Assert.Single(request.Purpose));
        Assert.Equal("Organization/bin-610515", request.Insurer.Reference);
    }

    [Fact]
    public void N1_Info_Reporting_Maps_To_Medication_Dispense_With_Ndc()
    {
        var parsed = ParseFixture("N1");
        var mapper = new NcpdpInfoReportingToMedicationDispenseMapper();

        var resource = mapper.Map(parsed);

        var dispense = Assert.IsType<MedicationDispense>(resource);
        Assert.Equal("Patient/PT-123", dispense.Subject!.Reference);
        Assert.Equal(MedicationDispense.MedicationDispenseStatusCodes.Completed, dispense.Status);
        var ndc = Assert.IsType<CodeableConcept>(dispense.Medication);
        Assert.Equal("12345-6789-01", Assert.Single(ndc.Coding).Code);
    }

    [Fact]
    public async Task Dispatch_Stage_Routes_To_Mapper_By_Transaction_Code_Async()
    {
        var stage = new NcpdpToFhirTransformStage([
            new NcpdpBillingToClaimMapper(),
            new NcpdpReversalToClaimMapper(),
            new NcpdpEligibilityToCoverageEligibilityRequestMapper(),
            new NcpdpInfoReportingToMedicationDispenseMapper(),
        ]);
        var message = Build_Message(BuildFixturePayload("B1"));

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        var json = Encoding.UTF8.GetString(transformed.Payload.Span);
        var resource = ParseFhirJson<Claim>(json);
        Assert.Equal(FinancialResourceStatusCodes.Active, resource.Status);
        Assert.Equal("Patient/PT-123", resource.Patient.Reference);
    }

    [Fact]
    public async Task Dispatch_Stage_Passes_Through_Unknown_Transaction_Code_Async()
    {
        // B3 (Rebill) has no mapper registered — payload passes through unchanged.
        var stage = new NcpdpToFhirTransformStage([new NcpdpBillingToClaimMapper()]);
        var message = Build_Message(BuildFixturePayload("B3"));
        var originalText = Encoding.UTF8.GetString(message.Payload.Span);

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal(originalText, Encoding.UTF8.GetString(transformed.Payload.Span));
    }

    [Fact]
    public async Task Dispatch_Stage_Passes_Through_Non_Telecom_Payload_Async()
    {
        var stage = new NcpdpToFhirTransformStage([new NcpdpBillingToClaimMapper()]);
        var message = Build_Message("not an NCPDP message");

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal("not an NCPDP message", Encoding.UTF8.GetString(transformed.Payload.Span));
    }

    private static NcpdpTelecomMessage ParseFixture(string transactionCode)
    {
        var payload = BuildFixturePayload(transactionCode);
        var parsed = NcpdpTelecomMessage.TryParse(payload);
        Assert.NotNull(parsed);
        return parsed!;
    }

    private static string BuildFixturePayload(string transactionCode)
    {
        // Header (A1 BIN, A2 Version, A3 Transaction Code) + AM01 patient (C2 cardholder)
        // + AM02 pharmacy provider (B2 NPI) + AM07 claim (D2 Rx ref, D7 NDC, E7 qty, D1 date).
        var header = $"A1610515{Fs}A2D0{Fs}A3{transactionCode}";
        var patient = $"AM01{Fs}C2PT-123{Fs}C4{19850101}{Fs}C51";
        var provider = $"AM02{Fs}B2{1234567890}";
        var claim = $"AM07{Fs}D2RX-001{Fs}D712345-6789-01{Fs}E730{Fs}D120260524";
        return string.Join(Ss, header, patient, provider, claim) + Ss;
    }

    private static IntegrationMessage Build_Message(string payload) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = "C",
        Payload = Encoding.UTF8.GetBytes(payload),
        PayloadFormat = PayloadFormat.Utf8Text,
        Metadata = ImmutableDictionary<string, string>.Empty,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };
}
