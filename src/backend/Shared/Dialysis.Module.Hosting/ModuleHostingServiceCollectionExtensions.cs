using System.Reflection;
using Dialysis.BuildingBlocks.DistributedCache.Valkey;
using Dialysis.BuildingBlocks.Transponder.Hosting;
using Dialysis.CQRS;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Module.Hosting.Authorization;
using Dialysis.Module.Hosting.Middleware;
using Dialysis.Module.Hosting.OpenApi;
using Dialysis.Module.Hosting.RateLimiting;
using Dialysis.Module.Hosting.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Dialysis.Module.Hosting;

public static class ModuleHostingServiceCollectionExtensions
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Bundles the cross-cutting plumbing every modular monolith host needs:
        /// CQRS + validation, JWT auth, current-user resolution, authorization pipeline,
        /// audit interceptor wiring, telemetry, problem details, OpenAPI, health, correlation-id.
        /// The module's persistence layer (DbContext + migrations) and Transponder transports
        /// are wired separately in the module's composition root.
        /// </summary>
        public WebApplicationBuilder AddModuleHost<TPermissionCatalog>(
            ModuleHostingOptions options)
            where TPermissionCatalog : class, IModulePermissionCatalog, new()
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.ModuleSlug);

            var authSection = options.AuthenticationConfigurationSection
                ?? $"{options.ModuleSlug}:Authentication";

            builder.Services.AddSingleton<IModulePermissionCatalog>(new TPermissionCatalog());
            builder.Services.Configure<ModuleAuthenticationOptions>(builder.Configuration.GetSection(authSection));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
            builder.Services.AddScoped<IModuleAuthorizationService, ModuleAuthorizationService>();
            builder.Services.TryAddSingleton(TimeProvider.System);
            builder.Services.TryAddScoped<IAuditActorAccessor, CurrentUserAuditActorAccessor>();
            builder.Services.TryAddScoped<AuditSaveChangesInterceptor>();

            var auth = builder.Configuration.GetSection(authSection).Get<ModuleAuthenticationOptions>() ?? new ModuleAuthenticationOptions();

            if (auth.RequireAuthorityWhenNotDevelopment
                && !builder.Environment.IsDevelopment()
                && string.IsNullOrWhiteSpace(auth.Authority))
            {
                throw new InvalidOperationException(
                    $"{authSection}:Authority must be set when RequireAuthorityWhenNotDevelopment is true and ASPNETCORE_ENVIRONMENT is not Development.");
            }

            if (!string.IsNullOrWhiteSpace(auth.Authority))
            {
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(o =>
                    {
                        o.Authority = auth.Authority;
                        if (!string.IsNullOrWhiteSpace(auth.Audience))
                            o.Audience = auth.Audience;
                        if (Uri.TryCreate(auth.Authority, UriKind.Absolute, out var issuer)
                            && string.Equals(issuer.Scheme, "http", StringComparison.OrdinalIgnoreCase)
                            && builder.Environment.IsDevelopment())
                            o.RequireHttpsMetadata = false;
                        // SignalR WebSocket upgrades can't carry custom headers from the browser,
                        // so the JS client appends ?access_token=<jwt>. Honor it for /hubs/* paths.
                        o.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = ctx =>
                            {
                                var accessToken = ctx.Request.Query["access_token"];
                                var path = ctx.HttpContext.Request.Path;
                                if (!string.IsNullOrEmpty(accessToken)
                                    && path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                                {
                                    ctx.Token = accessToken;
                                }
                                return Task.CompletedTask;
                            },
                        };
                    });
                builder.Services.AddAuthorization();
            }

            var assemblies = options.HandlerAssemblies?.ToArray() ?? [Assembly.GetCallingAssembly()];
            builder.Services.AddCqrs(c => c.AddFromAssemblies(assemblies));

            var telemetrySection = builder.Configuration.GetSection($"{options.ModuleSlug}:Telemetry");
            builder.Services.AddModuleTelemetry(options.ModuleSlug, telemetry =>
            {
                var configured = telemetrySection.Get<ModuleTelemetryOptions>();
                if (configured is not null)
                {
                    if (!string.IsNullOrWhiteSpace(configured.ServiceName))
                        telemetry.ServiceName = configured.ServiceName;
                    if (!string.IsNullOrWhiteSpace(configured.ServiceVersion))
                        telemetry.ServiceVersion = configured.ServiceVersion;
                    if (!string.IsNullOrWhiteSpace(configured.OtlpEndpoint))
                        telemetry.OtlpEndpoint = configured.OtlpEndpoint;
                    telemetry.AdditionalActivitySources.AddRange(configured.AdditionalActivitySources);
                    telemetry.AdditionalMeters.AddRange(configured.AdditionalMeters);
                }
                options.ConfigureTelemetry?.Invoke(telemetry);
            });

            builder.Services.AddExceptionHandler<ModulePermissionDeniedExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.AddModuleApiVersioning();

            // /health/ready must verify the module's real dependencies, not just process-up:
            // the module Postgres (ConnectionStrings:<slug>, matched case-insensitively, e.g.
            // "His") and the Transponder RabbitMQ broker when one is configured. Each probe is
            // skipped when its configuration is absent (in-memory tests, identity-only smoke).
            var healthChecks = builder.Services.AddHealthChecks();
            var moduleDbConnectionString = builder.Configuration.GetConnectionString(options.ModuleSlug);
            if (!string.IsNullOrWhiteSpace(moduleDbConnectionString))
                healthChecks.AddNpgSql(moduleDbConnectionString, name: $"{options.ModuleSlug}-postgres");

            var rabbitConnectionUri = builder.Configuration[$"{options.ModuleSlug}:Transponder:RabbitMq:ConnectionUri"];
            if (!string.IsNullOrWhiteSpace(rabbitConnectionUri))
            {
                healthChecks.AddRabbitMQ(
                    async _ =>
                    {
                        var factory = new ConnectionFactory { Uri = new Uri(rabbitConnectionUri) };
                        return await factory.CreateConnectionAsync().ConfigureAwait(false);
                    },
                    name: $"{options.ModuleSlug}-rabbitmq");
            }

            var valkeySection = builder.Configuration.GetSection($"{options.ModuleSlug}:DistributedCache:Valkey");
            if (!string.IsNullOrWhiteSpace(valkeySection["ConnectionString"]))
            {
                if (string.IsNullOrWhiteSpace(valkeySection["InstanceName"]))
                {
                    valkeySection["InstanceName"] = options.ModuleSlug;
                }
                builder.Services.AddValkeyDistributedCache(valkeySection);
            }

            // Built-in ASP.NET 7+ rate limiting — first backpressure surface in front of the
            // module API, partitioned per authenticated subject (sub claim) or IP fallback.
            // See docs/operations/load-and-capacity.md for the per-tier shedding story.
            builder.Services.AddModuleRateLimiting(builder.Configuration.GetSection(options.ModuleSlug));

            // Transponder Hangfire scheduler — PostgreSQL-backed background scheduling. The AppHost
            // injects ConnectionStrings:Hangfire (the host's module database); it is absent in tests,
            // where Hangfire is skipped.
            var hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire");
            if (!string.IsNullOrWhiteSpace(hangfireConnectionString))
                builder.Services.AddTransponderHangfire(hangfireConnectionString);

            return builder;
        }
    }

    extension(IServiceProvider services)
    {
        /// <summary>
        /// Convenience accessor for the configured <see cref="ModuleAuthenticationOptions"/>
        /// (e.g. to decide whether to wire auth middleware in the pipeline).
        /// </summary>
        public ModuleAuthenticationOptions ResolveModuleAuthenticationOptions() =>
            services.GetRequiredService<IOptions<ModuleAuthenticationOptions>>().Value;
    }
}
