using System.Text.Json;
using BuildingBlocks.Serialization;
using Dialysis.Contracts.Events;
using Dialysis.Contracts.Ids;
using Dialysis.Contracts.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Transponder;
using Transponder.Abstractions;
using Transponder.Transports;
using Transponder.Transports.Abstractions;
using Transponder.Transports.AzureServiceBus;
using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Dialysis.Messaging;

/// <summary>
/// Extension methods to register Transponder with Dialysis Azure Service Bus topology.
/// Uses BuildingBlocks types for integration event handling and serialization.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Transponder with Azure Service Bus when connection string is configured;
    /// otherwise registers a no-op publish endpoint for local development without Service Bus.
    /// Configures the message serializer with BuildingBlocks.EnumerationJsonConverter for event DTOs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="address">The bus address (e.g. sb://mybus.servicebus.windows.net/gateway).</param>
    /// <param name="connectionString">The Azure Service Bus connection string (null/empty = no-op).</param>
    /// <param name="configure">Optional configuration for sagas, outbox, or other Transponder options.</param>
    public static IServiceCollection AddDialysisTransponder(
        this IServiceCollection services,
        Uri address,
        string? connectionString,
        Action<TransponderBusOptions>? configure = null)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            jsonOptions.Converters.Add(new EnumerationJsonConverterFactory());
            jsonOptions.Converters.Add(new PatientIdJsonConverter());
            jsonOptions.Converters.Add(new EncounterIdJsonConverter());
            jsonOptions.Converters.Add(new ObservationIdJsonConverter());

            services.TryAddSingleton<IMessageSerializer>(_ => new JsonMessageSerializer(jsonOptions));

            services.AddTransponder(address, options =>
            {
                options.TransportBuilder.UseAzureServiceBus(sp =>
                {
                    var topicMappings = new Dictionary<Type, string>
                    {
                        [typeof(ObservationCreated)] = "observation-created",
                        [typeof(HypotensionRiskRaised)] = "hypotension-risk-raised",
                        [typeof(ResourceWrittenEvent)] = "resource-written",
                        [typeof(Hl7Ingested)] = "hl7-ingest",
                        [typeof(DeliverReportSagaMessage)] = "report-delivery-saga"
                    };
                    var topology = new MappingAzureServiceBusTopology(topicMappings);
                    return new AzureServiceBusHostSettings(
                        address,
                        topology,
                        AzureServiceBusTransportType.AmqpTcp,
                        connectionString);
                });
                configure?.Invoke(options);
            });
            services.AddHostedService<TransponderHostedService>();
        }
        else
        {
            services.TryAddSingleton<IPublishEndpoint, NoOpPublishEndpoint>();
        }

        return services;
    }

    /// <summary>
    /// Adds an integration event consumer that deserializes messages and dispatches to <see cref="BuildingBlocks.Abstractions.IIntegrationEventHandler{T}"/>.
    /// Call after <see cref="AddDialysisTransponder"/> when connection string is configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="inputAddress">The subscription address (e.g. sb://dialysis/observation-created/subscriptions/prediction-subscription).</param>
    public static IServiceCollection AddIntegrationEventConsumer<TMessage>(
        this IServiceCollection services,
        Uri inputAddress)
        where TMessage : class, BuildingBlocks.Abstractions.IIntegrationEvent
    {
        services.Configure<IntegrationEventConsumerOptions<TMessage>>(o => o.InputAddress = inputAddress);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IReceiveEndpoint, IntegrationEventConsumerEndpoint<TMessage>>());
        return services;
    }

    /// <summary>
    /// Adds a Transponder message consumer that deserializes messages and dispatches to <see cref="IMessageHandler{TMessage}"/>.
    /// Use for any <see cref="Transponder.Abstractions.IMessage"/> type (e.g. <see cref="Dialysis.Contracts.Messages.Hl7Ingested"/>).
    /// Call after <see cref="AddDialysisTransponder"/> when connection string is configured.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="inputAddress">The subscription address (e.g. sb://dialysis/hl7-ingest/subscriptions/his-subscription).</param>
    public static IServiceCollection AddMessageConsumer<TMessage>(
        this IServiceCollection services,
        Uri inputAddress)
        where TMessage : class, Transponder.Abstractions.IMessage
    {
        services.Configure<MessageConsumerOptions<TMessage>>(o => o.InputAddress = inputAddress);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IReceiveEndpoint, MessageConsumerEndpoint<TMessage>>());
        return services;
    }
}
