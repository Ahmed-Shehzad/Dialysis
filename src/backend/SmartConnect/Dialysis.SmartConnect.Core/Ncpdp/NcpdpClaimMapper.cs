using System.Globalization;
using Dialysis.SmartConnect.DataTypes.Ncpdp;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Ncpdp;

/// <summary>
/// Slice K2: maps an NCPDP Telecom <c>B1</c> (Billing Request) transaction onto a FHIR
/// R4 <see cref="Claim"/>. The reversal mapper (<see cref="NcpdpReversalToClaimMapper"/>)
/// inherits via <c>status = cancelled</c>; everything else round-trips through this
/// type so the field mapping lives in one place.
/// </summary>
public sealed class NcpdpBillingToClaimMapper : INcpdpToFhirMapper
{
    public string TransactionCode => "B1";

    public Resource? Map(NcpdpTelecomMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return MapInternal(message, FinancialResourceStatusCodes.Active);
    }

    /// <summary>Shared field-projection helper used by the B2 reversal mapper.</summary>
    internal static Claim MapInternal(
        NcpdpTelecomMessage message,
        FinancialResourceStatusCodes status)
    {
        var patientSegment = FindSegment(message, "AM01");
        var providerSegment = FindSegment(message, "AM02");
        var claimSegment = FindSegment(message, "AM07");

        var cardholderId = patientSegment?.GetField(NcpdpFieldKeys.CardholderId);
        var serviceProviderId = providerSegment?.GetField(NcpdpFieldKeys.ServiceProviderId);
        var prescriptionReference = claimSegment?.GetField(NcpdpFieldKeys.PrescriptionReferenceNumber);
        var ndc = claimSegment?.GetField(NcpdpFieldKeys.ProductServiceId);
        var qtyRaw = claimSegment?.GetField(NcpdpFieldKeys.QuantityDispensed);
        var dateRaw = claimSegment?.GetField(NcpdpFieldKeys.DateOfService);

        var claim = new Claim
        {
            Status = status,
            Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/claim-type", "pharmacy"),
            Use = ClaimUseCode.Claim,
            Created = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(prescriptionReference))
        {
            claim.Identifier.Add(new Identifier(
                system: "urn:oid:2.16.840.1.113883.3.7305.4", // NCPDP prescription reference
                value: prescriptionReference));
        }

        if (!string.IsNullOrWhiteSpace(cardholderId))
        {
            claim.Patient = new ResourceReference($"Patient/{cardholderId}");
        }

        if (!string.IsNullOrWhiteSpace(serviceProviderId))
        {
            claim.Provider = new ResourceReference($"Practitioner/{serviceProviderId}");
        }

        if (!string.IsNullOrWhiteSpace(ndc))
        {
            var item = new Claim.ItemComponent
            {
                Sequence = 1,
                ProductOrService = new CodeableConcept("http://hl7.org/fhir/sid/ndc", ndc),
            };

            if (!string.IsNullOrWhiteSpace(qtyRaw) &&
                decimal.TryParse(qtyRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
            {
                item.Quantity = new Quantity { Value = qty };
            }

            if (TryParseNcpdpDate(dateRaw, out var serviced))
            {
                item.Serviced = new Date(serviced.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            claim.Item.Add(item);
        }

        return claim;
    }

    internal static NcpdpSegment? FindSegment(NcpdpTelecomMessage message, string segmentId) =>
        message.Segments.FirstOrDefault(s => string.Equals(s.SegmentId, segmentId, StringComparison.OrdinalIgnoreCase));

    private static bool TryParseNcpdpDate(string? raw, out DateTime value)
    {
        value = default;
        return !string.IsNullOrWhiteSpace(raw)
            && DateTime.TryParseExact(
                raw,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value);
    }
}

/// <summary>Slice K2: NCPDP <c>B2</c> Reversal — same projection as B1 but the resulting
/// Claim is emitted with <c>status = cancelled</c> so downstream EHR systems treat it as
/// a void of the original billing record.</summary>
public sealed class NcpdpReversalToClaimMapper : INcpdpToFhirMapper
{
    public string TransactionCode => "B2";

    public Resource? Map(NcpdpTelecomMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return NcpdpBillingToClaimMapper.MapInternal(message, FinancialResourceStatusCodes.Cancelled);
    }
}
