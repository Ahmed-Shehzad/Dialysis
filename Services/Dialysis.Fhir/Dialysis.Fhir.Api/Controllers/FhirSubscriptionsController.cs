using Hl7.Fhir.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Fhir.Api.Controllers;

/// <summary>
/// FHIR R4 Subscription resource CRUD.
/// Channel types: rest-hook (callback URL), websocket (SignalR) – dispatcher TBD.
/// </summary>
[ApiController]
[Route("api/fhir/Subscription")]
[Authorize(Policy = "FhirExport")]
public sealed class FhirSubscriptionsController : ControllerBase
{
    private static readonly Dictionary<string, Subscription> Store = new(StringComparer.OrdinalIgnoreCase);

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
        lock (Store) { Store[id] = subscription; }

        return CreatedAtAction(nameof(Get), new { id }, subscription);
    }

    [HttpGet("{id}")]
    [Produces("application/fhir+json", "application/json")]
    [ProducesResponseType(typeof(Subscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(string id)
    {
        lock (Store)
        {
            if (Store.TryGetValue(id, out Subscription? sub))
                return Content(Dialysis.Hl7ToFhir.FhirJsonHelper.ToJson(sub!), "application/fhir+json");
        }
        return NotFound();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(string id)
    {
        lock (Store)
        {
            if (Store.Remove(id))
                return NoContent();
        }
        return NotFound();
    }
}
