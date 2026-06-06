using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes.Features.OrderSets;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Order sets — reusable, standardized order bundles. Authoring (create/deactivate/list) plus apply,
/// which fans out to the individual order commands so each line runs the point-of-care safety checks.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/order-sets")]
public sealed class OrderSetController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public OrderSetController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>Lists active order sets for the apply picker.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderSetView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var sets = await _gateway.SendQueryAsync<ListOrderSetsQuery, IReadOnlyList<OrderSetView>>(
            new ListOrderSetsQuery(take), cancellationToken).ConfigureAwait(false);
        return Ok(sets);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateOrderSetCommand body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<CreateOrderSetCommand, Guid>(body, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/order-sets/{id}", new { id });
    }

    [HttpPost("{orderSetId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAsync(Guid orderSetId, CancellationToken cancellationToken)
    {
        await _gateway.SendCommandAsync<DeactivateOrderSetCommand, Unit>(
            new DeactivateOrderSetCommand(orderSetId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Applies an order set to a patient/encounter. A blocking safety advisory on any line returns
    /// <c>422</c> with the advisory list until re-submitted with <c>acknowledgeAdvisories</c> + a reason.
    /// </summary>
    [HttpPost("{orderSetId:guid}/apply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ApplyAsync(
        Guid orderSetId,
        [FromBody] ApplyOrderSetRequest body,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            var result = await _gateway.SendCommandAsync<ApplyOrderSetCommand, ApplyOrderSetResult>(
                new ApplyOrderSetCommand(orderSetId, body.PatientId, body.EncounterId, body.OrderingProviderId,
                    AcknowledgeAdvisories: body.AcknowledgeAdvisories, OverrideReason: body.OverrideReason,
                    OverriddenBy: currentUser.UserId ?? "clinician"),
                cancellationToken).ConfigureAwait(false);
            return Ok(new
            {
                orders = result.Orders,
                advisories = result.Advisories.Select(ToAdvisoryDto),
            });
        }
        catch (ClinicalSafetyBlockedException ex)
        {
            return UnprocessableEntity(new { advisories = ex.Advisories.Select(ToAdvisoryDto) });
        }
    }

    private static object ToAdvisoryDto(SafetyAdvisory a) => new
    {
        category = a.Category.ToString(),
        severity = a.Severity.ToString(),
        matchedCode = a.MatchedCode,
        matchedDisplay = a.MatchedDisplay,
        orderedConcept = a.OrderedConcept,
        sourceRowId = a.SourceRowId,
        sourceKind = a.SourceKind,
        detail = a.Detail,
    };

    /// <summary>Apply-order-set request body.</summary>
    public sealed record ApplyOrderSetRequest(
        Guid PatientId,
        Guid EncounterId,
        Guid OrderingProviderId,
        bool AcknowledgeAdvisories = false,
        string? OverrideReason = null);
}
