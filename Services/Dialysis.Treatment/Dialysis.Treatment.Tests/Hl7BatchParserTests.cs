using Dialysis.Treatment.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Treatment.Tests;

public class Hl7BatchParserTests
{
    private readonly Hl7BatchParser _parser = new();

    [Fact]
    public void ExtractMessages_EmptyBatch_ReturnsEmpty()
    {
        IReadOnlyList<string> result = _parser.ExtractMessages("FHS|^~\\&||||||\rBHS|^~\\&||||||\rBTS|1\rFTS|1\r");
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void ExtractMessages_SingleOru_ReturnsOneMessage()
    {
        string oru = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01|M001|P|2.5\r"
                     + "PID|||MRN123^^^^MR\r"
                     + "OBR|1||S001||||||||||||\r"
                     + "OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM\r";
        string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{oru}\rBTS|1\rFTS|1\r";

        IReadOnlyList<string> result = _parser.ExtractMessages(batch);

        result.Count.ShouldBe(1);
        result[0].ShouldStartWith("MSH|");
        result[0].ShouldContain("OBX");
    }

    [Fact]
    public void ExtractMessages_TwoOruMessages_ReturnsBoth()
    {
        string oru1 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01|M001|P|2.5\rPID|||MRN1\rOBR|1||S001\rOBX|1|NM|X^Y^MDC|1.1.1|100\r";
        string oru2 = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120100||ORU^R01|M002|P|2.5\rPID|||MRN2\rOBR|1||S002\rOBX|1|NM|X^Y^MDC|1.1.1|200\r";
        string batch = $"FHS|^~\\&||||||\rBHS|^~\\&||||||\r{oru1}\r{oru2}\rBTS|2\rFTS|1\r";

        IReadOnlyList<string> result = _parser.ExtractMessages(batch);

        result.Count.ShouldBe(2);
        result[0].ShouldContain("MRN1");
        result[1].ShouldContain("MRN2");
    }

    [Fact]
    public void ExtractMessages_WithLfOnly_NormalizesAndExtracts()
    {
        string oru = "MSH|^~\\&|DEV|FAC|PDMS|FAC|20230215120000||ORU^R01|M001|P|2.5\n"
                     + "PID|||MRN123^^^^MR\n"
                     + "OBR|1||S001||||||||||||\n"
                     + "OBX|1|NM|152348^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE^MDC|1.1.3.1|300|ml/min^ml/min^UCUM";
        string batch = $"FHS|^~\\&||||||\nBHS|^~\\&||||||\n{oru}\nBTS|1\nFTS|1\n";

        IReadOnlyList<string> result = _parser.ExtractMessages(batch);

        result.Count.ShouldBe(1);
        result[0].ShouldStartWith("MSH|");
        result[0].ShouldContain("OBX");
    }

    [Fact]
    public void ExtractMessages_ThrowsOnNullOrWhitespace()
    {
        _ = Should.Throw<ArgumentException>(() => _parser.ExtractMessages(null!));
        _ = Should.Throw<ArgumentException>(() => _parser.ExtractMessages(""));
        _ = Should.Throw<ArgumentException>(() => _parser.ExtractMessages("   "));
    }
}
