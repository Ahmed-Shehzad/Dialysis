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
public sealed class BillingController : ControllerBase
{
    private readonly IClaimRepository _claims;
    private readonly IChargeRepository _charges;
    private readonly IBillableEncounterRepository _billableEncounters;
    /// <summary>
    /// HTTP surface for the EHR Billing slice — read-only views the SPA needs on top of
    /// the Charge / Claim / acknowledgement aggregates that EHR.Billing owns. Write
    /// operations stay on the CQRS / consumer path; this controller exposes the operator
    /// dashboard's query side.
    /// </summary>
    public BillingController(IClaimRepository claims,
        IChargeRepository charges,
        IBillableEncounterRepository billableEncounters)
    {
        _claims = claims;
        _charges = charges;
        _billableEncounters = billableEncounters;
    }
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

        var rows = await _charges.ListAsync(parsed, take, cancellationToken).ConfigureAwait(false);
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
                DiagnosisPointerIcd10Codes: [.. c.DiagnosisPointerIcd10Codes]))
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

        var rows = await _claims.ListAsync(parsed, take, cancellationToken).ConfigureAwait(false);
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
        var claim = await _claims.GetAsync(claimId, cancellationToken).ConfigureAwait(false);
        if (claim is null)
            return NotFound();

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

    /// <summary>
    /// Lost-charge worklist: clinical encounters closed more than <paramref name="olderThanDays"/> days
    /// ago that still have no captured charge.
    /// </summary>
    [HttpGet("worklist/lost-charges")]
    [ProducesResponseType(typeof(IReadOnlyList<LostChargeRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLostChargesAsync(
        [FromQuery] int olderThanDays = 2,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(olderThanDays));
        var rows = await _billableEncounters.ListMissingChargesAsync(cutoff, take, cancellationToken).ConfigureAwait(false);
        var dto = rows
            .Select(e => new LostChargeRow(e.EncounterId, e.PatientId, e.ProviderId, e.ClosedAtUtc))
            .ToArray();
        return Ok(dto);
    }

    /// <summary>
    /// Charge-lag worklist: <c>Captured</c> charges created more than <paramref name="olderThanDays"/>
    /// days ago that haven't been assembled onto a claim yet (late-filing risk).
    /// </summary>
    [HttpGet("worklist/charge-lag")]
    [ProducesResponseType(typeof(IReadOnlyList<ChargeRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChargeLagAsync(
        [FromQuery] int olderThanDays = 7,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(olderThanDays));
        var rows = await _charges.ListAgedCapturedAsync(cutoff, take, cancellationToken).ConfigureAwait(false);
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
                DiagnosisPointerIcd10Codes: [.. c.DiagnosisPointerIcd10Codes]))
            .ToArray();
        return Ok(dto);
    }

    /// <summary>Denials worklist: claims a payer rejected (999 / 277CA verdict), for appeal/resubmit.</summary>
    [HttpGet("worklist/denials")]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDenialsAsync(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var rows = await _claims.ListAsync(ClaimStatus.Denied, take, cancellationToken).ConfigureAwait(false);
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
}

/// <summary>A closed encounter with no captured charge — a lost-charge worklist row.</summary>
public sealed record LostChargeRow(Guid EncounterId, Guid PatientId, Guid ProviderId, DateTime ClosedAtUtc);

public sealed record ChargeRow
{
    public ChargeRow(Guid ChargeId,
        Guid PatientId,
        Guid EncounterId,
        string CptCode,
        decimal BilledAmount,
        string CurrencyCode,
        string Status,
        Guid? AssignedClaimId,
        IReadOnlyList<string> DiagnosisPointerIcd10Codes)
    {
        this.ChargeId = ChargeId;
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.CptCode = CptCode;
        this.BilledAmount = BilledAmount;
        this.CurrencyCode = CurrencyCode;
        this.Status = Status;
        this.AssignedClaimId = AssignedClaimId;
        this.DiagnosisPointerIcd10Codes = DiagnosisPointerIcd10Codes;
    }
    public Guid ChargeId { get; init; }
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public string CptCode { get; init; }
    public decimal BilledAmount { get; init; }
    public string CurrencyCode { get; init; }
    public string Status { get; init; }
    public Guid? AssignedClaimId { get; init; }
    public IReadOnlyList<string> DiagnosisPointerIcd10Codes { get; init; }
    public void Deconstruct(out Guid ChargeId, out Guid PatientId, out Guid EncounterId, out string CptCode, out decimal BilledAmount, out string CurrencyCode, out string Status, out Guid? AssignedClaimId, out IReadOnlyList<string> DiagnosisPointerIcd10Codes)
    {
        ChargeId = this.ChargeId;
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        CptCode = this.CptCode;
        BilledAmount = this.BilledAmount;
        CurrencyCode = this.CurrencyCode;
        Status = this.Status;
        AssignedClaimId = this.AssignedClaimId;
        DiagnosisPointerIcd10Codes = this.DiagnosisPointerIcd10Codes;
    }
}

public sealed record ClaimRow
{
    public ClaimRow(Guid ClaimId,
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
        int AcknowledgementCount)
    {
        this.ClaimId = ClaimId;
        this.PatientId = PatientId;
        this.PayerId = PayerId;
        this.PayerCode = PayerCode;
        this.ClaimFormatCode = ClaimFormatCode;
        this.BilledTotal = BilledTotal;
        this.CurrencyCode = CurrencyCode;
        this.Status = Status;
        this.ExternalControlNumber = ExternalControlNumber;
        this.PayerClaimControlNumber = PayerClaimControlNumber;
        this.SubmittedAtUtc = SubmittedAtUtc;
        this.AcknowledgedAtUtc = AcknowledgedAtUtc;
        this.ChargeCount = ChargeCount;
        this.AcknowledgementCount = AcknowledgementCount;
    }
    public Guid ClaimId { get; init; }
    public Guid PatientId { get; init; }
    public Guid PayerId { get; init; }
    public string PayerCode { get; init; }
    public string ClaimFormatCode { get; init; }
    public decimal BilledTotal { get; init; }
    public string CurrencyCode { get; init; }
    public string Status { get; init; }
    public string? ExternalControlNumber { get; init; }
    public string? PayerClaimControlNumber { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
    public DateTime? AcknowledgedAtUtc { get; init; }
    public int ChargeCount { get; init; }
    public int AcknowledgementCount { get; init; }
    public void Deconstruct(out Guid ClaimId, out Guid PatientId, out Guid PayerId, out string PayerCode, out string ClaimFormatCode, out decimal BilledTotal, out string CurrencyCode, out string Status, out string? ExternalControlNumber, out string? PayerClaimControlNumber, out DateTime? SubmittedAtUtc, out DateTime? AcknowledgedAtUtc, out int ChargeCount, out int AcknowledgementCount)
    {
        ClaimId = this.ClaimId;
        PatientId = this.PatientId;
        PayerId = this.PayerId;
        PayerCode = this.PayerCode;
        ClaimFormatCode = this.ClaimFormatCode;
        BilledTotal = this.BilledTotal;
        CurrencyCode = this.CurrencyCode;
        Status = this.Status;
        ExternalControlNumber = this.ExternalControlNumber;
        PayerClaimControlNumber = this.PayerClaimControlNumber;
        SubmittedAtUtc = this.SubmittedAtUtc;
        AcknowledgedAtUtc = this.AcknowledgedAtUtc;
        ChargeCount = this.ChargeCount;
        AcknowledgementCount = this.AcknowledgementCount;
    }
}

public sealed record ClaimAcksResponse
{
    public ClaimAcksResponse(Guid ClaimId,
        string Status,
        string? ExternalControlNumber,
        string? PayerClaimControlNumber,
        DateTime? AcknowledgedAtUtc,
        IReadOnlyList<ClaimAckRow> Acknowledgements)
    {
        this.ClaimId = ClaimId;
        this.Status = Status;
        this.ExternalControlNumber = ExternalControlNumber;
        this.PayerClaimControlNumber = PayerClaimControlNumber;
        this.AcknowledgedAtUtc = AcknowledgedAtUtc;
        this.Acknowledgements = Acknowledgements;
    }
    public Guid ClaimId { get; init; }
    public string Status { get; init; }
    public string? ExternalControlNumber { get; init; }
    public string? PayerClaimControlNumber { get; init; }
    public DateTime? AcknowledgedAtUtc { get; init; }
    public IReadOnlyList<ClaimAckRow> Acknowledgements { get; init; }
    public void Deconstruct(out Guid ClaimId, out string Status, out string? ExternalControlNumber, out string? PayerClaimControlNumber, out DateTime? AcknowledgedAtUtc, out IReadOnlyList<ClaimAckRow> Acknowledgements)
    {
        ClaimId = this.ClaimId;
        Status = this.Status;
        ExternalControlNumber = this.ExternalControlNumber;
        PayerClaimControlNumber = this.PayerClaimControlNumber;
        AcknowledgedAtUtc = this.AcknowledgedAtUtc;
        Acknowledgements = this.Acknowledgements;
    }
}

public sealed record ClaimAckRow
{
    public ClaimAckRow(Guid AcknowledgementId,
        string Kind,
        string Verdict,
        string? PayerClaimControlNumber,
        IReadOnlyList<string> ReasonCodes,
        DateTime ReceivedAtUtc)
    {
        this.AcknowledgementId = AcknowledgementId;
        this.Kind = Kind;
        this.Verdict = Verdict;
        this.PayerClaimControlNumber = PayerClaimControlNumber;
        this.ReasonCodes = ReasonCodes;
        this.ReceivedAtUtc = ReceivedAtUtc;
    }
    public Guid AcknowledgementId { get; init; }
    public string Kind { get; init; }
    public string Verdict { get; init; }
    public string? PayerClaimControlNumber { get; init; }
    public IReadOnlyList<string> ReasonCodes { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public void Deconstruct(out Guid AcknowledgementId, out string Kind, out string Verdict, out string? PayerClaimControlNumber, out IReadOnlyList<string> ReasonCodes, out DateTime ReceivedAtUtc)
    {
        AcknowledgementId = this.AcknowledgementId;
        Kind = this.Kind;
        Verdict = this.Verdict;
        PayerClaimControlNumber = this.PayerClaimControlNumber;
        ReasonCodes = this.ReasonCodes;
        ReceivedAtUtc = this.ReceivedAtUtc;
    }
}
