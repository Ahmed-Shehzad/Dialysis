using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>Options for the on-demand FHIR authoring pipeline.</summary>
public sealed class FhirAuthoringOptions
{
    /// <summary>
    /// Directory scanned at startup for external FHIR packages (<c>*.tgz</c> / <c>*.tar.gz</c>),
    /// each loaded into the conformance registry so declared IG dependencies resolve without a
    /// manual upload after every restart. Null/empty disables the preloader.
    /// </summary>
    public string? PackagesPath { get; set; }
}

/// <summary>
/// Loads every FHIR package found in <see cref="FhirAuthoringOptions.PackagesPath"/> into the
/// conformance registry on host startup. The in-memory registry is rebuilt per process, so this
/// makes a configured package set survive restarts. Per-file failures are logged, never fatal.
/// </summary>
public sealed class FhirPackagePreloader : IHostedService
{
    private readonly IFhirPackageLoader _loader;
    private readonly FhirAuthoringOptions _options;
    private readonly ILogger<FhirPackagePreloader> _logger;
    /// <summary>
    /// Loads every FHIR package found in <see cref="FhirAuthoringOptions.PackagesPath"/> into the
    /// conformance registry on host startup. The in-memory registry is rebuilt per process, so this
    /// makes a configured package set survive restarts. Per-file failures are logged, never fatal.
    /// </summary>
    public FhirPackagePreloader(IFhirPackageLoader loader,
        FhirAuthoringOptions options,
        ILogger<FhirPackagePreloader> logger)
    {
        _loader = loader;
        _options = options;
        _logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var dir = _options.PackagesPath;
        if (string.IsNullOrWhiteSpace(dir))
            return;

        if (!Directory.Exists(dir))
        {
            _logger.LogWarning(
                "FHIR package preloader: directory '{Directory}' does not exist; skipping.", dir);
            return;
        }

        var files = Directory
            .EnumerateFiles(dir)
            .Where(f => f.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogInformation(
                "FHIR package preloader: no .tgz packages found in '{Directory}'.", dir);
            return;
        }

        var total = 0;
        foreach (var file in files)
        {
            try
            {
                var result = await _loader.LoadFileAsync(file, cancellationToken).ConfigureAwait(false);
                total += result.Loaded;
                _logger.LogInformation(
                    "FHIR package preloader: loaded {Loaded} resource(s) from {Package}@{Version} ({File}).",
                    result.Loaded, result.PackageName ?? "(unknown)", result.PackageVersion ?? "?",
                    Path.GetFileName(file));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
            {
                _logger.LogWarning(ex,
                    "FHIR package preloader: failed to load '{File}'; continuing.", file);
            }
        }

        _logger.LogInformation(
            "FHIR package preloader: registered {Total} conformance resource(s) from {Count} package(s).",
            total, files.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
