using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.BuildingBlocks.DataProtection.Retention;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.DataProtection.Ropa;

/// <summary>Default in-memory generator. Reads from DI-injected per-module registries.</summary>
public sealed class RopaGenerator : IRopaGenerator
{
    private readonly IReadOnlyList<ILawfulBasisRegistry> _moduleRegistries;
    private readonly IRetentionSchedule _retention;
    private readonly RopaOptions _options;
    private readonly TimeProvider _clock;

    public RopaGenerator(
        IEnumerable<ILawfulBasisRegistry> moduleRegistries,
        IRetentionSchedule retention,
        IOptions<RopaOptions> options,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(moduleRegistries);
        ArgumentNullException.ThrowIfNull(retention);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        _moduleRegistries = [.. moduleRegistries];
        _retention = retention;
        _options = options.Value;
        _clock = clock;
    }

    public RopaDocument Generate()
    {
        var modules = _moduleRegistries
            .Select(r => new RopaModuleSection(r.ModuleSlug, r.Activities))
            .OrderBy(m => m.ModuleSlug, StringComparer.Ordinal)
            .ToArray();

        return new RopaDocument(
            ControllerName: _options.ControllerName,
            ControllerContact: _options.ControllerContact,
            GeneratedAtUtc: _clock.GetUtcNow(),
            Modules: modules,
            Retention: _retention.All());
    }
}

/// <summary>
/// Configurable controller-of-record metadata for the RoPA document. GDPR Art. 30 requires
/// the controller's name + contact and (where applicable) the DPO.
/// </summary>
public sealed class RopaOptions
{
    public string ControllerName { get; set; } = "Dialysis Platform Operator";

    public string ControllerContact { get; set; } = "dpo@example.com";

    public string? DpoName { get; set; }

    public string? DpoContact { get; set; }
}
