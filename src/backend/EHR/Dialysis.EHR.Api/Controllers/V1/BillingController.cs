using Asp.Versioning;
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
public sealed class BillingController(IClaimRepository claims) : ControllerBase
{
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
