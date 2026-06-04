using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// End-to-end orchestrator for IG §5 prescription transfer: parse inbound query → call
/// resolver → build response. Mirrors the <c>PdqResponder</c> shape so the inbound
/// channel hook can route by <see cref="Hl7V2RxQueryParser.IsPrescriptionQuery"/> and
/// pick the right orchestrator without duplicating parsing logic.
/// </summary>
public sealed class PrescriptionResponder
{
    private readonly IPrescriptionResolver _resolver;
    private readonly TimeProvider _time;
    /// <summary>
    /// End-to-end orchestrator for IG §5 prescription transfer: parse inbound query → call
    /// resolver → build response. Mirrors the <c>PdqResponder</c> shape so the inbound
    /// channel hook can route by <see cref="Hl7V2RxQueryParser.IsPrescriptionQuery"/> and
    /// pick the right orchestrator without duplicating parsing logic.
    /// </summary>
    public PrescriptionResponder(IPrescriptionResolver resolver, TimeProvider time)
    {
        _resolver = resolver;
        _time = time;
    }
    public async Task<string> RespondAsync(Hl7V2Message inbound, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inbound);

        var query = Hl7V2RxQueryParser.Parse(inbound);
        var document = await _resolver.ResolveAsync(query, cancellationToken).ConfigureAwait(false);
        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var responseControlId = $"{query.MessageControlId}-R";
        return Hl7V2RxResponseBuilder.Build(query, document, responseControlId, nowUtc);
    }
}
