namespace Dialysis.SmartConnect.Ncpdp;

/// <summary>
/// 2-character NCPDP Telecom field identifiers used by the K2 mappers. Names mirror the
/// NCPDP data dictionary's full alphabetic codes (e.g. "402-D2"); we use the trailing
/// two-character suffix because that's what appears in the wire stream.
/// </summary>
internal static class NcpdpFieldKeys
{
    // Patient segment (AM01)
    public const string CardholderId = "C2";
    public const string PatientDateOfBirth = "C4";
    public const string PatientGenderCode = "C5";
    public const string PatientFirstName = "CA";
    public const string PatientLastName = "CB";

    // Pharmacy provider segment (AM02)
    public const string ServiceProviderId = "B2";
    public const string ServiceProviderIdQualifier = "B1";

    // Claim segment (AM07)
    public const string PrescriptionReferenceNumber = "D2";
    public const string ProductServiceId = "D7"; // NDC when qualifier=03
    public const string ProductServiceIdQualifier = "E1";
    public const string QuantityDispensed = "E7";
    public const string DateOfService = "D1";

    // Pricing segment (AM11)
    public const string GrossAmountDue = "DU";

    // Transaction header (no AM)
    public const string BinNumber = "A1";
}
