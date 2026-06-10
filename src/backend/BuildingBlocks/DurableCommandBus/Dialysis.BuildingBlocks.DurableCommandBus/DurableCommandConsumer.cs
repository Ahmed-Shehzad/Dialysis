using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Per-module <see cref="IConsumer{DurableCommandEnvelope}"/> that owns the same-transaction
/// idempotency guarantee. For each delivery:
/// <list type="number">
///   <item>Opens an explicit EF transaction on the module's <typeparamref name="TContext"/>.</item>
///   <item>Calls <see cref="ICommandLedger.TryClaimAsync"/>. <c>AlreadyApplied</c>/<c>AlreadyFailed</c> → ack (idempotent redelivery). Other states proceed.</item>
///   <item>Looks up the registration in <see cref="IDurableCommandCatalog"/>; unknown type → throw → broker dead-letters.</item>
///   <item>Deserializes the payload and dispatches through the typed closure on the registration. The handler runs as normal — including its own internal <c>SaveChangesAsync</c>, which is folded into the open transaction.</item>
///   <item><see cref="ICommandLedger.MarkAppliedAsync"/> + <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> + <c>tx.CommitAsync</c> commit handler change + ledger row together.</item>
/// </list>
/// If anything throws between the transaction open and commit, EF rolls back automatically →
/// no ledger row, no aggregate change. The broker redelivers, and the next consumer sees a
/// clean state.
/// </summary>
public sealed class DurableCommandConsumer<TContext> : IConsumer<DurableCommandEnvelope>
    where TContext : DbContext
{
    private readonly IDurableCommandCatalog _catalog;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DurableCommandBusOptions> _options;
    private readonly DurableCommandMetrics _metrics;
    private readonly TimeProvider _clock;
    private readonly ILogger<DurableCommandConsumer<TContext>> _logger;
    private readonly string _consumerInstanceId;

    public DurableCommandConsumer(
        IDurableCommandCatalog catalog,
        IServiceScopeFactory scopeFactory,
        IOptions<DurableCommandBusOptions> options,
        DurableCommandMetrics metrics,
        TimeProvider clock,
        ILogger<DurableCommandConsumer<TContext>> logger)
    {
        _catalog = catalog;
        _scopeFactory = scopeFactory;
        _options = options;
        _metrics = metrics;
        _clock = clock;
        _logger = logger;
        _consumerInstanceId = $"{Environment.MachineName}/{Environment.ProcessId}";
    }

    public async Task HandleAsync(ConsumeContext<DurableCommandEnvelope> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var envelope = context.Message;
        var ct = context.CancellationToken;

        if (!_catalog.TryGet(envelope.CommandTypeKey, out var registration))
        {
            _logger.LogWarning(
                "Durable command of unknown type {CommandTypeKey} (id={CommandId}) — dead-lettering.",
                envelope.CommandTypeKey, envelope.CommandId);
            throw new DurableCommandException(
                $"Unknown durable command type {envelope.CommandTypeKey}.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var ledger = scope.ServiceProvider.GetRequiredService<ICommandLedger>();

        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var claim = await ledger.TryClaimAsync(envelope, ct).ConfigureAwait(false);
        if (claim.Outcome is LedgerClaim.AlreadyApplied or LedgerClaim.AlreadyFailed)
        {
            _logger.LogInformation(
                "Durable command {CommandId} ({CommandType}) — redelivery skipped; previous outcome was {Outcome}.",
                envelope.CommandId, envelope.CommandTypeKey, claim.Outcome);
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }
        // PendingRetry can happen when an earlier delivery crashed mid-flight. Because we use
        // explicit transactions, a crash always rolls back — so a Pending row in the DB means
        // the *committed* state DIDN'T include an aggregate change. Treat it like FirstClaim:
        // re-run the handler. The handler's idempotency is its own (e.g. PDMS RecordReading
        // ties the new reading's id to the CommandId, so a re-run produces the same id).

        object command;
        try
        {
            command = registration.Deserialize(envelope.PayloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Durable command {CommandId} ({CommandType}) — payload failed to deserialize; dead-lettering.",
                envelope.CommandId, envelope.CommandTypeKey);
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw new DurableCommandException(
                $"Failed to deserialize payload for {envelope.CommandTypeKey} (commandId={envelope.CommandId}).", ex);
        }

        DurableCommandScope.Activate(envelope.CommandId);
        try
        {
            var resultJson = await registration.Dispatch(command, scope.ServiceProvider, ct).ConfigureAwait(false);
            await ledger.MarkAppliedAsync(envelope.CommandId, resultJson, _consumerInstanceId, ct).ConfigureAwait(false);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);

            var appliedAt = _clock.GetUtcNow().UtcDateTime;
            var latencySeconds = Math.Max(0, (appliedAt - envelope.EnqueuedAtUtc).TotalSeconds);
            _metrics.CommandsApplied.Add(
                1,
                new KeyValuePair<string, object?>("module", _options.Value.ModuleSlug),
                new KeyValuePair<string, object?>("command_type", envelope.CommandTypeKey));
            _metrics.CommandLatencySeconds.Record(
                latencySeconds,
                new KeyValuePair<string, object?>("module", _options.Value.ModuleSlug),
                new KeyValuePair<string, object?>("command_type", envelope.CommandTypeKey));

            _logger.LogInformation(
                "Durable command {CommandId} ({CommandType}) applied in {Module} ({LatencyMs}ms).",
                envelope.CommandId, envelope.CommandTypeKey, _options.Value.ModuleSlug,
                (long)(latencySeconds * 1000));
        }
        catch (DurableCommandException)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            _metrics.CommandsFailed.Add(
                1,
                new KeyValuePair<string, object?>("module", _options.Value.ModuleSlug),
                new KeyValuePair<string, object?>("command_type", envelope.CommandTypeKey),
                new KeyValuePair<string, object?>("reason", "catalog_or_payload"));
            throw;
        }
        catch (Exception ex)
        {
            // Handler threw something non-fatal-to-the-consumer. Roll back the in-flight tx,
            // then write a terminal Failed row in a SEPARATE tx so the failure record outlives
            // the rollback. Rethrow so the broker nacks → DLQ.
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            await WriteFailureAsync(envelope, ex, ct).ConfigureAwait(false);
            _metrics.CommandsFailed.Add(
                1,
                new KeyValuePair<string, object?>("module", _options.Value.ModuleSlug),
                new KeyValuePair<string, object?>("command_type", envelope.CommandTypeKey),
                new KeyValuePair<string, object?>("reason", "handler_exception"));
            throw;
        }
        finally
        {
            DurableCommandScope.Clear();
        }
    }

    private async Task WriteFailureAsync(DurableCommandEnvelope envelope, Exception ex, CancellationToken ct)
    {
        await using var failureScope = _scopeFactory.CreateAsyncScope();
        var failureDb = failureScope.ServiceProvider.GetRequiredService<TContext>();
        var failureLedger = failureScope.ServiceProvider.GetRequiredService<ICommandLedger>();
        try
        {
            var claim = await failureLedger.TryClaimAsync(envelope, ct).ConfigureAwait(false);
            if (claim.Outcome is LedgerClaim.AlreadyApplied or LedgerClaim.AlreadyFailed)
            {
                return;
            }
            var failureJson = JsonSerializer.Serialize(new
            {
                exceptionType = ex.GetType().FullName,
                message = ex.Message,
            }, DurableCommandConsumerJson.Options);
            await failureLedger.MarkFailedAsync(envelope.CommandId, failureJson, _consumerInstanceId, ct).ConfigureAwait(false);
            await failureDb.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception failureEx)
        {
            _logger.LogError(failureEx,
                "Durable command {CommandId} ({CommandType}) — failed to write failure ledger row after handler error.",
                envelope.CommandId, envelope.CommandTypeKey);
        }
    }
}

// Non-generic holder so the options instance is shared across every closed DurableCommandConsumer<T>.
file static class DurableCommandConsumerJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
