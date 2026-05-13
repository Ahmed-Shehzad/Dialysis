using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Composition;

public static class HospitalInformationSystemExtensions
{
    /// <summary>
    /// Registers HIS facility-operations bounded contexts (Operations, DataServices, Integration, RaCapabilities),
    /// EF persistence against PostgreSQL, CQRS via <see cref="HisCqrsServiceCollectionExtensions.AddHisCqrs"/>,
    /// authorization pipeline behaviors, Transponder bus, and optional outbox relay.
    /// Clinical concerns (Registration/Scheduling/PatientChart/Portal/ClinicalNotes/Billing) have moved to the EHR module.
    /// </summary>
    public static IServiceCollection AddHospitalInformationSystem(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxRelay = false,
        Action<IServiceCollection>? configureTransponderTransport = null)
    {
        _ = configuration;

        services.AddHisPersistence(configurePersistence);

        services.AddTransponder(_ => { });
        configureTransponderTransport?.Invoke(services);

        services.AddHisCqrs();

        if (enableOutboxRelay)
            services.AddTransponderOutboxRelay<HisDbContext>();

        return services;
    }

    /// <summary>Applies RabbitMQ as <see cref="ITransponderBus"/> when <paramref name="rabbitConnectionUri"/> is non-empty.</summary>
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
            });
    }
}
