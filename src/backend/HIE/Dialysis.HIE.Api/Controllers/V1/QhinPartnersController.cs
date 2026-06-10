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
public sealed class QhinPartnersController : ControllerBase
{
    private readonly ICqrsGateway _cqrs;
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
    public QhinPartnersController(ICqrsGateway cqrs) => _cqrs = cqrs;
    [HttpGet]
    [PhiAccess("hie.tefca.partners.list")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<QhinPartnerRow>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _cqrs.SendQueryAsync<ListQhinPartnersQuery, IReadOnlyList<QhinPartnerRow>>(
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
        var detail = await _cqrs.SendQueryAsync<GetQhinPartnerQuery, QhinPartnerDetail?>(
            new GetQhinPartnerQuery(id), cancellationToken).ConfigureAwait(false);
        if (detail is null)
            return NotFound();
        return Ok(new ResourceEnvelope<QhinPartnerDetail>(detail, [new LinkDto("self", Self(), "GET")]));
    }

    [HttpPost]
    [PhiAccess("hie.tefca.partners.onboard")]
    [ProducesResponseType(typeof(ResourceEnvelope<OnboardedDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> OnboardAsync([FromBody] OnboardBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _cqrs.SendCommandAsync<OnboardQhinPartnerCommand, Guid>(
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
        await _cqrs.SendCommandAsync<ReviseQhinPartnerCommand, Unit>(
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
        await _cqrs.SendCommandAsync<TransitionQhinPartnerStatusCommand, Unit>(
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
        var anchorId = await _cqrs.SendCommandAsync<AttachTrustAnchorCommand, Guid>(
            new AttachTrustAnchorCommand(id, body.CertificatePem, User.Identity?.Name ?? "operator"),
            cancellationToken).ConfigureAwait(false);
        return Ok(new ResourceEnvelope<AttachedAnchorDto>(new AttachedAnchorDto(anchorId), [new LinkDto("self", Self(), "GET")]));
    }

    [HttpDelete("{id:guid}/trust-anchors/{anchorId:guid}")]
    [PhiAccess("hie.tefca.partners.trust_anchor.revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAnchorAsync(Guid id, Guid anchorId, CancellationToken cancellationToken)
    {
        await _cqrs.SendCommandAsync<RevokeTrustAnchorCommand, Unit>(
            new RevokeTrustAnchorCommand(id, anchorId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{id:guid}/mtls")]
    [PhiAccess("hie.tefca.partners.mtls.rotate")]
    [ProducesResponseType(typeof(ResourceEnvelope<RotatedMtlsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateMtlsAsync(Guid id, [FromBody] MtlsBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var thumbprint = await _cqrs.SendCommandAsync<RotateMtlsCertificateCommand, string>(
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
        var token = await _cqrs.SendCommandAsync<IssueIasJwtCommand, string>(
            new IssueIasJwtCommand(id, body.SubjectPatientId, body.Scope ?? "patient.read", body.LifetimeSeconds ?? 300),
            cancellationToken).ConfigureAwait(false);
        return Ok(new ResourceEnvelope<IssuedIasJwtDto>(new IssuedIasJwtDto(token), [new LinkDto("self", Self(), "GET")]));
    }

    private string Self() => $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";

    public sealed record OnboardBody
    {
        public OnboardBody(string Name, string FhirBaseUrl, string IasEndpoint)
        {
            this.Name = Name;
            this.FhirBaseUrl = FhirBaseUrl;
            this.IasEndpoint = IasEndpoint;
        }
        public string Name { get; init; }
        public string FhirBaseUrl { get; init; }
        public string IasEndpoint { get; init; }
        public void Deconstruct(out string name, out string fhirBaseUrl, out string iasEndpoint)
        {
            name = Name;
            fhirBaseUrl = FhirBaseUrl;
            iasEndpoint = IasEndpoint;
        }
    }

    public sealed record StatusBody
    {
        public StatusBody(QhinPartnerStatus Next) => this.Next = Next;
        public QhinPartnerStatus Next { get; init; }
        public void Deconstruct(out QhinPartnerStatus next) => next = Next;
    }

    public sealed record AnchorBody
    {
        public AnchorBody(string CertificatePem) => this.CertificatePem = CertificatePem;
        public string CertificatePem { get; init; }
        public void Deconstruct(out string certificatePem) => certificatePem = CertificatePem;
    }

    public sealed record MtlsBody
    {
        public MtlsBody(string Base64Pfx, string PfxPassword)
        {
            this.Base64Pfx = Base64Pfx;
            this.PfxPassword = PfxPassword;
        }
        public string Base64Pfx { get; init; }
        public string PfxPassword { get; init; }
        public void Deconstruct(out string base64Pfx, out string pfxPassword)
        {
            base64Pfx = Base64Pfx;
            pfxPassword = PfxPassword;
        }
    }

    public sealed record IssueIasJwtBody
    {
        public IssueIasJwtBody(string SubjectPatientId, string? Scope, int? LifetimeSeconds)
        {
            this.SubjectPatientId = SubjectPatientId;
            this.Scope = Scope;
            this.LifetimeSeconds = LifetimeSeconds;
        }
        public string SubjectPatientId { get; init; }
        public string? Scope { get; init; }
        public int? LifetimeSeconds { get; init; }
        public void Deconstruct(out string subjectPatientId, out string? scope, out int? lifetimeSeconds)
        {
            subjectPatientId = SubjectPatientId;
            scope = Scope;
            lifetimeSeconds = LifetimeSeconds;
        }
    }

    public sealed record OnboardedDto
    {
        public OnboardedDto(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = Id;
    }

    public sealed record AttachedAnchorDto
    {
        public AttachedAnchorDto(Guid AnchorId) => this.AnchorId = AnchorId;
        public Guid AnchorId { get; init; }
        public void Deconstruct(out Guid anchorId) => anchorId = AnchorId;
    }

    public sealed record RotatedMtlsDto
    {
        public RotatedMtlsDto(string Thumbprint) => this.Thumbprint = Thumbprint;
        public string Thumbprint { get; init; }
        public void Deconstruct(out string thumbprint) => thumbprint = Thumbprint;
    }

    public sealed record IssuedIasJwtDto
    {
        public IssuedIasJwtDto(string Token) => this.Token = Token;
        public string Token { get; init; }
        public void Deconstruct(out string token) => token = Token;
    }
}
