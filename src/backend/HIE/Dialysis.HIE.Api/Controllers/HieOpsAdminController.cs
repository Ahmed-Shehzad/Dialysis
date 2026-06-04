using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Inbound.Features.ListInboundResources;
using Dialysis.HIE.Outbound.Features.ListOutboundBundles;
using Dialysis.HIE.Outbound.Features.ListPartners;
using Dialysis.HIE.Outbound.Features.RetryOutboundBundle;
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
