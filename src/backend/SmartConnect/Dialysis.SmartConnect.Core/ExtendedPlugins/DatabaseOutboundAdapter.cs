using System.Data;
using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Outbound adapter that executes a parameterized SQL command per message. Connection strings are
/// resolved by name via <see cref="IDatabaseOutboundConnectionFactory"/>; SQL bodies must use
/// provider-style placeholders (e.g. <c>@p0</c>, <c>@name</c>) bound through
/// <see cref="DatabaseParameterBinding"/> entries to prevent SQL injection.
/// </summary>
public sealed class DatabaseOutboundAdapter : IOutboundAdapter
{
    private readonly IDatabaseOutboundConnectionFactory _connectionFactory;
    /// <summary>
    /// Outbound adapter that executes a parameterized SQL command per message. Connection strings are
    /// resolved by name via <see cref="IDatabaseOutboundConnectionFactory"/>; SQL bodies must use
    /// provider-style placeholders (e.g. <c>@p0</c>, <c>@name</c>) bound through
    /// <see cref="DatabaseParameterBinding"/> entries to prevent SQL injection.
    /// </summary>
    public DatabaseOutboundAdapter(IDatabaseOutboundConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;
    public const string KindValue = "database";

    public string Kind => KindValue;

    public async Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(HttpOutboundAdapter.ParametersMetadataKey, out var json) ||
            string.IsNullOrWhiteSpace(json))
        {
            return new OutboundSendResult(
                false,
                "Database outbound requires parameters JSON with Provider, ConnectionStringName and Sql.");
        }

        DatabaseOutboundParameters? opts;
        try
        {
            opts = JsonSerializer.Deserialize<DatabaseOutboundParameters>(json);
        }
        catch (JsonException ex)
        {
            return new OutboundSendResult(false, $"Database outbound parameters JSON is invalid: {ex.Message}");
        }

        if (opts is null
            || string.IsNullOrWhiteSpace(opts.ConnectionStringName)
            || string.IsNullOrWhiteSpace(opts.Sql))
        {
            return new OutboundSendResult(
                false,
                "Database outbound parameters must include ConnectionStringName and Sql.");
        }

        try
        {
            await using var connection = await _connectionFactory
                .OpenAsync(opts.Provider, opts.ConnectionStringName!, cancellationToken)
                .ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = opts.Sql!;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = opts.CommandTimeoutSeconds <= 0 ? 30 : opts.CommandTimeoutSeconds;

            foreach (var binding in opts.Parameters)
            {
                if (string.IsNullOrWhiteSpace(binding.Name))
                {
                    return new OutboundSendResult(false, "Database outbound parameter is missing 'Name'.");
                }

                var parameter = command.CreateParameter();
                parameter.ParameterName = binding.Name;
                parameter.Value = ResolveValue(message, binding);
                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return new OutboundSendResult(true, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new OutboundSendResult(false, ex.Message);
        }
    }

    private static object ResolveValue(IntegrationMessage message, DatabaseParameterBinding binding) =>
        binding.Source switch
        {
            DatabaseParameterSource.Payload => message.Payload.ToArray(),
            DatabaseParameterSource.PayloadAsString => Encoding.UTF8.GetString(message.Payload.Span),
            DatabaseParameterSource.CorrelationId => message.CorrelationId,
            DatabaseParameterSource.FlowId => message.FlowId,
            DatabaseParameterSource.MessageId => message.Id,
            DatabaseParameterSource.ReceivedAtUtc => message.ReceivedAtUtc.UtcDateTime,
            DatabaseParameterSource.Literal => binding.Path ?? string.Empty,
            DatabaseParameterSource.Metadata when !string.IsNullOrWhiteSpace(binding.Path) =>
                message.Metadata.TryGetValue(binding.Path!, out var value)
                    ? value
                    : DBNull.Value,
            DatabaseParameterSource.Metadata => DBNull.Value,
            _ => DBNull.Value,
        };
}
