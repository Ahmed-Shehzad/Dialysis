using Azure.Messaging.ServiceBus.Administration;

using Microsoft.Extensions.Logging;

namespace Transponder.Transports.AzureServiceBus;

/// <summary>
/// Provisions Azure Service Bus topics and subscriptions at startup.
/// Ensures entities exist before receive endpoints start (works with real ASB and emulator).
/// </summary>
public sealed class AzureServiceBusTopologyProvisioner
{
    private readonly string _connectionString;
    private readonly ILogger<AzureServiceBusTopologyProvisioner> _logger;

    public AzureServiceBusTopologyProvisioner(
        string connectionString,
        ILogger<AzureServiceBusTopologyProvisioner> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ensures the topic and subscription exist. Creates them if missing.
    /// For emulator with UseDevelopmentEmulator: uses port 5300 for management operations.
    /// </summary>
    public async Task EnsureTopicAndSubscriptionAsync(
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);

        string adminConnectionString = GetAdminConnectionString(_connectionString);
        var client = new ServiceBusAdministrationClient(adminConnectionString);

        await EnsureTopicExistsAsync(client, topicName, cancellationToken).ConfigureAwait(false);
        await EnsureSubscriptionExistsAsync(client, topicName, subscriptionName, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "ASB topology provisioned. Topic={TopicName} Subscription={SubscriptionName}",
            topicName,
            subscriptionName);
    }

    private static string GetAdminConnectionString(string connectionString)
    {
        if (!connectionString.Contains("UseDevelopmentEmulator", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        // Emulator: management uses port 5300 (messaging uses 5672)
        if (connectionString.Contains(":5300", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        const string endpointPrefix = "Endpoint=sb://";
        int start = connectionString.IndexOf(endpointPrefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return connectionString;

        int hostStart = start + endpointPrefix.Length;
        int hostEnd = connectionString.IndexOf(';', hostStart);
        if (hostEnd < 0)
            hostEnd = connectionString.Length;

        string host = connectionString[hostStart..hostEnd].Trim();
        if (host.Contains(':', StringComparison.Ordinal))
            return connectionString;

        string before = connectionString[..hostEnd];
        string after = connectionString[hostEnd..];
        return before + ":5300" + after;
    }

    private async Task EnsureTopicExistsAsync(
        ServiceBusAdministrationClient client,
        string topicName,
        CancellationToken cancellationToken)
    {
        if (await client.TopicExistsAsync(topicName, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("ASB topic already exists. Topic={TopicName}", topicName);
            return;
        }

        _ = await client.CreateTopicAsync(topicName, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("ASB topic created. Topic={TopicName}", topicName);
    }

    private async Task EnsureSubscriptionExistsAsync(
        ServiceBusAdministrationClient client,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        if (await client.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("ASB subscription already exists. Topic={TopicName} Subscription={SubscriptionName}", topicName, subscriptionName);
            return;
        }

        _ = await client.CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("ASB subscription created. Topic={TopicName} Subscription={SubscriptionName}", topicName, subscriptionName);
    }
}
