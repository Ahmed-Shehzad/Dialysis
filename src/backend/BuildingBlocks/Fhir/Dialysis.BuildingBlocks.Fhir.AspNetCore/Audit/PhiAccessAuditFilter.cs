using System.Security.Claims;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using AuditEvent = Hl7.Fhir.Model.AuditEvent;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;

/// <summary>
/// Action filter that emits a FHIR <see cref="AuditEvent"/> for every controller action
/// tagged with <see cref="PhiAccessAttribute"/>. Runs <em>after</em> the action so the
/// filter can record the HTTP status code (success / 4xx / 5xx) on the audit row.
///
/// The emitted event uses the FHIR R4 IHE BALP profile shape:
/// <list type="bullet">
///   <item><c>Type</c> = <c>rest</c> / <c>execute</c> — REST operation.</item>
///   <item><c>SubType</c> = the route's HTTP verb (read / create / update / delete).</item>
///   <item><c>Action</c> = R/C/U/D mapped from the verb.</item>
///   <item><c>Agent[0]</c> = the operator (sub from the JWT bearer).</item>
///   <item><c>Entity</c> contains the patient id; optionally the session id when the
///         attribute declares <c>SessionIdRouteKey</c>.</item>
/// </list>
///
/// Failures inside this filter never block the response — audit emission is logged on
/// error and the controller's response goes through unchanged.
/// </summary>
public sealed class PhiAccessAuditFilter : IAsyncActionFilter
{
    private readonly IAuditEventEmitter _emitter;
    private readonly TimeProvider _clock;
    private readonly ILogger<PhiAccessAuditFilter> _logger;
    /// <summary>
    /// Action filter that emits a FHIR <see cref="AuditEvent"/> for every controller action
    /// tagged with <see cref="PhiAccessAttribute"/>. Runs <em>after</em> the action so the
    /// filter can record the HTTP status code (success / 4xx / 5xx) on the audit row.
    ///
    /// The emitted event uses the FHIR R4 IHE BALP profile shape:
    /// <list type="bullet">
    ///   <item><c>Type</c> = <c>rest</c> / <c>execute</c> — REST operation.</item>
    ///   <item><c>SubType</c> = the route's HTTP verb (read / create / update / delete).</item>
    ///   <item><c>Action</c> = R/C/U/D mapped from the verb.</item>
    ///   <item><c>Agent[0]</c> = the operator (sub from the JWT bearer).</item>
    ///   <item><c>Entity</c> contains the patient id; optionally the session id when the
    ///         attribute declares <c>SessionIdRouteKey</c>.</item>
    /// </list>
    ///
    /// Failures inside this filter never block the response — audit emission is logged on
    /// error and the controller's response goes through unchanged.
    /// </summary>
    public PhiAccessAuditFilter(IAuditEventEmitter emitter,
        TimeProvider clock,
        ILogger<PhiAccessAuditFilter> logger)
    {
        _emitter = emitter;
        _clock = clock;
        _logger = logger;
    }
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next().ConfigureAwait(false);
        var attribute = FindAttribute(executed);
        if (attribute is null)
            return;

        try
        {
            var auditEvent = BuildAuditEvent(executed, attribute);
            await _emitter.EmitAsync(auditEvent, context.HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to emit AuditEvent for activity {Activity}; the controller response is unaffected.",
                attribute.ActivityName);
        }
    }

    private static PhiAccessAttribute? FindAttribute(ActionExecutedContext context)
    {
        return context.ActionDescriptor.EndpointMetadata
            .OfType<PhiAccessAttribute>()
            .FirstOrDefault();
    }

    private AuditEvent BuildAuditEvent(ActionExecutedContext context, PhiAccessAttribute attribute)
    {
        var request = context.HttpContext.Request;
        var statusCode = context.HttpContext.Response.StatusCode;
        var actorSub = context.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.HttpContext.User?.FindFirstValue("sub")
            ?? "anonymous";
        var patientId = ResolveRouteValue(request.RouteValues, attribute.PatientIdRouteKey);
        var sessionId = attribute.SessionIdRouteKey is null
            ? null
            : ResolveRouteValue(request.RouteValues, attribute.SessionIdRouteKey);

        var verb = request.Method.ToUpperInvariant();
        var auditEvent = new AuditEvent
        {
            Recorded = _clock.GetUtcNow().UtcDateTime,
            Type = new Coding("http://terminology.hl7.org/CodeSystem/audit-event-type", "rest", "RESTful Operation"),
            Action = MapAction(verb),
            Outcome = statusCode is >= 200 and < 400 ? AuditEvent.AuditEventOutcome.N0 : AuditEvent.AuditEventOutcome.N4,
            Agent =
            [
                new AuditEvent.AgentComponent
                {
                    Type = new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/extra-security-role-type",
                        "humanuser",
                        "human user"),
                    Who = new ResourceReference($"Practitioner/{actorSub}"),
                    Requestor = true,
                },
            ],
            Source = new AuditEvent.SourceComponent
            {
                Site = attribute.ActivityName,
                Observer = new ResourceReference("Device/dialysis-platform"),
            },
        };

        auditEvent.Subtype.Add(new Coding("http://hl7.org/fhir/restful-interaction", verb.ToLowerInvariant()));

        if (!string.IsNullOrWhiteSpace(patientId))
        {
            auditEvent.Entity.Add(new AuditEvent.EntityComponent
            {
                What = new ResourceReference($"Patient/{patientId}"),
                Type = new Coding("http://terminology.hl7.org/CodeSystem/audit-entity-type", "1", "Person"),
                Role = new Coding("http://terminology.hl7.org/CodeSystem/object-role", "1", "Patient"),
            });
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            auditEvent.Entity.Add(new AuditEvent.EntityComponent
            {
                What = new ResourceReference($"Encounter/{sessionId}"),
                Type = new Coding("http://terminology.hl7.org/CodeSystem/audit-entity-type", "2", "System Object"),
                Role = new Coding("http://terminology.hl7.org/CodeSystem/object-role", "3", "Report"),
            });
        }

        return auditEvent;
    }

    private static string? ResolveRouteValue(IDictionary<string, object?> values, string key)
    {
        if (!values.TryGetValue(key, out var raw) || raw is null)
            return null;
        var s = raw.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static AuditEvent.AuditEventAction MapAction(string verb) => verb switch
    {
        "GET" => AuditEvent.AuditEventAction.R,
        "POST" => AuditEvent.AuditEventAction.C,
        "PUT" or "PATCH" => AuditEvent.AuditEventAction.U,
        "DELETE" => AuditEvent.AuditEventAction.D,
        _ => AuditEvent.AuditEventAction.E,
    };
}
