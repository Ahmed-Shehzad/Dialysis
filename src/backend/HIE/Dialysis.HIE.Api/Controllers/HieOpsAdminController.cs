using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Inbound.Features.ListInboundResources;
using Dialysis.HIE.Outbound.CareSummary;
using Dialysis.HIE.Outbound.Features.GenerateCareSummary;
using Dialysis.HIE.Outbound.Features.ListOutboundBundles;
using Dialysis.HIE.Outbound.Features.ListPartners;
using Dialysis.HIE.Outbound.Features.RetryOutboundBundle;
using Dialysis.HIE.Query.Features.PullOutsideRecords;
using Dialysis.HIE.Query.Features.PullPartnerDocuments;
using Dialysis.HIE.Query.Features.PullPartnerRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers;

/// <summary>
/// Operator-facing dashboard endpoints for HIE: outbound dispatch queue, inbound feed by
/// partner, and TEFCA partner-configuration status. Backs the FHIR exchange admin panels
/// in the SPA (Phase 3b). FHIR endpoints continue to live in <see cref="Inbound.Controllers.FhirController"/>
/// and remain spec-compliant native FHIR JSON; the operator dashboard uses the standard
/// HATEOAS envelope.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hie/ops")]
[Authorize]
public sealed class HieOpsAdminController : ControllerBase
{
    private readonly ICqrsGateway _cqrs;
    /// <summary>
    /// Operator-facing dashboard endpoints for HIE: outbound dispatch queue, inbound feed by
    /// partner, and TEFCA partner-configuration status. Backs the FHIR exchange admin panels
    /// in the SPA (Phase 3b). FHIR endpoints continue to live in <see cref="Inbound.Controllers.FhirController"/>
    /// and remain spec-compliant native FHIR JSON; the operator dashboard uses the standard
    /// HATEOAS envelope.
    /// </summary>
    public HieOpsAdminController(ICqrsGateway cqrs) => _cqrs = cqrs;
    /// <summary>
    /// Lists outbound bundles. Filter by status with <c>?status=1</c> (Pending),
    /// <c>2</c> (Delivered), or <c>3</c> (Failed). Default returns every status, ordered
    /// most-recent first, capped at <paramref name="take"/>.
    /// </summary>
    [HttpGet("outbound")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<OutboundBundleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOutboundAsync(
        [FromQuery] int? status = null,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _cqrs.SendQueryAsync<ListOutboundBundlesQuery, IReadOnlyList<OutboundBundleDto>>(
            new ListOutboundBundlesQuery(status, take), cancellationToken).ConfigureAwait(false);
        return OkResource(rows);
    }

    /// <summary>
    /// Re-queues a Failed (or Pending stuck-behind-NextAttempt) outbound bundle by resetting
    /// it to Pending with NextAttemptAtUtc=now. Idempotent: bundles already Delivered are
    /// silently absorbed by the aggregate.
    /// </summary>
    [HttpPost("outbound/{bundleId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RetryOutboundAsync(Guid bundleId, CancellationToken cancellationToken)
    {
        await _cqrs.SendCommandAsync<RetryOutboundBundleCommand, Unit>(
            new RetryOutboundBundleCommand(bundleId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Assembles a Continuity of Care Document (CCD) for the patient from the FHIR resources HIE
    /// has already mapped and queues it for Directed Exchange. Returns <c>200</c> with the queued
    /// bundle id when generated, or <c>422</c> when there's nothing to summarise / consent denied.
    /// Optional <c>?purpose=</c> sets the TEFCA permitted purpose (defaults to Treatment).
    /// </summary>
    [HttpPost("outbound/care-summary/{patientId:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<CareSummaryResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResourceEnvelope<CareSummaryResult>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateCareSummaryAsync(
        Guid patientId,
        [FromQuery] string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cqrs.SendCommandAsync<GenerateCareSummaryCommand, CareSummaryResult>(
            new GenerateCareSummaryCommand(patientId, purpose), cancellationToken).ConfigureAwait(false);
        var envelope = new ResourceEnvelope<CareSummaryResult>(
            result,
            [new LinkDto("self", $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}", "POST")]);
        return result.Generated ? Ok(envelope) : UnprocessableEntity(envelope);
    }

    /// <summary>
    /// Query-based exchange (pull): fetches records for a patient from a partner QHIN and feeds them
    /// into the inbound ingestion pipeline. <paramref name="query"/> is a relative FHIR query
    /// (e.g. <c>Patient/123/$everything</c>); <paramref name="subject"/> is the partner-side patient id
    /// (the IAS JWT subject); optional <c>?purpose=</c> sets the TEFCA permitted purpose.
    /// </summary>
    [HttpPost("query/partner/{partnerId:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<PartnerPullResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PullPartnerRecordsAsync(
        Guid partnerId,
        [FromQuery] string query,
        [FromQuery] string subject,
        [FromQuery] string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cqrs.SendCommandAsync<PullPartnerRecordsCommand, PartnerPullResult>(
            new PullPartnerRecordsCommand(partnerId, query, subject, purpose), cancellationToken).ConfigureAwait(false);
        return OkResource(result);
    }

    /// <summary>
    /// Cross-gateway document pull (XCA): queries a partner registry for the patient's documents,
    /// retrieves their content, and lands them through inbound ingestion. <paramref name="patient"/>
    /// is the partner-side patient id; optional <c>?purpose=</c> sets the TEFCA permitted purpose.
    /// </summary>
    [HttpPost("query/partner/{partnerId:guid}/documents")]
    [ProducesResponseType(typeof(ResourceEnvelope<PartnerPullResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PullPartnerDocumentsAsync(
        Guid partnerId,
        [FromQuery] string patient,
        [FromQuery] string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cqrs.SendCommandAsync<PullPartnerDocumentsCommand, PartnerPullResult>(
            new PullPartnerDocumentsCommand(partnerId, patient, purpose), cancellationToken).ConfigureAwait(false);
        return OkResource(result);
    }

    /// <summary>
    /// On-demand "pull this patient's outside records": resolves the patient at the partner
    /// (discovery, unless <c>partnerPatientId</c> is supplied), pulls records (<c>$everything</c>) and
    /// documents (XCA), and lands everything through inbound ingestion. Returns the candidate count,
    /// the resolved partner-side id, and how many records/documents were fetched.
    /// </summary>
    [HttpPost("query/partner/{partnerId:guid}/outside-records")]
    [ProducesResponseType(typeof(ResourceEnvelope<OutsideRecordsResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PullOutsideRecordsAsync(
        Guid partnerId,
        [FromQuery] string? partnerPatientId = null,
        [FromQuery] string? mrn = null,
        [FromQuery] string? family = null,
        [FromQuery] string? given = null,
        [FromQuery] DateOnly? birthDate = null,
        [FromQuery] string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cqrs.SendCommandAsync<PullOutsideRecordsCommand, OutsideRecordsResult>(
            new PullOutsideRecordsCommand(partnerId, partnerPatientId, mrn, family, given, birthDate, purpose),
            cancellationToken).ConfigureAwait(false);
        return OkResource(result);
    }

    /// <summary>
    /// Lists recently received inbound resources, optionally filtered to one partner.
    /// </summary>
    [HttpGet("inbound")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<InboundResourceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInboundAsync(
        [FromQuery] string? partnerId = null,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await _cqrs.SendQueryAsync<ListInboundResourcesQuery, IReadOnlyList<InboundResourceDto>>(
            new ListInboundResourcesQuery(partnerId, take), cancellationToken).ConfigureAwait(false);
        return OkResource(rows);
    }

    /// <summary>
    /// Returns the configured partner endpoints — one entry per <c>Hie:Partners:&lt;id&gt;</c>
    /// in configuration — with a coarse <c>IsConfigured</c> flag (base URL is a valid
    /// absolute URI) so operators can verify partner wiring before flipping a route live.
    /// </summary>
    [HttpGet("partners")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<PartnerStatusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPartnersAsync(CancellationToken cancellationToken)
    {
        var rows = await _cqrs.SendQueryAsync<ListPartnersQuery, IReadOnlyList<PartnerStatusDto>>(
            new ListPartnersQuery(), cancellationToken).ConfigureAwait(false);
        return OkResource(rows);
    }

    private OkObjectResult OkResource<T>(T data) =>
        Ok(new ResourceEnvelope<T>(
            data,
            [new LinkDto("self", $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}", "GET")]));
}
