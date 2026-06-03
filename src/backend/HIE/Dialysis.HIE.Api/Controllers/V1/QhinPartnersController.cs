using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Tefca.Domain;
using Dialysis.HIE.Tefca.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers.V1;

/// <summary>
/// US TEFCA QHIN partner onboarding surface. Operators manage:
/// <list type="bullet">
///   <item>partner identity (name, FHIR base URL, IAS endpoint);</item>
///   <item>lifecycle (Onboarding → Active → Suspended);</item>
///   <item>trust anchors (PEM X.509 certs attached / revoked);</item>
///   <item>mTLS material (PFX uploads stored via the shared blob store);</item>
///   <item>preview IAS JWTs the operator hands to the partner for handshake validation.</item>
/// </list>
/// Every endpoint is <c>[PhiAccess]</c>-audited so a regulator can verify the partner
/// configuration trail end-to-end.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tefca/partners")]
[Authorize]
public sealed class QhinPartnersController(ICqrsGateway cqrs) : ControllerBase
{
    [HttpGet]
    [PhiAccess("hie.tefca.partners.list")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<QhinPartnerRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await cqrs.SendQueryAsync<ListQhinPartnersQuery, IReadOnlyList<QhinPartnerRow>>(
            new ListQhinPartnersQuery(), cancellationToken).ConfigureAwait(false);
        var self = Self();
        return Ok(new ResourceEnvelope<IReadOnlyList<QhinPartnerRow>>(rows, [new LinkDto("self", self, "GET")]));
    }

    [HttpGet("{id:guid}")]
    [PhiAccess("hie.tefca.partners.read")]
    [ProducesResponseType(typeof(ResourceEnvelope<QhinPartnerDetail>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var detail = await cqrs.SendQueryAsync<GetQhinPartnerQuery, QhinPartnerDetail?>(
            new GetQhinPartnerQuery(id), cancellationToken).ConfigureAwait(false);
        if (detail is null) return NotFound();
        return Ok(new ResourceEnvelope<QhinPartnerDetail>(detail, [new LinkDto("self", Self(), "GET")]));
    }

    [HttpPost]
    [PhiAccess("hie.tefca.partners.onboard")]
    [ProducesResponseType(typeof(ResourceEnvelope<OnboardedDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> OnboardAsync([FromBody] OnboardBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await cqrs.SendCommandAsync<OnboardQhinPartnerCommand, Guid>(
            new OnboardQhinPartnerCommand(body.Name, body.FhirBaseUrl, body.IasEndpoint, User.Identity?.Name ?? "operator"),
            cancellationToken).ConfigureAwait(false);
        var location = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1.0/tefca/partners/{id}";
        return Created(location, new ResourceEnvelope<OnboardedDto>(
            new OnboardedDto(id),
            [new LinkDto("self", location, "GET")]));
    }

    [HttpPut("{id:guid}")]
    [PhiAccess("hie.tefca.partners.update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReviseAsync(Guid id, [FromBody] OnboardBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await cqrs.SendCommandAsync<ReviseQhinPartnerCommand, Unit>(
            new ReviseQhinPartnerCommand(id, body.Name, body.FhirBaseUrl, body.IasEndpoint, User.Identity?.Name ?? "operator"),
            cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    [PhiAccess("hie.tefca.partners.transition")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TransitionAsync(Guid id, [FromBody] StatusBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await cqrs.SendCommandAsync<TransitionQhinPartnerStatusCommand, Unit>(
            new TransitionQhinPartnerStatusCommand(id, body.Next, User.Identity?.Name ?? "operator"),
            cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{id:guid}/trust-anchors")]
    [PhiAccess("hie.tefca.partners.trust_anchor.attach")]
    [ProducesResponseType(typeof(ResourceEnvelope<AttachedAnchorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AttachAnchorAsync(Guid id, [FromBody] AnchorBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var anchorId = await cqrs.SendCommandAsync<AttachTrustAnchorCommand, Guid>(
            new AttachTrustAnchorCommand(id, body.CertificatePem, User.Identity?.Name ?? "operator"),
            cancellationToken).ConfigureAwait(false);
        return Ok(new ResourceEnvelope<AttachedAnchorDto>(new AttachedAnchorDto(anchorId), [new LinkDto("self", Self(), "GET")]));
    }

    [HttpDelete("{id:guid}/trust-anchors/{anchorId:guid}")]
    [PhiAccess("hie.tefca.partners.trust_anchor.revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAnchorAsync(Guid id, Guid anchorId, CancellationToken cancellationToken)
    {
        await cqrs.SendCommandAsync<RevokeTrustAnchorCommand, Unit>(
            new RevokeTrustAnchorCommand(id, anchorId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{id:guid}/mtls")]
    [PhiAccess("hie.tefca.partners.mtls.rotate")]
    [ProducesResponseType(typeof(ResourceEnvelope<RotatedMtlsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateMtlsAsync(Guid id, [FromBody] MtlsBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var thumbprint = await cqrs.SendCommandAsync<RotateMtlsCertificateCommand, string>(
            new RotateMtlsCertificateCommand(id, body.Base64Pfx, body.PfxPassword, User.Identity?.Name ?? "operator"),
            cancellationToken).ConfigureAwait(false);
        return Ok(new ResourceEnvelope<RotatedMtlsDto>(new RotatedMtlsDto(thumbprint), [new LinkDto("self", Self(), "GET")]));
    }

    [HttpPost("{id:guid}/ias-jwt")]
    [PhiAccess("hie.tefca.ias_jwt.issue")]
    [ProducesResponseType(typeof(ResourceEnvelope<IssuedIasJwtDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueIasJwtAsync(Guid id, [FromBody] IssueIasJwtBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var token = await cqrs.SendCommandAsync<IssueIasJwtCommand, string>(
            new IssueIasJwtCommand(id, body.SubjectPatientId, body.Scope ?? "patient.read", body.LifetimeSeconds ?? 300),
            cancellationToken).ConfigureAwait(false);
        return Ok(new ResourceEnvelope<IssuedIasJwtDto>(new IssuedIasJwtDto(token), [new LinkDto("self", Self(), "GET")]));
    }

    private string Self() => $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";

    public sealed record OnboardBody(string Name, string FhirBaseUrl, string IasEndpoint);
    public sealed record StatusBody(QhinPartnerStatus Next);
    public sealed record AnchorBody(string CertificatePem);
    public sealed record MtlsBody(string Base64Pfx, string PfxPassword);
    public sealed record IssueIasJwtBody(string SubjectPatientId, string? Scope, int? LifetimeSeconds);

    public sealed record OnboardedDto(Guid Id);
    public sealed record AttachedAnchorDto(Guid AnchorId);
    public sealed record RotatedMtlsDto(string Thumbprint);
    public sealed record IssuedIasJwtDto(string Token);
}
