namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// Parsed criteria from an IG §5.2.1 Dialysis Prescription Query (HL7 v2 <c>QBP^Q22</c>
/// with query name <c>MDC_HDIALY_RX_QUERY</c>). Only the patient MRN is exercised today;
/// the slot is open for future enhancements (per the IG, QPD-3 follows the same shape as
/// the PDQ message).
/// </summary>
public sealed record PrescriptionQuery(
    string QueryTag,
    string MessageControlId,
    string? MedicalRecordNumber);
