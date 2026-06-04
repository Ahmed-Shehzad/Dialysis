using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Intercessor;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Hipaa.Audit;

/// <summary>
/// Intercessor pipeline behaviour that emits a FHIR <c>AuditEvent</c> for every request whose type
/// carries <see cref="PhiAccessAttribute"/>. Handlers cannot forget the audit because the pipeline
/// runs unconditionally — opting out requires removing the attribute, which is reviewable in code.
///
/// Failure semantics: when the handler throws the behaviour emits a minor-failure audit before
/// rethrowing, so a 500 to the operator still leaves an attempted-access trail.
///
/// Side-effect safety: the audit emission goes through <see cref="IAuditEventEmitter"/>, which the
/// host wires to either an in-memory store (dev / tests) or the EF-backed
/// <c>Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore</c> persistence.
/// </summary>
public sealed class HipaaAuditingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditEventEmitter _emitter;
    private readonly IHipaaAuditContext _context;
    private readonly ILogger<HipaaAuditingBehavior<TRequest, TResponse>> _logger;
    /// <summary>
    /// Intercessor pipeline behaviour that emits a FHIR <c>AuditEvent</c> for every request whose type
    /// carries <see cref="PhiAccessAttribute"/>. Handlers cannot forget the audit because the pipeline
    /// runs unconditionally — opting out requires removing the attribute, which is reviewable in code.
    ///
    /// Failure semantics: when the handler throws the behaviour emits a minor-failure audit before
    /// rethrowing, so a 500 to the operator still leaves an attempted-access trail.
    ///
    /// Side-effect safety: the audit emission goes through <see cref="IAuditEventEmitter"/>, which the
    /// host wires to either an in-memory store (dev / tests) or the EF-backed
    /// <c>Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore</c> persistence.
    /// </summary>
    public HipaaAuditingBehavior(IAuditEventEmitter emitter,
        IHipaaAuditContext context,
        ILogger<HipaaAuditingBehavior<TRequest, TResponse>> logger)
    {
        _emitter = emitter;
        _context = context;
        _logger = logger;
    }

    private static readonly PhiAccessAttribute? _attribute =
        (PhiAccessAttribute?)Attribute.GetCustomAttribute(typeof(TRequest), typeof(PhiAccessAttribute), inherit: false);

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_attribute is null)
        {
            return await next().ConfigureAwait(false);
        }

        try
        {
            var response = await next().ConfigureAwait(false);
            await TryEmitAsync(success: true, errorDetail: null, cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await TryEmitAsync(success: false, errorDetail: ex.Message, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async System.Threading.Tasks.Task TryEmitAsync(bool success, string? errorDetail, CancellationToken cancellationToken)
    {
        var ev = new AuditEvent
        {
            Type = new Coding("http://terminology.hl7.org/CodeSystem/audit-event-type", "rest"),
            Subtype = [new Coding("http://dialysis.local/CodeSystem/phi-access", _attribute!.Action.ToString().ToLowerInvariant())],
            Action = MapAction(_attribute.Action),
            Recorded = DateTimeOffset.UtcNow,
            Outcome = success ? AuditEvent.AuditEventOutcome.N0 : AuditEvent.AuditEventOutcome.N4,
            OutcomeDesc = errorDetail,
            Source = new AuditEvent.SourceComponent
            {
                Site = _context.ModuleSlug,
                Observer = new ResourceReference($"Device/{_context.ModuleSlug}-host"),
            },
        };

        ev.Agent.Add(new AuditEvent.AgentComponent
        {
            Requestor = true,
            Who = string.IsNullOrEmpty(_context.CurrentUserId)
                ? null
                : new ResourceReference($"Practitioner/{_context.CurrentUserId}"),
        });

        ev.Entity.Add(new AuditEvent.EntityComponent
        {
            What = new ResourceReference($"{_attribute.FhirResourceType}/{typeof(TRequest).Name}"),
            Description = typeof(TRequest).FullName,
        });

        try
        {
            await _emitter.EmitAsync(ev, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The audit emitter is best-effort within a request scope — losing one event must not
            // break the handler. The compliance dashboard surfaces emitter failures via its own
            // safeguard check, and the framework log keeps a record for postmortem.
            _logger.LogWarning(ex, "HipaaAuditingBehavior failed to emit AuditEvent for {Request}", typeof(TRequest).Name);
        }
    }

    private static AuditEvent.AuditEventAction MapAction(PhiAccessAction a) => a switch
    {
        PhiAccessAction.Create => AuditEvent.AuditEventAction.C,
        PhiAccessAction.Read => AuditEvent.AuditEventAction.R,
        PhiAccessAction.Update => AuditEvent.AuditEventAction.U,
        PhiAccessAction.Delete => AuditEvent.AuditEventAction.D,
        PhiAccessAction.Execute => AuditEvent.AuditEventAction.E,
        _ => AuditEvent.AuditEventAction.R,
    };
}

/// <summary>
/// Per-request context the audit behaviour reads from the DI container. Hosts implement this to
/// surface the current user (from JWT claims) and the module slug (used in <c>AuditEvent.source.site</c>).
/// </summary>
public interface IHipaaAuditContext
{
    string ModuleSlug { get; }
    string? CurrentUserId { get; }
}
