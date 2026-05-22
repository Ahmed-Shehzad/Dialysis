using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Prescription;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Tests.Prescription;

/// <summary>
/// Conformance against Dialysis Machine HL7 Implementation Guide rev 4.0 §5 — Dialysis
/// Prescription Query / Response. Tests round-trip the IG §5.4 sample messages.
/// </summary>
public sealed class PrescriptionRoundTripTests
{
    private static readonly DateTime _fixedNow = new(2026, 5, 22, 15, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Discriminator_Identifies_Prescription_Query_By_Qpd_1()
    {
        const string rxQuery =
            "MSH|^~\\&|||||20220330125317||QBP^Q22^QBP_Q21|PQ20211216144700|P|2.6\r" +
            "QPD|0^MDC_HDIALY_RX_QUERY^MDC|Q001|@PID.3^555444222111^^^^MR\r" +
            "RCP|I||R|";
        const string pdqQuery =
            "MSH|^~\\&|||||20220330125317||QBP^Q22^QBP_Q21|MSG-1|P|2.6\r" +
            "QPD|IHE PDQ Query|TAG-1|@PID.3^555444222111^^^^MR\r" +
            "RCP|I||R|";

        Assert.True(Hl7V2RxQueryParser.IsPrescriptionQuery(Hl7V2Message.Parse(rxQuery)));
        Assert.False(Hl7V2RxQueryParser.IsPrescriptionQuery(Hl7V2Message.Parse(pdqQuery)));
    }

    [Fact]
    public void Parser_Reads_Mrn_From_Ig_Example_5_4_1()
    {
        const string raw =
            "MSH|^~\\&|||||20220330125317||QBP^Q22^QBP_Q21|PQ20211216144700|P|2.6\r" +
            "QPD|0^MDC_HDIALY_RX_QUERY^MDC|Q001|@PID.3^555444222111^^^^MR\r" +
            "RCP|I||R|";

        var query = Hl7V2RxQueryParser.Parse(Hl7V2Message.Parse(raw));

        Assert.Equal("555444222111", query.MedicalRecordNumber);
        Assert.Equal("Q001", query.QueryTag);
        Assert.Equal("PQ20211216144700", query.MessageControlId);
    }

    [Fact]
    public void Builder_Emits_Not_Found_Per_Ig_Example_5_4_4()
    {
        var query = new PrescriptionQuery("Q001", "PQ20211216144700", "555444222111");

        var response = Hl7V2RxResponseBuilder.Build(query, document: null, "RESP-NF-001", _fixedNow);

        Assert.Contains("RSP^K22^RSP_K21", response);
        Assert.Contains("MSA|AA|PQ20211216144700", response);
        Assert.Contains("QAK|Q001|NF|0^MDC_HDIALY_RX_QUERY^MDC|0|0|0", response);
        Assert.Contains("QPD|0^MDC_HDIALY_RX_QUERY^MDC|Q001|@PID.3^555444222111^^^^MR", response);
        Assert.DoesNotContain("OBX|", response);
    }

    [Fact]
    public void Builder_Emits_Hd_Prescription_Per_Ig_Example_5_4_2()
    {
        var query = new PrescriptionQuery("Q001", "PQ20211216144700", "555444222111");
        var document = new PrescriptionDocument(
            MedicalRecordNumber: "555444222111",
            OrderNumber: "A226677",
            OrderingProviderId: "444-44-4444",
            OrderingProviderFamily: "HIPPOCRATES",
            OrderingProviderGiven: "HAROLD",
            Modality: TherapyModality.Hd,
            TherapyCompletionMethod: "UF",
            BloodPump: new BloodPumpSettings(BloodFlowRateMlPerMin: 250, PumpMode: "2N"),
            Dialysate: new DialysateFluidSettings(
                FlowMode: "CONST",
                FlowRateMlPerMin: 120,
                VolumeLiters: 25,
                DialysateName: "RFP-204"),
            Ultrafiltration: new UltrafiltrationSettings(
                UfMode: "CONST-WT",
                UfRateMlPerHour: 400,
                TargetVolumeToRemoveMl: 1000));

        var response = Hl7V2RxResponseBuilder.Build(query, document, "RESP-001", _fixedNow);

        // QAK reports OK + 1 hit.
        Assert.Contains("QAK|Q001|OK|0^MDC_HDIALY_RX_QUERY^MDC|1|1|0", response);
        // OBC carries the order number + ordering provider.
        Assert.Contains("OBC|NW|A226677^PC||||N||||444-44-4444^HIPPOCRATES^HAROLD^^^^MD", response);
        // OBX hierarchy: top-of-tree machine MDS through UF channel target.
        Assert.Contains("70929^MDC_DEV_HDIALY_MACHINE_MDS^MDC|1|", response);
        Assert.Contains("158598^MDC_HDIALY_MACH_TX_MODALITY^MDC|1.1.1.1|HD|", response);
        Assert.Contains("158618^MDC_HDIALY_THERAPY_COMPLETE_METHOD^MDC|1.1.2.1|UF|", response);
        Assert.Contains("16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC|1.1.3.1|250|ml/min^ml/min^UCUM|", response);
        Assert.Contains("158606^MDC_HDIALY_DIALYSATE_FLOW_MODE^MDC|1.1.4.1|CONST|", response);
        Assert.Contains("16936008^MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING^MDC|1.1.4.2|120|ml/min^ml/min^UCUM|", response);
        Assert.Contains("158608^MDC_HDIALY_DIALYSATE_NAME^MDC|1.1.4.4|RFP-204|", response);
        Assert.Contains("158619^MDC_HDIALY_UF_MODE^MDC|1.1.5.1|CONST-WT|", response);
        Assert.Contains("16936252^MDC_HDIALY_UF_RATE_SETTING^MDC|1.1.5.2|400|ml/h^ml/h^UCUM|", response);
        Assert.Contains("159028^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC|1.1.5.3|1000|ml^ml^UCUM|", response);
    }

    [Fact]
    public async Task Responder_End_To_End_Resolves_And_Builds_Async()
    {
        const string raw =
            "MSH|^~\\&|||||20220330125317||QBP^Q22^QBP_Q21|MSG-200|P|2.6\r" +
            "QPD|0^MDC_HDIALY_RX_QUERY^MDC|TAG-2|@PID.3^MRN-9001^^^^MR\r" +
            "RCP|I||R|";
        var resolver = new StubResolver(new PrescriptionDocument(
            MedicalRecordNumber: "MRN-9001",
            OrderNumber: "ORD-1",
            OrderingProviderId: null,
            OrderingProviderFamily: null,
            OrderingProviderGiven: null,
            Modality: TherapyModality.Hd,
            TherapyCompletionMethod: "UF",
            BloodPump: new BloodPumpSettings(300, "2N"),
            Dialysate: new DialysateFluidSettings("CONST", 500, 30, "RFP-200"),
            Ultrafiltration: new UltrafiltrationSettings("CONST-WT", 350, 1500)));
        var clock = new FakeTimeProvider(_fixedNow);
        var responder = new PrescriptionResponder(resolver, clock);

        var response = await responder.RespondAsync(Hl7V2Message.Parse(raw), CancellationToken.None);

        Assert.Contains("MSA|AA|MSG-200", response);
        Assert.Contains("QAK|TAG-2|OK|0^MDC_HDIALY_RX_QUERY^MDC|1|1|0", response);
        Assert.Contains("16935956^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC|1.1.3.1|300|", response);
        Assert.Equal("MRN-9001", resolver.LastQuery?.MedicalRecordNumber);
    }

    private sealed class StubResolver(PrescriptionDocument? document) : IPrescriptionResolver
    {
        public PrescriptionQuery? LastQuery { get; private set; }

        public Task<PrescriptionDocument?> ResolveAsync(PrescriptionQuery query, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult(document);
        }
    }

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTime _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
