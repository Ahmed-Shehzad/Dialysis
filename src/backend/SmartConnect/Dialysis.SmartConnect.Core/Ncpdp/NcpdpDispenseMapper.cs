using System.Globalization;
using Dialysis.SmartConnect.DataTypes.Ncpdp;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Ncpdp;

/// <summary>
/// Slice K2: maps an NCPDP Telecom <c>N1</c> (Information Reporting) transaction onto a
/// FHIR R4 <see cref="MedicationDispense"/>. The pharmacy is reporting that a script was
/// filled and handed over — this is the dispense-event view of the same data B1 carries
/// as a billing record.
/// </summary>
public sealed class NcpdpInfoReportingToMedicationDispenseMapper : INcpdpToFhirMapper
{
    public string TransactionCode => "N1";

    public Resource? Map(NcpdpTelecomMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var patientSegment = NcpdpBillingToClaimMapper.FindSegment(message, "AM01");
        var providerSegment = NcpdpBillingToClaimMapper.FindSegment(message, "AM02");
        var claimSegment = NcpdpBillingToClaimMapper.FindSegment(message, "AM07");

        var cardholderId = patientSegment?.GetField(NcpdpFieldKeys.CardholderId);
        var serviceProviderId = providerSegment?.GetField(NcpdpFieldKeys.ServiceProviderId);
        var ndc = claimSegment?.GetField(NcpdpFieldKeys.ProductServiceId);
        var qtyRaw = claimSegment?.GetField(NcpdpFieldKeys.QuantityDispensed);
        var dateRaw = claimSegment?.GetField(NcpdpFieldKeys.DateOfService);

        var dispense = new MedicationDispense
        {
            Status = MedicationDispense.MedicationDispenseStatusCodes.Completed,
        };

        if (!string.IsNullOrWhiteSpace(cardholderId))
        {
            dispense.Subject = new ResourceReference($"Patient/{cardholderId}");
        }

        if (!string.IsNullOrWhiteSpace(ndc))
        {
            dispense.Medication = new CodeableConcept(
                system: "http://hl7.org/fhir/sid/ndc",
                code: ndc);
        }

        if (!string.IsNullOrWhiteSpace(qtyRaw) &&
            decimal.TryParse(qtyRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
        {
            dispense.Quantity = new Quantity { Value = qty };
        }

        if (!string.IsNullOrWhiteSpace(dateRaw) &&
            DateTime.TryParseExact(
                dateRaw,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dispenseDate))
        {
            dispense.WhenHandedOverElement = new FhirDateTime(
                new DateTimeOffset(dispenseDate, TimeSpan.Zero));
        }

        if (!string.IsNullOrWhiteSpace(serviceProviderId))
        {
            dispense.Performer.Add(new MedicationDispense.PerformerComponent
            {
                Actor = new ResourceReference($"Practitioner/{serviceProviderId}"),
            });
        }

        return dispense;
    }
}
