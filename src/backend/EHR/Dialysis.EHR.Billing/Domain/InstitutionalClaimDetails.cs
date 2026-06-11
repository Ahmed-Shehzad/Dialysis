namespace Dialysis.EHR.Billing.Domain;

/// <summary>
/// Institutional claim section carried by a <see cref="Claim"/> billed on the 837I / UB-04
/// path — how freestanding ESRD facilities commonly bill. Holds the UB-04 type of bill
/// (e.g. <c>0721</c> = ESRD freestanding clinic, original claim), the statement period,
/// the optional admission date/type, and the optional ICD-10-PCS procedure entries
/// (principal + others; system URI <c>EhrCodeSystems.Icd10Pcs</c>). Professional (837P)
/// claims never carry this section — it is null on them, so the existing flow is untouched.
/// </summary>
public sealed class InstitutionalClaimDetails
{
    private readonly List<string> _otherProcedureCodes = new();

    private InstitutionalClaimDetails()
    {
    }

    /// <summary>UB-04 type of bill — four characters including the leading zero (e.g. <c>0721</c>).</summary>
    public string TypeOfBill { get; private set; } = string.Empty;

    /// <summary>Statement-covers period start (UB-04 FL6 "from"; 837I DTP*434).</summary>
    public DateTime StatementFromUtc { get; private set; }

    /// <summary>Statement-covers period end (UB-04 FL6 "through"; 837I DTP*434).</summary>
    public DateTime StatementToUtc { get; private set; }

    /// <summary>Admission date where applicable (UB-04 FL12; 837I DTP*435); null for outpatient-only claims.</summary>
    public DateTime? AdmissionDateUtc { get; private set; }

    /// <summary>Admission type code where applicable (UB-04 FL14, e.g. <c>3</c> = elective; 837I CL1-01).</summary>
    public string? AdmissionTypeCode { get; private set; }

    /// <summary>Principal ICD-10-PCS procedure code (837I HI with <c>BBR</c> qualifier); null when no procedure is reported.</summary>
    public string? PrincipalProcedureCode { get; private set; }

    /// <summary>Other ICD-10-PCS procedure codes (837I HI with <c>BBQ</c> qualifiers).</summary>
    public IReadOnlyCollection<string> OtherProcedureCodes => _otherProcedureCodes;

    /// <summary>Validates and creates the institutional section. Throws when the type of bill or statement period is malformed.</summary>
    public static InstitutionalClaimDetails Create(
        string typeOfBill,
        DateTime statementFromUtc,
        DateTime statementToUtc,
        DateTime? admissionDateUtc = null,
        string? admissionTypeCode = null,
        string? principalProcedureCode = null,
        IReadOnlyList<string>? otherProcedureCodes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeOfBill);
        var tob = typeOfBill.Trim();
        if (tob.Length != 4 || tob[0] != '0' || !tob.All(char.IsLetterOrDigit))
            throw new ArgumentException(
                "Type of bill must be four characters including the leading zero (e.g. '0721').", nameof(typeOfBill));
        if (statementToUtc < statementFromUtc)
            throw new ArgumentException("Statement period end must not precede its start.", nameof(statementToUtc));

        var others = (otherProcedureCodes ?? [])
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(static c => c.Length > 0)
            .ToList();
        if (string.IsNullOrWhiteSpace(principalProcedureCode) && others.Count > 0)
            throw new ArgumentException(
                "Other procedure codes require a principal procedure code.", nameof(otherProcedureCodes));

        var details = new InstitutionalClaimDetails
        {
            TypeOfBill = tob,
            StatementFromUtc = statementFromUtc,
            StatementToUtc = statementToUtc,
            AdmissionDateUtc = admissionDateUtc,
            AdmissionTypeCode = string.IsNullOrWhiteSpace(admissionTypeCode) ? null : admissionTypeCode.Trim(),
            PrincipalProcedureCode = string.IsNullOrWhiteSpace(principalProcedureCode) ? null : principalProcedureCode.Trim(),
        };
        details._otherProcedureCodes.AddRange(others);
        return details;
    }
}
