using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.BuildingBlocks.DataProtection.Restriction;
using Dialysis.BuildingBlocks.DataProtection.Retention;
using Dialysis.BuildingBlocks.DataProtection.Ropa;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.DataProtection;

/// <summary>
/// Single composition entry-point that wires GDPR / BDSG / PDSG scaffolding into a module's
/// host. Mirrors the existing <c>services.AddHipaaCompliance("module-slug")</c> shape.
///
/// A module's <c>Program.cs</c> looks like:
/// <code>
/// builder.Services.AddEuDataProtection("pdms", registry =>
/// {
///     registry.RegisterActivity(
///         "pdms.medications.administer",
///         LawfulBasis.HealthcareProvision,
///         DataCategory.ClinicalHealth | DataCategory.Medication,
///         purpose: "Record what was administered at the chair.",
///         retentionKey: "clinical.record",
///         recipientCategories: ["EHR (MedicationStatement update)"]);
/// });
/// </code>
/// </summary>
public static class DataProtectionServiceCollectionExtensions
{
    public static IServiceCollection AddEuDataProtection(
        this IServiceCollection services,
        string moduleSlug,
        Action<LawfulBasisRegistryBuilder>? configureRegistry = null,
        Action<RetentionScheduleBuilder>? configureRetention = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleSlug);

        // Per-module registry — each module's composition root contributes its own. The
        // RoPA generator collects every registry registered in the container.
        var registryBuilder = new LawfulBasisRegistryBuilder(moduleSlug);
        configureRegistry?.Invoke(registryBuilder);
        services.AddSingleton<ILawfulBasisRegistry>(registryBuilder.Build());

        // Retention schedule — platform defaults always; modules can add their own keys.
        services.TryAddSingleton<IRetentionSchedule>(_ =>
        {
            var builder = new RetentionScheduleBuilder();
            builder.AddRange(RetentionSchedule.PlatformDefaults());
            configureRetention?.Invoke(builder);
            return builder.Build();
        });

        // Singletons that consume the registries above. Modules adding the package multiple
        // times safely re-register — `TryAddSingleton` keeps the first wins semantics.
        services.TryAddSingleton<IRopaGenerator, RopaGenerator>();
        services.AddOptions<RopaOptions>();
        services.TryAddSingleton(TimeProvider.System);

        // GDPR Art. 15 / 17 / 18 / 20 orchestrator. Walks every registered
        // IModuleDataExtractor for export, every registered IPatientEraser for approval,
        // and persists the audit-trail row through IErasureRequestStore. HIE registers the
        // EF-backed store (Scoped) before this call — `TryAdd` keeps it; non-HIE hosts fall
        // back to the in-memory baseline below so DI validation succeeds without an erasure
        // persistence story of their own.
        services.TryAddSingleton<IErasureRequestStore, InMemoryErasureRequestStore>();
        // Art. 18 restriction store — same baseline/EF split as erasure above.
        services.TryAddSingleton<IRestrictionRequestStore, InMemoryRestrictionRequestStore>();
        services.TryAddScoped<IDataSubjectRightsService, DefaultDataSubjectRightsService>();

        return services;
    }
}

/// <summary>Fluent builder for a module's lawful-basis registry.</summary>
public sealed class LawfulBasisRegistryBuilder
{
    private readonly string _moduleSlug;
    private readonly List<ProcessingActivity> _activities = [];

    public LawfulBasisRegistryBuilder(string moduleSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleSlug);
        _moduleSlug = moduleSlug;
    }

    public LawfulBasisRegistryBuilder RegisterActivity(
        string activityName,
        LawfulBasis basis,
        DataCategory categories,
        string purpose,
        string? retentionKey = null,
        IEnumerable<string>? recipientCategories = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        _activities.Add(new ProcessingActivity(
            ActivityName: activityName,
            Basis: basis,
            Categories: categories,
            Purpose: purpose,
            RetentionKey: retentionKey,
            RecipientCategories: recipientCategories?.ToArray() ?? []));
        return this;
    }

    public ILawfulBasisRegistry Build() => new LawfulBasisRegistry(_moduleSlug, _activities);
}

/// <summary>Fluent builder for the platform-wide retention schedule.</summary>
public sealed class RetentionScheduleBuilder
{
    private readonly List<RetentionWindowRegistration> _registrations = [];

    public RetentionScheduleBuilder Add(string key, RetentionWindow window, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        _registrations.Add(new RetentionWindowRegistration(key, window, description));
        return this;
    }

    public RetentionScheduleBuilder AddRange(IEnumerable<RetentionWindowRegistration> regs)
    {
        ArgumentNullException.ThrowIfNull(regs);
        _registrations.AddRange(regs);
        return this;
    }

    public IRetentionSchedule Build() => new RetentionSchedule(_registrations);
}
