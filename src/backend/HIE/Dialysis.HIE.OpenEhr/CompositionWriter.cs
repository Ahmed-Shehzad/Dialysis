using Dialysis.HIE.Core.Abstraction.OpenEhr;
using Dialysis.HIE.OpenEhr.Ports;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Composition = Dialysis.HIE.OpenEhr.Domain.Composition;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.OpenEhr;

/// <summary>
/// Records an archetype-shaped projection of a FHIR resource for the longitudinal openEHR store.
/// Looks up the right <see cref="IArchetypeProjection{TResource}"/>; no-ops when none is registered.
/// </summary>
public sealed class CompositionWriter(
    IServiceProvider services,
    ICompositionStore store,
    TimeProvider timeProvider,
    ILogger<CompositionWriter> logger)
{
    public async Task WriteAsync<TResource>(Guid patientId, TResource resource, string composer, CancellationToken cancellationToken = default)
        where TResource : Resource
    {
        var projection = (IArchetypeProjection<TResource>?)services.GetService(typeof(IArchetypeProjection<TResource>));
        if (projection is null) return;

        await WriteWithProjectionAsync(patientId, resource, projection, composer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resource-typed entry point invoked from the outbound dispatcher when only a runtime <see cref="Resource"/>
    /// is available. Pattern-matches the supported types and forwards to the strongly-typed overload.
    /// </summary>
    public Task WriteResourceAsync(Guid patientId, Resource resource, string composer, CancellationToken cancellationToken = default)
    {
        return resource switch
        {
            Patient p => WriteAsync(patientId, p, composer, cancellationToken),
            Procedure pr => WriteAsync(patientId, pr, composer, cancellationToken),
            Observation o => WriteAsync(patientId, o, composer, cancellationToken),
            _ => SkipAsync(resource),
        };

        Task SkipAsync(Resource r)
        {
            logger.LogDebug("No archetype projection registered for {ResourceType}; skipping composition write", r.TypeName);
            return Task.CompletedTask;
        }
    }

    private async Task WriteWithProjectionAsync<TResource>(
        Guid patientId,
        TResource resource,
        IArchetypeProjection<TResource> projection,
        string composer,
        CancellationToken cancellationToken)
        where TResource : Resource
    {
        var payload = projection.Project(resource);
        var version = await store.NextVersionAsync(patientId, projection.ArchetypeId, cancellationToken).ConfigureAwait(false);
        var composition = new Composition(
            patientId,
            projection.ArchetypeId,
            version,
            composer,
            timeProvider.GetUtcNow().UtcDateTime,
            payload);
        await store.AddAsync(composition, cancellationToken).ConfigureAwait(false);
    }
}
