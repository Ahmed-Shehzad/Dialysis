using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Consent.Features.GrantConsent;
using Dialysis.HIE.Consent.Features.ListConsentsForPatient;
using Dialysis.HIE.Consent.Features.RevokeConsent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers;

/// <summary>
/// Admin endpoints for patient cross-organization consents. Uses the standard HATEOAS envelope
/// (FHIR endpoints in <c>FhirController</c> remain spec-compliant native FHIR JSON).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hie/consents")]
[Authorize]
public sealed class ConsentAdminController : ControllerBase
{
    private readonly ICqrsGateway _cqrs;
    /// <summary>
    /// Admin endpoints for patient cross-organization consents. Uses the standard HATEOAS envelope
    /// (FHIR endpoints in <c>FhirController</c> remain spec-compliant native FHIR JSON).
    /// </summary>
    public ConsentAdminController(ICqrsGateway cqrs) => _cqrs = cqrs;
    [HttpPost]
    [ProducesResponseType(typeof(ResourceEnvelope<GrantedConsentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> GrantAsync([FromBody] GrantConsentRequest body, CancellationToken cancellationToken)
    {
        var command = new GrantConsentCommand(
            body.PatientId,
            body.PartnerId,
            body.Scope,
            body.Direction,
            body.EffectiveFromUtc,
            body.EffectiveToUtc);
        var id = await _cqrs.SendCommandAsync<GrantConsentCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        var location = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1.0/hie/consents/patient/{body.PatientId}";
        var envelope = new ResourceEnvelope<GrantedConsentDto>(
            new GrantedConsentDto(id),
            [
                new LinkDto("self", $"{location}", "GET"),
                new LinkDto("hie:consent:revoke", $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v1.0/hie/consents/{id}", "DELETE"),
            ]);
        return Created(location, envelope);
    }

    [HttpDelete("{consentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAsync(Guid consentId, CancellationToken cancellationToken)
    {
        await _cqrs.SendCommandAsync<RevokeConsentCommand, Unit>(new RevokeConsentCommand(consentId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("patient/{patientId:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<ConsentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var rows = await _cqrs.SendQueryAsync<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>>(
            new ListConsentsForPatientQuery(patientId), cancellationToken).ConfigureAwait(false);
        var self = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        var envelope = new ResourceEnvelope<IReadOnlyList<ConsentDto>>(
            rows,
            [new LinkDto("self", self, "GET")]);
        return Ok(envelope);
    }

    public sealed record GrantConsentRequest
    {
        public GrantConsentRequest(Guid PatientId,
            string PartnerId,
            string Scope,
            ConsentDirection Direction,
            DateTime EffectiveFromUtc,
            DateTime? EffectiveToUtc)
        {
            this.PatientId = PatientId;
            this.PartnerId = PartnerId;
            this.Scope = Scope;
            this.Direction = Direction;
            this.EffectiveFromUtc = EffectiveFromUtc;
            this.EffectiveToUtc = EffectiveToUtc;
        }
        public Guid PatientId { get; init; }
        public string PartnerId { get; init; }
        public string Scope { get; init; }
        public ConsentDirection Direction { get; init; }
        public DateTime EffectiveFromUtc { get; init; }
        public DateTime? EffectiveToUtc { get; init; }
        public void Deconstruct(out Guid patientId, out string partnerId, out string scope, out ConsentDirection direction, out DateTime effectiveFromUtc, out DateTime? effectiveToUtc)
        {
            patientId = this.PatientId;
            partnerId = this.PartnerId;
            scope = this.Scope;
            direction = this.Direction;
            effectiveFromUtc = this.EffectiveFromUtc;
            effectiveToUtc = this.EffectiveToUtc;
        }
    }

    public sealed record GrantedConsentDto
    {
        public GrantedConsentDto(Guid ConsentId) => this.ConsentId = ConsentId;
        public Guid ConsentId { get; init; }
        public void Deconstruct(out Guid consentId) => consentId = this.ConsentId;
    }
}
