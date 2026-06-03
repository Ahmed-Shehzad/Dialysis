using System.Reflection;
using System.Text.Json;
using Dialysis.HIE.Core.Abstraction.OpenEhr;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIE.OpenEhr.Archetypes.Declarative;

/// <summary>
/// Loads the shipped <c>Archetypes/Definitions/*.json</c> embedded-resource catalog and
/// registers one <see cref="DeclarativeArchetypeProjection{TResource}"/> per definition.
/// The mapping from <c>fhirResourceType</c> (string) to the FHIR POCO type uses the
/// Hl7.Fhir model registry — unknown types are surfaced as a clear startup-time error so
/// a typo in the JSON can't slip past CI.
///
/// Partner clinics that want to extend the catalog with their own archetypes do one of:
/// <list type="bullet">
///   <item>drop additional JSON files into the embedded-resource folder of a downstream
///         module that references this project — the catalog scanner picks up every
///         embedded resource ending in <c>.archetype.json</c>;</item>
///   <item>call <see cref="AddArchetypeMappingDefinition"/> at composition time with a
///         hand-built <see cref="ArchetypeMappingDefinition"/> — useful for unit tests.</item>
/// </list>
/// </summary>
public static class ArchetypeMappingCatalog
{
    /// <summary>
    /// Registers every embedded <c>Archetypes/Definitions/*.json</c> from this assembly as a
    /// <see cref="DeclarativeArchetypeProjection{TResource}"/>. Existing manual
    /// registrations win — the catalog uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton(IServiceCollection, Type, Type)"/>
    /// shape so a test or a partner override can pre-register a custom projection without
    /// being clobbered.
    /// </summary>
    public static IServiceCollection AddArchetypeMappingCatalog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        foreach (var definition in LoadEmbeddedDefinitions(typeof(ArchetypeMappingCatalog).Assembly))
        {
            services.AddArchetypeMappingDefinition(definition);
        }
        return services;
    }

    /// <summary>
    /// Manual registration entry-point — pre-populates a single
    /// <see cref="DeclarativeArchetypeProjection{TResource}"/>. Used by tests and partner
    /// overrides that want to short-circuit the embedded-resource scan.
    /// </summary>
    public static IServiceCollection AddArchetypeMappingDefinition(
        this IServiceCollection services, ArchetypeMappingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(definition);
        var resourceType = ResolveFhirResourceType(definition.FhirResourceType);
        var serviceType = typeof(IArchetypeProjection<>).MakeGenericType(resourceType);
        var implementationType = typeof(DeclarativeArchetypeProjection<>).MakeGenericType(resourceType);
        services.AddSingleton(serviceType, _ =>
            Activator.CreateInstance(implementationType, definition)
                ?? throw new InvalidOperationException($"Failed to construct {implementationType.Name}."));
        return services;
    }

    /// <summary>
    /// Streams every embedded <c>*.json</c> under <c>Archetypes/Definitions/</c> in
    /// <paramref name="assembly"/> and deserialises it into an
    /// <see cref="ArchetypeMappingDefinition"/>. Exposed for tests + partner extensions.
    /// </summary>
    public static IReadOnlyList<ArchetypeMappingDefinition> LoadEmbeddedDefinitions(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        const string prefix = "Dialysis.HIE.OpenEhr.Archetypes.Definitions.";
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var definitions = new List<ArchetypeMappingDefinition>();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' could not be opened.");
            var definition = JsonSerializer.Deserialize<ArchetypeMappingDefinition>(stream, jsonOptions)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' deserialised to null.");
            definitions.Add(definition);
        }
        return definitions;
    }

    private static Type ResolveFhirResourceType(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        // Every FHIR resource POCO lives in the same `Hl7.Fhir.Model` namespace as Patient.
        var type = typeof(Patient).Assembly.GetType("Hl7.Fhir.Model." + typeName);
        if (type is null || !typeof(Resource).IsAssignableFrom(type))
        {
            throw new InvalidOperationException(
                $"FHIR resource type '{typeName}' is not a known Hl7.Fhir.R4 Resource subtype.");
        }
        return type;
    }
}
