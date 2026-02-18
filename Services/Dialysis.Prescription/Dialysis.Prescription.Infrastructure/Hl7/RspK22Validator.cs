using Dialysis.Prescription.Application.Abstractions;

namespace Dialysis.Prescription.Infrastructure.Hl7;

/// <summary>
/// Validates RSP^K22 parse result per IHE/HL7 prescription response rules.
/// MSA-1: AA = success, AE/AR = error. MSA-2 = request MSH-10.
/// QPD-1: MDC_HDIALY_RX_QUERY for prescription. QPD-2 = query tag.
/// </summary>
public sealed class RspK22Validator : IRspK22Validator
{
    private const string PrescriptionQueryName = "MDC_HDIALY_RX_QUERY";

    public RspK22ValidationResult Validate(RspK22ParseResult result, RspK22ValidationContext? context = null)
    {
        if (!string.IsNullOrEmpty(result.MsaAcknowledgmentCode))
        {
            string msa = result.MsaAcknowledgmentCode.ToUpperInvariant();
            if (msa is "AE" or "AR")
                return RspK22ValidationResult.MsaError(result.MsaAcknowledgmentCode, "EMR rejected the prescription query.");
        }

        if (!string.IsNullOrEmpty(result.QpdQueryName)
            && !string.Equals(result.QpdQueryName, PrescriptionQueryName, StringComparison.OrdinalIgnoreCase))
            return RspK22ValidationResult.QueryNameMismatch(PrescriptionQueryName, result.QpdQueryName);

        if (context is not null)
        {
            if (!string.IsNullOrEmpty(context.MessageControlId) && !string.IsNullOrEmpty(result.MsaControlId)
                && !string.Equals(result.MsaControlId, context.MessageControlId, StringComparison.Ordinal))
                return RspK22ValidationResult.Msa2Mismatch(context.MessageControlId, result.MsaControlId);

            if (!string.IsNullOrEmpty(context.QueryTag) && !string.IsNullOrEmpty(result.QueryTag)
                && !string.Equals(result.QueryTag, context.QueryTag, StringComparison.Ordinal))
                return RspK22ValidationResult.Qpd2Mismatch(context.QueryTag, result.QueryTag);
        }

        return RspK22ValidationResult.Success();
    }
}
