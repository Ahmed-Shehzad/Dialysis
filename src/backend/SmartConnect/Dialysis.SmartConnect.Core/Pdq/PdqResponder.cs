using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Pdq;

/// <summary>
/// End-to-end PDQ responder: parses an inbound <c>QBP^Q22^QBP_Q21</c>, resolves matches
/// via the registered <see cref="IPatientDemographicsResolver"/>, and emits an
/// <c>RSP^K22^RSP_K21</c> response frame ready to send back over the originating channel.
///
/// Keeping all three pieces in a single orchestrator lets tests round-trip the IG sample
/// messages (Section 4.3) end-to-end without spinning up DI; the production host injects
/// the resolver implementation and calls <see cref="RespondAsync"/> from its inbound
/// channel handler.
/// </summary>
public sealed class PdqResponder
{
    private readonly IPatientDemographicsResolver _resolver;
    private readonly TimeProvider _time;
    /// <summary>
    /// End-to-end PDQ responder: parses an inbound <c>QBP^Q22^QBP_Q21</c>, resolves matches
    /// via the registered <see cref="IPatientDemographicsResolver"/>, and emits an
    /// <c>RSP^K22^RSP_K21</c> response frame ready to send back over the originating channel.
    ///
    /// Keeping all three pieces in a single orchestrator lets tests round-trip the IG sample
    /// messages (Section 4.3) end-to-end without spinning up DI; the production host injects
    /// the resolver implementation and calls <see cref="RespondAsync"/> from its inbound
    /// channel handler.
    /// </summary>
    public PdqResponder(IPatientDemographicsResolver resolver, TimeProvider time)
    {
        _resolver = resolver;
        _time = time;
    }
    public async Task<string> RespondAsync(Hl7V2Message inbound, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inbound);

        var criteria = Hl7V2QbpQ22Parser.Parse(inbound);
        var matches = await _resolver.ResolveAsync(criteria, cancellationToken).ConfigureAwait(false);
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        // Fresh control id for the response — deterministic on the inbound id so a
        // re-delivered query produces an idempotent reply with the same MSH-10.
        var responseControlId = $"{criteria.MessageControlId}-R";
        return Hl7V2RspK22Builder.Build(criteria, matches, responseControlId, nowUtc);
    }
}
