using Dialysis.BuildingBlocks.Fhir.Terminology;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Inbound.Terminology;

/// <summary>
/// At host startup, overlays every <c>active</c> authored terminology resource onto the in-memory
/// <see cref="DialysisTerminologyCatalog"/> so DB-authored ValueSets/CodeSystems/ConceptMaps serve
/// via the terminology endpoints alongside the built-ins. Runs once; a malformed authored row is
/// logged and skipped (it never blocks startup), and authored versioning takes effect on the next
/// restart by design.
/// </summary>
public sealed class TerminologyCatalogLoader : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DialysisTerminologyCatalog _catalog;
    private readonly ILogger<TerminologyCatalogLoader> _logger;

    public TerminologyCatalogLoader(
        IServiceScopeFactory scopeFactory,
        DialysisTerminologyCatalog catalog,
        ILogger<TerminologyCatalogLoader> logger)
    {
        _scopeFactory = scopeFactory;
        _catalog = catalog;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetService<IAuthoredTerminologyRepository>();
        if (repository is null) return; // authoring not wired on this host

        IReadOnlyList<AuthoredTerminologyResource> active;
        try
        {
            active = await repository.ListActiveAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A missing table / migration shouldn't crash the host — the built-in catalog still serves.
            _logger.LogWarning(ex, "Could not load authored terminology resources; serving built-ins only.");
            return;
        }

        var parser = new FhirJsonDeserializer(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));
        var loaded = 0;
        foreach (var row in active)
        {
            try
            {
                _catalog.Register(parser.Deserialize<Resource>(row.FhirJson));
                loaded++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Skipping authored terminology {Url}@{Version}: {Message}", row.Url, row.Version, ex.Message);
            }
        }

        if (loaded > 0)
            _logger.LogInformation("Overlaid {Count} authored terminology resource(s) onto the catalog.", loaded);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
