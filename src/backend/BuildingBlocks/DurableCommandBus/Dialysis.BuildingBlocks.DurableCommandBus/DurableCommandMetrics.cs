using System.Diagnostics.Metrics;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Per-module observability surface for the durable command bus. Meter name is exposed
/// as <see cref="MeterName"/> so hosts can register it on the OTLP meter provider via
/// <c>ModuleTelemetryOptions.AdditionalMeters.Add(DurableCommandMetrics.MeterName)</c>
/// in their composition root.
///
/// Emitted instruments — see <c>docs/operations/observability.md</c> for the
/// production dashboards built on these.
/// </summary>
public sealed class DurableCommandMetrics : IDisposable
{
    /// <summary>OpenTelemetry meter name. Stable contract — dashboards reference it.</summary>
    public const string MeterName = "Dialysis.DurableCommandBus";

    private readonly Meter _meter;

    public DurableCommandMetrics()
    {
        _meter = new Meter(MeterName, "1.0");

        CommandsEnqueued = _meter.CreateCounter<long>(
            "dialysis.durable_commands.enqueued",
            unit: "{commands}",
            description: "Total durable commands accepted by the publisher (publisher-confirm ACK received).");

        CommandsApplied = _meter.CreateCounter<long>(
            "dialysis.durable_commands.applied",
            unit: "{commands}",
            description: "Total durable commands the consumer applied to the database (ledger row + aggregate change committed).");

        CommandsFailed = _meter.CreateCounter<long>(
            "dialysis.durable_commands.failed",
            unit: "{commands}",
            description: "Total durable commands that exited the consumer in the Failed state (handler threw a non-transient error and the envelope was dead-lettered).");

        CommandLatencySeconds = _meter.CreateHistogram<double>(
            "dialysis.durable_commands.latency",
            unit: "s",
            description: "Enqueue → applied latency observed by the consumer. The gap between the envelope's EnqueuedAtUtc and the ledger row's AppliedAtUtc.");

        EnqueueLatencyMilliseconds = _meter.CreateHistogram<double>(
            "dialysis.durable_commands.enqueue_latency",
            unit: "ms",
            description: "Publish → confirm-ack latency observed on the bus side. Tracks broker round-trip + JSON serialize cost.");
    }

    /// <summary>Counter incremented by <c>DurableCommandBus.EnqueueAsync</c> on each successful publish-confirm.</summary>
    public Counter<long> CommandsEnqueued { get; }

    /// <summary>Counter incremented by <c>DurableCommandConsumer</c> after a successful transaction commit.</summary>
    public Counter<long> CommandsApplied { get; }

    /// <summary>Counter incremented by <c>DurableCommandConsumer</c> after writing a Failed ledger row.</summary>
    public Counter<long> CommandsFailed { get; }

    /// <summary>End-to-end latency: from caller-side enqueue stamp to server-side applied stamp.</summary>
    public Histogram<double> CommandLatencySeconds { get; }

    /// <summary>Bus-side publish latency for the 202 response time SLO.</summary>
    public Histogram<double> EnqueueLatencyMilliseconds { get; }

    public void Dispose() => _meter.Dispose();
}
