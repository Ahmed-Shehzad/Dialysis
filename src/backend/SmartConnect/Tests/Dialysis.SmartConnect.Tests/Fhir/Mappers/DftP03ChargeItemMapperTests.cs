using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Hl7.Fhir.Model;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Fhir.Mappers;

/// <summary>
/// Covers the new DFT^P03 → FHIR R4 <see cref="ChargeItem"/> mapper introduced by the
/// HL7 v2 tutorial follow-ups.
/// </summary>
public sealed class DftP03ChargeItemMapperTests
{
    // HL7 v2.5 FT1 field map: 1=SetID, 3=TxnId, 4=TxnDate, 7=TxnCode, 10=Quantity, 22=TotalAmount.
    // Empty positions 8/9 + 11..21 are filled with empty pipes so the parser indexes line up.
    private const string DftSample =
        "MSH|^~\\&|BILL|HOSPITAL|FIN|CLINIC|20260526123000||DFT^P03|MSG-P03-1|P|2.5\r" +
        "EVN|P03|20260526123000\r" +
        "PID|1||MRN-12345\r" +
        "PV1|1|O|CLN^101\r" +
        "FT1|1||TXN-001|20260526|20260526|CG|99213^Office visit^CPT|||2||||||||||||175.00";

    [Fact]
    public void Mapper_Advertises_Dft_P03_Trigger()
    {
        var mapper = new DftP03ToChargeItemMapper();
        Assert.Equal("DFT^P03", mapper.TriggerEvent);
    }

    [Fact]
    public void Pipeline_Maps_Dft_To_Charge_Item()
    {
        var pipeline = new Hl7V2ToFhirPipeline(new IFhirV2MessageMapperWrapper[]
        {
            new MapperWrapper<ChargeItem>(new DftP03ToChargeItemMapper()),
        });

        var produced = pipeline.Transform(Hl7V2Message.Parse(DftSample));
        var item = Assert.IsType<ChargeItem>(Assert.Single(produced));

        Assert.Equal(ChargeItem.ChargeItemStatus.Billable, item.Status);
        Assert.Equal("99213", Assert.Single(item.Code.Coding).Code);
        Assert.Equal("Patient/MRN-12345", item.Subject?.Reference);
        Assert.Equal("TXN-001", Assert.Single(item.Identifier).Value);

        var quantity = Assert.IsType<Quantity>(item.Quantity);
        Assert.Equal(2m, quantity.Value);

        Assert.Equal(175m, item.PriceOverride?.Value);
        Assert.Equal(Money.Currencies.USD, item.PriceOverride?.Currency);
    }

    private sealed class MapperWrapper<TResource>(IFhirV2MessageMapper<TResource> inner) : IFhirV2MessageMapperWrapper
        where TResource : Resource
    {
        public string TriggerEvent => inner.TriggerEvent;

        public Resource Map(Hl7V2Message message) => inner.Map(message);
    }
}
