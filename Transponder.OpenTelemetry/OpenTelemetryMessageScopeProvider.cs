using System.Diagnostics;
using System.Diagnostics.Metrics;

using Transponder.Abstractions;

namespace Transponder.OpenTelemetry;

internal sealed class OpenTelemetryMessageScopeProvider : ITransponderMessageScopeProvider
{
    private const string MessagingSystem = "transponder";

    private readonly TransponderOpenTelemetryOptions _options;
    private readonly TransponderOpenTelemetryInstrumentation _instrumentation;

    public OpenTelemetryMessageScopeProvider(
        TransponderOpenTelemetryOptions options,
        TransponderOpenTelemetryInstrumentation instrumentation)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    public IDisposable? BeginSend(TransponderMessageContext context)
    {
        RecordMetric(_instrumentation.SendCounter, "send", context);
        return StartActivity("transponder.send", ActivityKind.Producer, "send", context);
    }

    public IDisposable? BeginPublish(TransponderMessageContext context)
    {
        RecordMetric(_instrumentation.PublishCounter, "publish", context);
        return StartActivity("transponder.publish", ActivityKind.Producer, "publish", context);
    }

    public IDisposable? BeginConsume(TransponderMessageContext context)
    {
        RecordMetric(_instrumentation.ConsumeCounter, "consume", context);
        return StartActivity("transponder.consume", ActivityKind.Consumer, "consume", context);
    }

    private Activity? StartActivity(
        string name,
        ActivityKind kind,
        string operation,
        TransponderMessageContext context)
    {
        if (!_options.EnableTracing) return null;

        Activity? activity = _instrumentation.ActivitySource.StartActivity(name, kind);
        if (activity is null) return null;

        _ = activity.SetTag("messaging.system", MessagingSystem);
        _ = activity.SetTag("messaging.operation", operation);
        SetTags(activity, context);

        return activity;
    }

    private void RecordMetric(Counter<long> counter, string operation, TransponderMessageContext context)
    {
        if (!_options.EnableMetrics) return;

        var tags = new TagList
        {
            { "messaging.system", MessagingSystem },
            { "messaging.operation", operation }
        };

        AddTags(tags, context);
        counter.Add(1, tags);
    }

    private static void SetTags(Activity activity, TransponderMessageContext context)
    {
        if (context.MessageId.HasValue)
            _ = activity.SetTag("messaging.message_id", context.MessageId.Value.ToString());

        if (context.CorrelationId.HasValue)
            _ = activity.SetTag("messaging.correlation_id", context.CorrelationId.Value.ToString());

        if (context.ConversationId.HasValue)
            _ = activity.SetTag("messaging.conversation_id", context.ConversationId.Value.ToString());

        if (!string.IsNullOrWhiteSpace(context.MessageType))
            _ = activity.SetTag("messaging.message_type", context.MessageType);

        if (context.SourceAddress is not null)
            _ = activity.SetTag("messaging.source_address", context.SourceAddress.ToString());

        if (context.DestinationAddress is not null)
            _ = activity.SetTag("messaging.destination_address", context.DestinationAddress.ToString());
    }

    private static void AddTags(TagList tags, TransponderMessageContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.MessageType))
            tags.Add("messaging.message_type", context.MessageType);

        if (context.DestinationAddress is not null)
            tags.Add("messaging.destination_address", context.DestinationAddress.ToString());
        else if (context.SourceAddress is not null)
            tags.Add("messaging.source_address", context.SourceAddress.ToString());
    }
}
