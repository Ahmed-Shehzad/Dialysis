using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dialysis.HealthChecks;

/// <summary>
/// Health check extensions for Dialysis services.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds common health checks (DB, Redis) based on configuration.
    /// </summary>
    public static IHealthChecksBuilder AddDialysisHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddHealthChecks();

        AddPostgresChecks(builder, configuration);
        AddRedisCheck(builder, configuration);

        return builder;
    }

    /// <summary>
    /// Adds Azure Service Bus health check when connection string is configured.
    /// </summary>
    public static IHealthChecksBuilder AddServiceBusHealthCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? configuration.GetConnectionString("ServiceBus");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            builder.AddAzureServiceBusTopic(
                connectionString,
                topicName: "observation-created",
                name: "servicebus",
                failureStatus: HealthStatus.Degraded,
                tags: ["messaging", "ready"]);
        }

        return builder;
    }

    /// <summary>
    /// Maps /health endpoint. Use with WebApplication: app.MapHealthChecks("/health").
    /// Health checks are registered via AddDialysisHealthChecks.
    /// </summary>
    public static T MapDialysisHealthChecks<T>(this T app) where T : IApplicationBuilder
    {
        app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true
        });
        return app;
    }

    private static void AddPostgresChecks(IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var subscriptionsConn = configuration.GetConnectionString("Subscriptions")
            ?? configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(subscriptionsConn))
        {
            builder.AddNpgSql(
                subscriptionsConn,
                name: "postgres-subscriptions",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "ready"]);
        }

        var template = configuration["Tenancy:ConnectionStringTemplate"];
        if (!string.IsNullOrWhiteSpace(template))
        {
            var conn = template.Replace("{TenantId}", "default", StringComparison.OrdinalIgnoreCase);
            builder.AddNpgSql(
                conn,
                name: "postgres-tenancy",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "ready"]);
        }

        var pgConn = configuration.GetConnectionString("PostgreSQL");
        if (!string.IsNullOrWhiteSpace(pgConn) && !string.Equals(pgConn, subscriptionsConn, StringComparison.Ordinal))
        {
            builder.AddNpgSql(
                pgConn,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "ready"]);
        }
    }

    private static void AddRedisCheck(IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            builder.AddRedis(
                redisConn,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "ready"]);
        }
    }
}
