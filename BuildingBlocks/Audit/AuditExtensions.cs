using BuildingBlocks.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Audit;

public static class AuditExtensions
{
    /// <summary>
    /// Registers <see cref="IAuditRecorder"/> with <see cref="LoggingAuditRecorder"/> implementation.
    /// </summary>
    public static IServiceCollection AddAuditRecorder(this IServiceCollection services) => services.AddScoped<IAuditRecorder, LoggingAuditRecorder>();

    /// <summary>
    /// Registers <see cref="IAuditRecorder"/> with <see cref="FhirAuditRecorder"/>, which stores events for FHIR AuditEvent retrieval.
    /// Also registers <see cref="IAuditEventStore"/> with in-memory implementation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="storeCapacity">Maximum number of events to retain in memory (default 1000).</param>
    public static IServiceCollection AddFhirAuditRecorder(this IServiceCollection services, int storeCapacity = 1000)
    {
        _ = services.AddSingleton<IAuditEventStore>(_ => new InMemoryAuditEventStore(storeCapacity));
        _ = services.AddScoped<IAuditRecorder, FhirAuditRecorder>();
        return services;
    }
}
