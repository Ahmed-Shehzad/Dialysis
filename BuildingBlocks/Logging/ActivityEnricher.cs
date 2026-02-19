using System.Diagnostics;

using Serilog.Core;
using Serilog.Events;

namespace BuildingBlocks.Logging;

/// <summary>
/// Enriches log events with OpenTelemetry TraceId and SpanId from Activity.Current for correlation.
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        Activity? activity = Activity.Current;
        if (activity is null)
            return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
    }
}
