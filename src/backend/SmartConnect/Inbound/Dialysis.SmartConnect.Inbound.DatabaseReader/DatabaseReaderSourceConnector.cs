using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Scheduling;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Inbound.DatabaseReader;

/// <summary>
/// Polls a database table via parameterized SELECT with watermark-based tracking.
/// Each row produces one <see cref="IntegrationMessage"/> with payload = JSON of all columns.
/// </summary>
public sealed class DatabaseReaderSourceConnector : ISourceConnector
{
    public const string KindValue = "database-reader";

    private readonly IDatabaseOutboundConnectionFactory _connectionFactory;

    public DatabaseReaderSourceConnector(IDatabaseOutboundConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public string Kind => KindValue;

    public async Task RunAsync(SourceConnectorContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        DatabaseReaderParameters parameters;
        try
        {
            parameters = DatabaseReaderParameters.Parse(context.Parameters);
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError(ex, "DatabaseReader '{Name}' has invalid parameters; not starting.", context.InstanceName);
            return;
        }

        ISchedule schedule;
        try
        {
            schedule = ScheduleFactory.FromParameters(context.Parameters, parameters.PollIntervalSeconds);
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogError(ex, "DatabaseReader '{Name}' has invalid schedule; not starting.", context.InstanceName);
            return;
        }

        context.Logger.LogInformation(
            "DatabaseReader '{Name}' polling (provider={Provider}, schedule={Schedule}, watermark={WatermarkColumn}).",
            context.InstanceName,
            parameters.Provider,
            schedule.GetType().Name,
            parameters.WatermarkColumn);

        var state = new WatermarkState();

        await PollOnceAsync(context, parameters, state, cancellationToken).ConfigureAwait(false);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                var next = schedule.NextOccurrence(now);
                if (next is null)
                {
                    context.Logger.LogInformation(
                        "DatabaseReader '{Name}' schedule has no future occurrence; stopping.",
                        context.InstanceName);
                    break;
                }

                var delay = next.Value - now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                await PollOnceAsync(context, parameters, state, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    private async Task PollOnceAsync(
        SourceConnectorContext context,
        DatabaseReaderParameters parameters,
        WatermarkState state,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var conn = await _connectionFactory.OpenAsync(
                parameters.Provider,
                parameters.ConnectionStringName,
                cancellationToken).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = parameters.PollSql;

            if (state.Value is not null)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = "@watermark";
                param.Value = state.Value;
                cmd.Parameters.Add(param);
            }
            else
            {
                // Bind a null watermark so SQL that references @watermark doesn't fail
                var param = cmd.CreateParameter();
                param.ParameterName = "@watermark";
                param.Value = DBNull.Value;
                cmd.Parameters.Add(param);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(reader.FieldCount);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                // Update watermark
                if (!string.IsNullOrWhiteSpace(parameters.WatermarkColumn) &&
                    row.TryGetValue(parameters.WatermarkColumn, out var wmVal) &&
                    wmVal is not null)
                {
                    state.Value = wmVal;
                }

                var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(row));
                var msg = context.MessageFactory.Create(
                    context.DefaultFlowId,
                    payload,
                    PayloadFormat.Json,
                    correlationId: null);

                await context.DispatchAsync(msg, cancellationToken).ConfigureAwait(false);
            }

            // Optional delete after read
            if (parameters.DeleteAfterRead && !string.IsNullOrWhiteSpace(parameters.DeleteSql) && state.Value is not null)
            {
                await using var delCmd = conn.CreateCommand();
                delCmd.CommandText = parameters.DeleteSql;
                var delParam = delCmd.CreateParameter();
                delParam.ParameterName = "@watermark";
                delParam.Value = state.Value;
                delCmd.Parameters.Add(delParam);
                await delCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "DatabaseReader '{Name}' poll error.", context.InstanceName);
        }
    }

    private sealed class WatermarkState
    {
        public object? Value { get; set; }
    }
}
