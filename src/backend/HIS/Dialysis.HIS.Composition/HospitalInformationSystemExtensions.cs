using Dialysis.HIS.DataServices;
using Dialysis.HIS.Integration;
using Dialysis.HIS.Medication;
using Dialysis.HIS.Operations;
using Dialysis.HIS.PatientAccess;
using Dialysis.HIS.PatientFlow;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.Scheduling;
using Dialysis.HIS.Security;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Composition;

public static class HospitalInformationSystemExtensions
{
    /// <summary>
    /// Registers all HIS bounded contexts, EF persistence (in-memory by default), CQRS (<see cref="HisCqrsServiceCollectionExtensions.AddHisCqrs"/>),
    /// authorization pipeline for permissioned requests, Transponder bus, and integration stubs.
    /// </summary>
    /// <param name="configurePersistence">SQL Server (or other) provider when a connection string is supplied by the host.</param>
    /// <param name="enableOutboxDispatcher">When true, registers <c>AddTransponderOutboxRelay&lt;HisDbContext&gt;</c> so queued outbox rows publish to <see cref="ITransponderBus"/>.</param>
    /// <param name="configureTransponderTransport">Optional: e.g. <see cref="RabbitMqTransponderServiceCollectionExtensions.AddTransponderRabbitMq"/> when broker URI is configured.</param>
    public static IServiceCollection AddHospitalInformationSystem(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxDispatcher = false,
        Action<IServiceCollection>? configureTransponderTransport = null)
    {
        services.AddHisSecurityCore(configuration);
        services.Configure<PatientPortalOptions>(configuration.GetSection("His:PatientAccess"));
        services.AddHisPersistence(configurePersistence);
        services.AddHisIntegrationStubs(configuration);

        services.AddTransponder(t => t.AddHisIntegrationConsumers());
        configureTransponderTransport?.Invoke(services);

        services.AddHisCqrs();

        if (enableOutboxDispatcher)
            services.AddTransponderOutboxRelay<HisDbContext>();

        return services;
    }

    /// <summary>
    /// Applies RabbitMQ as <see cref="ITransponderBus"/> when <paramref name="rabbitConnectionUri"/> is non-empty; configures HIS integration subscriptions.
    /// </summary>
    public static void AddHisTransponderRabbitMqIfConfigured(
        this IServiceCollection services,
        string? rabbitConnectionUri,
        string? queueName = null,
        string? exchangeName = null)
    {
        if (string.IsNullOrWhiteSpace(rabbitConnectionUri))
            return;

        services.AddTransponderRabbitMq(
            o =>
            {
                o.ConnectionUri = rabbitConnectionUri;
                if (!string.IsNullOrWhiteSpace(queueName))
                    o.QueueName = queueName;
                if (!string.IsNullOrWhiteSpace(exchangeName))
                    o.ExchangeName = exchangeName;
            },
            b => b.AddHisIntegrationMessageSubscriptions());
    }
}
