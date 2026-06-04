namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// Parsed criteria from an IG §5.2.1 Dialysis Prescription Query (HL7 v2 <c>QBP^Q22</c>
/// with query name <c>MDC_HDIALY_RX_QUERY</c>). Only the patient MRN is exercised today;
/// the slot is open for future enhancements (per the IG, QPD-3 follows the same shape as
/// the PDQ message).
/// </summary>
public sealed record PrescriptionQuery
{
    /// <summary>
    /// Parsed criteria from an IG §5.2.1 Dialysis Prescription Query (HL7 v2 <c>QBP^Q22</c>
    /// with query name <c>MDC_HDIALY_RX_QUERY</c>). Only the patient MRN is exercised today;
    /// the slot is open for future enhancements (per the IG, QPD-3 follows the same shape as
    /// the PDQ message).
    /// </summary>
    public PrescriptionQuery(string QueryTag,
        string MessageControlId,
        string? MedicalRecordNumber)
    {
        this.QueryTag = QueryTag;
        this.MessageControlId = MessageControlId;
        this.MedicalRecordNumber = MedicalRecordNumber;
    }
    public string QueryTag { get; init; }
    public string MessageControlId { get; init; }
    public string? MedicalRecordNumber { get; init; }
    public void Deconstruct(out string QueryTag, out string MessageControlId, out string? MedicalRecordNumber)
    {
        QueryTag = this.QueryTag;
        MessageControlId = this.MessageControlId;
        MedicalRecordNumber = this.MedicalRecordNumber;
    }
}
