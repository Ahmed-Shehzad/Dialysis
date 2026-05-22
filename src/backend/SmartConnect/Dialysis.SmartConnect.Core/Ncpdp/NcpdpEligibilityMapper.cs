using System.Globalization;
using Dialysis.SmartConnect.DataTypes.Ncpdp;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Ncpdp;

/// <summary>
/// Slice K2: maps an NCPDP Telecom <c>E1</c> (Eligibility Verification) transaction onto
/// a FHIR R4 <see cref="CoverageEligibilityRequest"/>. The pharmacy is asking the PBM
/// whether the patient's plan covers a particular medication.
/// </summary>
public sealed class NcpdpEligibilityToCoverageEligibilityRequestMapper : INcpdpToFhirMapper
{
    public string TransactionCode => "E1";

    public Resource? Map(NcpdpTelecomMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var patientSegment = NcpdpBillingToClaimMapper.FindSegment(message, "AM01");
        var cardholderId = patientSegment?.GetField(NcpdpFieldKeys.CardholderId);
        var binNumber = message.Segments.Count > 0
            ? message.Segments[0].GetField(NcpdpFieldKeys.BinNumber)
            : null;

        var request = new CoverageEligibilityRequest
        {
            Status = FinancialResourceStatusCodes.Active,
            Purpose = [CoverageEligibilityRequest.EligibilityRequestPurpose.Validation],
            Created = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(cardholderId))
        {
            request.Patient = new ResourceReference($"Patient/{cardholderId}");
        }

        if (!string.IsNullOrWhiteSpace(binNumber))
        {
            // The BIN identifies the PBM (Insurer); we surface it as an Organization
            // reference using a urn:bin URN so the downstream EHR can resolve the
            // payer record without inventing FHIR-canonical URLs.
            request.Insurer = new ResourceReference($"Organization/bin-{binNumber}");
        }

        return request;
    }
}
