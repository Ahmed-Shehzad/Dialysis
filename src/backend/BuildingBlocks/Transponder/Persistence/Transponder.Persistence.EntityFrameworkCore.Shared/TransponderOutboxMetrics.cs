using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Observability surface for the Transponder outbox relay — the backbone of every cross-module
/// flow, previously a blind spot (only the durable command bus had a meter). Static because the
/// relay is a generic hosted service per <c>TContext</c>; the owning DbContext name rides on every
/// measurement as the <c>context</c> tag, which identifies the module. The meter is registered on
/// the OTLP provider centrally by <c>ModuleTelemetryExtensions</c>, so hosts that never enable the
/// relay simply emit nothing. Instrument names are stable contracts — the
/// <c>deploy/k8s/observability</c> dashboards and alerts reference them.
/// </summary>
public static class TransponderOutboxMetrics
{
    /// <summary>OpenTelemetry meter name. Stable contract — dashboards reference it.</summary>
    public const string MeterName = "Dialysis.Transponder.Outbox";

    private static readonly Meter _meter = new(MeterName, "1.0");

    private static readonly Counter<long> _published = _meter.CreateCounter<long>(
        "dialysis.transponder.outbox.published",
        unit: "{messages}",
        description: "Outbox rows the relay successfully published to the bus and marked processed.");

    private static readonly Counter<long> _failed = _meter.CreateCounter<long>(
        "dialysis.transponder.outbox.failed",
        unit: "{messages}",
        description: "Outbox publish attempts that threw; the row stays pending and is retried on the next poll.");

    // Updated once per relay poll from the already-fetched batch (zero extra queries): the batch
    // is ordered by CreatedAtUtc, so its head is the oldest pending row. 0 = outbox drained.
    private static readonly ConcurrentDictionary<string, double> _oldestPendingAgeSeconds = new(StringComparer.Ordinal);

#pragma warning disable CA1823, IDE0052 // The meter holds the gauge alive; it is read by the OTel SDK, not by code.
    private static readonly ObservableGauge<double> _oldestPendingAge = _meter.CreateObservableGauge(
        "dialysis.transponder.outbox.oldest_pending_age",
        () => _oldestPendingAgeSeconds.Select(pair =>
            new Measurement<double>(pair.Value, new KeyValuePair<string, object?>(ContextTag, pair.Key))),
        unit: "s",
        description: "Age of the oldest unprocessed outbox row as observed on the last relay poll. Sustained growth = relay lag (broker down, poison row, or relay not running).");
#pragma warning restore CA1823, IDE0052

    private const string ContextTag = "context";

    /// <summary>Records a successful publish for the given DbContext (module) name.</summary>
    public static void RecordPublished(string contextName) =>
        _published.Add(1, new KeyValuePair<string, object?>(ContextTag, contextName));

    /// <summary>Records a failed publish attempt for the given DbContext (module) name.</summary>
    public static void RecordFailure(string contextName) =>
        _failed.Add(1, new KeyValuePair<string, object?>(ContextTag, contextName));

    /// <summary>Updates the oldest-pending-age gauge from the head of the freshly fetched batch.</summary>
    public static void RecordOldestPendingAge(string contextName, TimeSpan age) =>
        _oldestPendingAgeSeconds[contextName] = age >= TimeSpan.Zero ? age.TotalSeconds : 0d;
}
