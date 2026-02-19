using Dialysis.Fhir.Api.Subscriptions;

using Hl7.Fhir.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Fhir.Api.Controllers;

/// <summary>
/// FHIR R4 Subscription resource CRUD.
/// Channel types: rest-hook (callback URL) – dispatcher evaluates and POSTs to endpoints.
/// </summary>
[ApiController]
[Route("api/fhir/Subscription")]
[Authorize(Policy = "FhirExport")]
public sealed class FhirSubscriptionsController : ControllerBase
{
    private readonly ISubscriptionStore _store;

    public FhirSubscriptionsController(ISubscriptionStore store) => _store = store;

    [HttpPost]
    [Produces("application/fhir+json", "application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] Subscription subscription)
    {
        if (string.IsNullOrEmpty(subscription.Criteria) || subscription.Channel?.Endpoint is null)
            return BadRequest("Subscription requires criteria and channel.endpoint");

        string id = "sub-" + Ulid.NewUlid();
        subscription.Id = id;
        subscription.Status = Subscription.SubscriptionStatus.Active;
        _store.Add(id, subscription);

        return CreatedAtAction(nameof(Get), new { id }, subscription);
    }

    [HttpGet("{id}")]
    [Produces("application/fhir+json", "application/json")]
    [ProducesResponseType(typeof(Subscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(string id)
    {
        if (_store.TryGet(id, out Subscription? sub))
            return Content(Dialysis.Hl7ToFhir.FhirJsonHelper.ToJson(sub!), "application/fhir+json");

        return NotFound();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        if (_store.Remove(id))
            return NoContent();

        return NotFound();
    }
}
