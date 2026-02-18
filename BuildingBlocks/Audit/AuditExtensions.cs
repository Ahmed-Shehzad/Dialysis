using BuildingBlocks.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Audit;

public static class AuditExtensions
{
    /// <summary>
    /// Registers <see cref="IAuditRecorder"/> with <see cref="LoggingAuditRecorder"/> implementation.
    /// </summary>
    public static IServiceCollection AddAuditRecorder(this IServiceCollection services) => services.AddScoped<IAuditRecorder, LoggingAuditRecorder>();
}
