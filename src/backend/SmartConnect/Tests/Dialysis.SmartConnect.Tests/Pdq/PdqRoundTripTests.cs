using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Pdq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.SmartConnect.Tests.Pdq;

/// <summary>
/// Conformance against Dialysis Machine HL7 Implementation Guide rev 4.0 §4.3 — IHE PDQ
/// Patient Demographics Query / Response. Each test mirrors one of the worked examples
/// 4.3.1 through 4.3.5 from the IG.
/// </summary>
public sealed class PdqRoundTripTests
{
    private static readonly DateTime _fixedNow = new(2026, 5, 22, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Parser_Reads_MRN_Criterion_From_Ig_Example_4_3_1()
    {
        const string qbp =
            "MSH|^~\\&|ACME Dialysis Machine^00059AFFFE3C7A00^EUI-64||||202204120831230000||QBP^Q22^QBP_Q21|20220412083123173|P|2.6|||AL|NE|||||\r" +
            "QPD|IHE PDQ Query|20220412083123174|@PID.3^555444222111^^^^MR\r" +
            "RCP|I||R|";

        var msg = Hl7V2Message.Parse(qbp);
        var criteria = Hl7V2QbpQ22Parser.Parse(msg);

        Assert.Equal("555444222111", criteria.MedicalRecordNumber);
        Assert.Null(criteria.FamilyName);
        Assert.Null(criteria.GivenName);
        Assert.Null(criteria.PersonNumber);
        Assert.Equal("20220412083123174", criteria.QueryTag);
        Assert.Equal("20220412083123173", criteria.MessageControlId);
    }

    [Fact]
    public void Parser_Reads_Name_Criteria_From_Ig_Example_4_3_3()
    {
        const string qbp =
            "MSH|^~\\&|ACME Dialysis Machine^00059AFFFE3C7A00^EUI-64||||202204120831230000||QBP^Q22^QBP_Q21|20220412083123138|P|2.6|||AL|NE|||||\r" +
            "QPD|IHE PDQ Query|20220412083123153|@PID.5.1^Smith~@PID.5.2^John\r" +
            "RCP|I||R|";

        var criteria = Hl7V2QbpQ22Parser.Parse(Hl7V2Message.Parse(qbp));

        Assert.Equal("Smith", criteria.FamilyName);
        Assert.Equal("John", criteria.GivenName);
        Assert.Null(criteria.MedicalRecordNumber);
    }

    [Fact]
    public void Parser_Reads_Person_Number_From_Ig_Example_4_3_5()
    {
        const string qbp =
            "MSH|^~\\&|ACME Dialysis Machine^00059AFFFE3C7A00^EUI-64||||202204120831230000||QBP^Q22^QBP_Q21|20220412083123173|P|2.6|||AL|NE|||||\r" +
            "QPD|IHE PDQ Query|20220412083123174|@PID.3^010199-000H^^^^PN\r" +
            "RCP|I||R|";

        var criteria = Hl7V2QbpQ22Parser.Parse(Hl7V2Message.Parse(qbp));

        Assert.Equal("010199-000H", criteria.PersonNumber);
        Assert.Null(criteria.MedicalRecordNumber);
    }

    [Fact]
    public void Builder_Emits_Not_Found_Response_When_Empty_Per_Ig_Example_4_3_2()
    {
        var criteria = new PdqCriteria(
            QueryTag: "20220412083123174",
            MessageControlId: "20220412083123173",
            MedicalRecordNumber: "555444222111",
            PersonNumber: null,
            FamilyName: null,
            GivenName: null);

        var response = Hl7V2RspK22Builder.Build(criteria, [], "RESP-001", _fixedNow);

        Assert.Contains("MSH|^~\\&|||||", response);
        Assert.Contains("RSP^K22^RSP_K21", response);
        Assert.Contains("MSA|AA|20220412083123173", response);
        Assert.Contains("QAK|20220412083123174|NF|IHE PDQ Query|0|0|0", response);
        Assert.Contains("QPD|IHE PDQ Query|20220412083123174|@PID.3^555444222111^^^^MR", response);
        Assert.DoesNotContain("PID|", response);
    }

    [Fact]
    public void Builder_Emits_Multiple_Matches_Response_Per_Ig_Example_4_3_4()
    {
        var criteria = new PdqCriteria(
            QueryTag: "20220412083123153",
            MessageControlId: "20220412083123138",
            MedicalRecordNumber: null,
            PersonNumber: null,
            FamilyName: "Smith",
            GivenName: "John");

        var matches = new[]
        {
            new PdqMatch("555444222111", "Smith", "John", new DateOnly(1964, 3, 6), "U"),
            new PdqMatch("555444999999", "Smith", "John", new DateOnly(2000, 9, 21), "U"),
        };

        var response = Hl7V2RspK22Builder.Build(criteria, matches, "RESP-002", _fixedNow);

        Assert.Contains("MSA|AA|20220412083123138", response);
        Assert.Contains("QAK|20220412083123153|OK|IHE PDQ Query|2|2|0", response);
        Assert.Contains("QPD|IHE PDQ Query|20220412083123153|@PID.5.1^Smith~@PID.5.2^John", response);
        Assert.Contains("PID|||555444222111^^^^MR||Smith^John^^^^^U||19640306|U", response);
        Assert.Contains("PID|||555444999999^^^^MR||Smith^John^^^^^U||20000921|U", response);
    }

    [Fact]
    public async Task Responder_End_To_End_Resolves_And_Builds_Response_Async()
    {
        const string qbp =
            "MSH|^~\\&|ACME Dialysis Machine^00059AFFFE3C7A00^EUI-64||||202204120831230000||QBP^Q22^QBP_Q21|MSG-100|P|2.6|||AL|NE|||||\r" +
            "QPD|IHE PDQ Query|TAG-1|@PID.3^MRN-9001^^^^MR\r" +
            "RCP|I||R|";

        var resolver = new StubResolver(
            new PdqMatch("MRN-9001", "Doe", "Jane", new DateOnly(1972, 8, 14), "F"));
        var clock = new FakeTimeProvider(_fixedNow);
        var responder = new PdqResponder(resolver, clock);

        var response = await responder.RespondAsync(Hl7V2Message.Parse(qbp), CancellationToken.None);

        Assert.Contains("MSA|AA|MSG-100", response);
        Assert.Contains("QAK|TAG-1|OK|IHE PDQ Query|1|1|0", response);
        Assert.Contains("MRN-9001^^^^MR||Doe^Jane", response);
        Assert.Equal("MRN-9001", resolver.LastCriteria?.MedicalRecordNumber);
    }

    private sealed class StubResolver(params PdqMatch[] rows) : IPatientDemographicsResolver
    {
        public PdqCriteria? LastCriteria { get; private set; }

        public Task<IReadOnlyList<PdqMatch>> ResolveAsync(PdqCriteria criteria, CancellationToken cancellationToken = default)
        {
            LastCriteria = criteria;
            return Task.FromResult<IReadOnlyList<PdqMatch>>(rows);
        }
    }

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTime _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
