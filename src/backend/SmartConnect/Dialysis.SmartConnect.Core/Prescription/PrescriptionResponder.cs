using Dialysis.SmartConnect.DataTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private readonly ILogger<PrescriptionResponder> _logger;
    /// <summary>
    /// End-to-end orchestrator for IG §5 prescription transfer: parse inbound query → call
    /// resolver → build response. Mirrors the <c>PdqResponder</c> shape so the inbound
    /// channel hook can route by <see cref="Hl7V2RxQueryParser.IsPrescriptionQuery"/> and
    /// pick the right orchestrator without duplicating parsing logic.
    /// </summary>
    public PrescriptionResponder(
        IPrescriptionResolver resolver,
        TimeProvider time,
        ILogger<PrescriptionResponder>? logger = null)
    {
        _resolver = resolver;
        _time = time;
        _logger = logger ?? NullLogger<PrescriptionResponder>.Instance;
    }
    public async Task<string> RespondAsync(Hl7V2Message inbound, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inbound);

        var query = Hl7V2RxQueryParser.Parse(inbound);
        var document = await _resolver.ResolveAsync(query, cancellationToken).ConfigureAwait(false);

        // Modality ↔ channel mismatches are warnings, never refusals: the machine always
        // gets the channels the prescription actually carries (observation streams, not a
        // monolithic modality payload); integrators triage the warnings from the logs.
        if (document is not null)
        {
            foreach (var warning in document.GetModalityConsistencyWarnings())
            {
                _logger.LogWarning(
                    "Prescription {OrderNumber} ({Modality}) consistency: {Warning}",
                    document.OrderNumber, document.Modality, warning);
            }
        }

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var responseControlId = $"{query.MessageControlId}-R";
        return Hl7V2RxResponseBuilder.Build(query, document, responseControlId, nowUtc);
    }
}
