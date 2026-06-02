using Asp.Versioning;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// HTTP surface for the EHR Billing slice — read-only views the SPA needs on top of
/// the Charge / Claim / acknowledgement aggregates that EHR.Billing owns. Write
/// operations stay on the CQRS / consumer path; this controller exposes the operator
/// dashboard's query side.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing")]
public sealed class BillingController(
    IClaimRepository claims,
    IChargeRepository charges) : ControllerBase
{
    /// <summary>
    /// Lists recent charges, optionally narrowed to a single <see cref="ChargeStatus"/> and
    /// bounded by <paramref name="take"/> (1–500, default 100). Drives the
    /// <c>/admin/billing/dialysis-charges</c> SPA page — operator triages "Captured"
    /// charges, drills into the parent claim status, and watches the EDI 999 / 277CA
    /// ack timeline.
    /// </summary>
    [HttpGet("charges")]
    [ProducesResponseType(typeof(IReadOnlyList<ChargeRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChargesAsync(
        [FromQuery] string? status = null,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        ChargeStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ChargeStatus>(status, ignoreCase: true, out var s))
                return BadRequest(new { error = $"Unknown ChargeStatus '{status}'." });
            parsed = s;
        }

        var rows = await charges.ListAsync(parsed, take, cancellationToken).ConfigureAwait(false);
        var dto = rows
            .Select(c => new ChargeRow(
                ChargeId: c.Id,
                PatientId: c.PatientId,
                EncounterId: c.EncounterId,
                CptCode: c.CptCode,
                BilledAmount: c.BilledAmount.Amount,
                CurrencyCode: c.BilledAmount.CurrencyCode,
                Status: c.Status.ToString(),
                AssignedClaimId: c.AssignedClaimId,
                DiagnosisPointerIcd10Codes: c.DiagnosisPointerIcd10Codes.ToArray()))
            .ToArray();
        return Ok(dto);
    }

    /// <summary>
    /// Lists recent claims, optionally narrowed to a single <see cref="ClaimStatus"/> and
    /// bounded by <paramref name="take"/> (1–500, default 100). Newest-first by
    /// submission timestamp; drives the operator's claim-status board.
    /// </summary>
    [HttpGet("claims")]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListClaimsAsync(
        [FromQuery] string? status = null,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        ClaimStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ClaimStatus>(status, ignoreCase: true, out var s))
                return BadRequest(new { error = $"Unknown ClaimStatus '{status}'." });
            parsed = s;
        }

        var rows = await claims.ListAsync(parsed, take, cancellationToken).ConfigureAwait(false);
        var dto = rows
            .Select(c => new ClaimRow(
                ClaimId: c.Id,
                PatientId: c.PatientId,
                PayerId: c.PayerId,
                PayerCode: c.PayerCode,
                ClaimFormatCode: c.ClaimFormatCode,
                BilledTotal: c.BilledTotal.Amount,
                CurrencyCode: c.BilledTotal.CurrencyCode,
                Status: c.Status.ToString(),
                ExternalControlNumber: c.ExternalControlNumber,
                PayerClaimControlNumber: c.PayerClaimControlNumber,
                SubmittedAtUtc: c.SubmittedAtUtc,
                AcknowledgedAtUtc: c.AcknowledgedAtUtc,
                ChargeCount: c.ChargeIds.Count,
                AcknowledgementCount: c.Acknowledgements.Count))
            .ToArray();
        return Ok(dto);
    }

    /// <summary>
    /// Returns the acknowledgement history for one claim — every 999 / 277CA ack we've
    /// received against it, oldest first, with verdict + reason codes + receive
    /// timestamp. Drives the /admin/billing/dialysis-charges page's per-claim
    /// "Ack timeline" panel.
    /// </summary>
    [HttpGet("claims/{claimId:guid}/acks")]
    [ProducesResponseType(typeof(ClaimAcksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAcksAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var claim = await claims.GetAsync(claimId, cancellationToken).ConfigureAwait(false);
        if (claim is null) return NotFound();

        var rows = claim.Acknowledgements
            .OrderBy(a => a.ReceivedAtUtc)
            .Select(a => new ClaimAckRow(
                AcknowledgementId: a.Id,
                Kind: a.Kind.ToString(),
                Verdict: a.Verdict.ToString(),
                PayerClaimControlNumber: a.PayerClaimControlNumber,
                ReasonCodes: a.ReasonCodes,
                ReceivedAtUtc: a.ReceivedAtUtc))
            .ToArray();
        return Ok(new ClaimAcksResponse(
            ClaimId: claim.Id,
            Status: claim.Status.ToString(),
            ExternalControlNumber: claim.ExternalControlNumber,
            PayerClaimControlNumber: claim.PayerClaimControlNumber,
            AcknowledgedAtUtc: claim.AcknowledgedAtUtc,
            Acknowledgements: rows));
    }
}

public sealed record ChargeRow(
    Guid ChargeId,
    Guid PatientId,
    Guid EncounterId,
    string CptCode,
    decimal BilledAmount,
    string CurrencyCode,
    string Status,
    Guid? AssignedClaimId,
    IReadOnlyList<string> DiagnosisPointerIcd10Codes);

public sealed record ClaimRow(
    Guid ClaimId,
    Guid PatientId,
    Guid PayerId,
    string PayerCode,
    string ClaimFormatCode,
    decimal BilledTotal,
    string CurrencyCode,
    string Status,
    string? ExternalControlNumber,
    string? PayerClaimControlNumber,
    DateTime? SubmittedAtUtc,
    DateTime? AcknowledgedAtUtc,
    int ChargeCount,
    int AcknowledgementCount);

public sealed record ClaimAcksResponse(
    Guid ClaimId,
    string Status,
    string? ExternalControlNumber,
    string? PayerClaimControlNumber,
    DateTime? AcknowledgedAtUtc,
    IReadOnlyList<ClaimAckRow> Acknowledgements);

public sealed record ClaimAckRow(
    Guid AcknowledgementId,
    string Kind,
    string Verdict,
    string? PayerClaimControlNumber,
    IReadOnlyList<string> ReasonCodes,
    DateTime ReceivedAtUtc);
