using Dialysis.BuildingBlocks.Fhir.Terminology;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Core.Coding;

/// <summary>
/// On startup, calls <c>$lookup</c> against the configured terminology server for every entry in
/// <see cref="ConceptCatalog"/>. Two outcomes:
/// <list type="bullet">
///   <item>The concept resolves — its display is updated to the authoritative value returned by the server.</item>
///   <item>The concept does NOT resolve — a warning is logged. The fallback display continues to be served
///         so callers see no failure at runtime; the warning surfaces in module health/observability.</item>
/// </list>
/// The service never throws — terminology connectivity issues should not prevent the module from starting.
/// </summary>
public sealed class ConceptCatalogValidatorHostedService : IHostedService
{
    private readonly ConceptCatalog _catalog;
    private readonly ITerminologyService _terminology;
    private readonly ILogger<ConceptCatalogValidatorHostedService> _logger;
    /// <summary>
    /// On startup, calls <c>$lookup</c> against the configured terminology server for every entry in
    /// <see cref="ConceptCatalog"/>. Two outcomes:
    /// <list type="bullet">
    ///   <item>The concept resolves — its display is updated to the authoritative value returned by the server.</item>
    ///   <item>The concept does NOT resolve — a warning is logged. The fallback display continues to be served
    ///         so callers see no failure at runtime; the warning surfaces in module health/observability.</item>
    /// </list>
    /// The service never throws — terminology connectivity issues should not prevent the module from starting.
    /// </summary>
    public ConceptCatalogValidatorHostedService(ConceptCatalog catalog,
        ITerminologyService terminology,
        ILogger<ConceptCatalogValidatorHostedService> logger)
    {
        _catalog = catalog;
        _terminology = terminology;
        _logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in _catalog.Entries)
        {
            try
            {
                var parameters = await _terminology
                    .LookupAsync(entry.System, entry.Code, cancellationToken)
                    .ConfigureAwait(false);

                var display = parameters.Parameter
                    .Find(p => string.Equals(p.Name, "display", StringComparison.Ordinal))
                    ?.Value as FhirString;

                if (display?.Value is { Length: > 0 } authoritativeDisplay)
                {
                    _catalog.UpdateDisplay(entry.Name, authoritativeDisplay);
                    _logger.LogDebug(
                        "Validated concept {Name} ({System}#{Code}) — display \"{Display}\"",
                        entry.Name, entry.System, entry.Code, authoritativeDisplay);
                }
                else
                {
                    _logger.LogWarning(
                        "Concept {Name} ({System}#{Code}) did not resolve on upstream terminology server; using fallback display \"{Fallback}\"",
                        entry.Name, entry.System, entry.Code, entry.FallbackDisplay);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Concept validation skipped for {Name} ({System}#{Code}): terminology server unreachable",
                    entry.Name, entry.System, entry.Code);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
