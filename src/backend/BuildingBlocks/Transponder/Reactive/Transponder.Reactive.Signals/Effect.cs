using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>
/// A reactive side-effect over one or more signals. Source changes wake a dedicated pump loop
/// through a bounded(1), drop-write channel, so rapid bursts coalesce into at most one queued
/// run and the reaction always observes the latest state when it executes (latest-wins —
/// effects are for current-state sinks like gauges, logs, and push notifications, not for
/// processing every transition; consume messages for that).
///
/// The reaction runs on the thread pool — no <see cref="SynchronizationContext"/> or UI
/// scheduler is involved. It also runs once at startup so the effect observes the initial
/// state. Failures are logged and never propagate into the signal graph: effects are sinks,
/// never actors. Dispose to detach from sources and stop the pump.
/// </summary>
public sealed class Effect : IDisposable, IAsyncDisposable
{
    private readonly Channel<bool> _wake = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    private readonly Func<CancellationToken, Task> _reaction;
    private readonly ILogger _logger;
    private readonly IDisposable[] _subscriptions;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;
    private int _disposed;

    /// <summary>Creates an effect with an asynchronous reaction.</summary>
    /// <param name="sources">Signals whose changes trigger the reaction.</param>
    /// <param name="reaction">The reaction; read current signal values inside it.</param>
    /// <param name="logger">Sink for reaction failures; defaults to a null logger.</param>
    public Effect(IReadOnlyList<ISignal> sources, Func<CancellationToken, Task> reaction, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(reaction);
        _reaction = reaction;
        _logger = logger ?? NullLogger.Instance;

        _subscriptions = new IDisposable[sources.Count];
        for (var i = 0; i < sources.Count; i++)
        {
            _subscriptions[i] = sources[i].Subscribe(Wake);
        }

        // Prime once so the effect observes the initial state.
        _wake.Writer.TryWrite(true);
        _pump = Task.Run(PumpAsync);
    }

    /// <summary>Creates an effect with a synchronous reaction.</summary>
    /// <param name="sources">Signals whose changes trigger the reaction.</param>
    /// <param name="reaction">The reaction; read current signal values inside it.</param>
    /// <param name="logger">Sink for reaction failures; defaults to a null logger.</param>
    public Effect(IReadOnlyList<ISignal> sources, Action reaction, ILogger? logger = null)
        : this(
            sources,
            reaction is null
                ? throw new ArgumentNullException(nameof(reaction))
                : _ =>
                {
                    reaction();
                    return Task.CompletedTask;
                },
            logger)
    {
    }

    /// <summary>Detaches from all sources and stops the pump without waiting for it.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _wake.Writer.TryComplete();
        _cts.Cancel();
        _cts.Dispose();
    }

    /// <summary>Detaches from all sources, stops the pump, and awaits its completion.</summary>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        try
        {
            // The pump is this instance's own free-threaded Task (no context to deadlock on).
#pragma warning disable VSTHRD003
            await _pump.ConfigureAwait(false);
#pragma warning restore VSTHRD003
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation raced the pump's read.
        }
    }

    private void Wake() => _wake.Writer.TryWrite(true);

    private async Task PumpAsync()
    {
        try
        {
            await foreach (var _ in _wake.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
            {
                try
                {
                    await _reaction(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Effects are sinks: failures are reported, never rethrown into the graph.
                    _logger.LogError(ex, "Signal effect reaction failed; the effect stays alive.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed while awaiting a wake.
        }
        catch (ObjectDisposedException)
        {
            // CTS disposed during shutdown race.
        }
    }
}
