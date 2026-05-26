using System.Globalization;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 DFT^P03 (post detail financial transaction) FT1 segment to a FHIR R4
/// <c>ChargeItem</c>. PID-3 carries the patient identifier; FT1-3 is the transaction id; FT1-4 the
/// transaction date; FT1-7 the procedure code; FT1-10 the quantity. Multiple FT1 segments per
/// message all roll up to a single ChargeItem here — flows that need per-FT1 ChargeItems can wrap
/// this mapper in an iterator transform.
/// </summary>
public sealed class DftP03ToChargeItemMapper : IFhirV2MessageMapper<ChargeItem>
{
    public string TriggerEvent => "DFT^P03";

    public ChargeItem Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var item = new ChargeItem
        {
            Status = ChargeItem.ChargeItemStatus.Billable,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding(
                        system: "http://www.ama-assn.org/go/cpt",
                        code: message.GetValue("FT1.7.1") ?? "unknown",
                        display: message.GetValue("FT1.7.2")),
                ],
            },
        };

        var txnId = message.GetValue("FT1.3");
        if (!string.IsNullOrEmpty(txnId))
        {
            item.Identifier.Add(new Identifier
            {
                System = "urn:ietf:rfc:3986",
                Value = txnId,
            });
        }

        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            item.Subject = new ResourceReference($"Patient/{mrn}");
        }

        var txnDate = message.GetValue("FT1.4");
        if (!string.IsNullOrEmpty(txnDate) && txnDate.Length >= 8)
        {
            item.Occurrence = new FhirDateTime(
                $"{txnDate[..4]}-{txnDate.Substring(4, 2)}-{txnDate.Substring(6, 2)}");
        }

        var quantityRaw = message.GetValue("FT1.10");
        if (!string.IsNullOrEmpty(quantityRaw)
            && decimal.TryParse(quantityRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
        {
            item.Quantity = new Quantity { Value = qty };
        }

        // FT1-22 (total cost) → ChargeItem.priceOverride when present. Optional; many DFT senders
        // emit only the unit price in FT1-12 and the platform downstream consumer aggregates.
        var totalRaw = message.GetValue("FT1.22");
        if (!string.IsNullOrEmpty(totalRaw)
            && decimal.TryParse(totalRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var total))
        {
            item.PriceOverride = new Money { Value = total, Currency = Money.Currencies.USD };
        }

        return item;
    }
}
