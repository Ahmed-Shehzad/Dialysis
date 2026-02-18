using Serilog.Context;

using Transponder.Abstractions;

namespace Transponder.Serilog;

internal sealed class SerilogMessageScopeProvider : ITransponderMessageScopeProvider
{
    private readonly TransponderSerilogOptions _options;

    public SerilogMessageScopeProvider(TransponderSerilogOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IDisposable? BeginSend(TransponderMessageContext context)
        => BeginScope(context, "send");

    public IDisposable? BeginPublish(TransponderMessageContext context)
        => BeginScope(context, "publish");

    public IDisposable? BeginConsume(TransponderMessageContext context)
        => BeginScope(context, "consume");

    private IDisposable? BeginScope(TransponderMessageContext context, string operation)
    {
        ArgumentNullException.ThrowIfNull(context);

        List<IDisposable>? scopes = null;

        TryPushProperty(_options.OperationPropertyName, operation, ref scopes);
        TryPushProperty(_options.MessageIdPropertyName, context.MessageId, ref scopes);
        TryPushProperty(_options.CorrelationIdPropertyName, context.CorrelationId, ref scopes);
        TryPushProperty(_options.ConversationIdPropertyName, context.ConversationId, ref scopes);
        TryPushProperty(_options.MessageTypePropertyName, context.MessageType, ref scopes);
        TryPushProperty(_options.SourceAddressPropertyName, context.SourceAddress?.ToString(), ref scopes);
        TryPushProperty(_options.DestinationAddressPropertyName, context.DestinationAddress?.ToString(), ref scopes);
        TryPushProperty(_options.SentTimePropertyName, context.SentTime, ref scopes);

        return scopes is null ? null : new CompositeScope(scopes);
    }

    private static void TryPushProperty(string propertyName, object? value, ref List<IDisposable>? scopes)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || value is null) return;

        scopes ??= [];
        scopes.Add(LogContext.PushProperty(propertyName, value));
    }

    private sealed class CompositeScope : IDisposable
    {
        private readonly List<IDisposable> _scopes;

        public CompositeScope(List<IDisposable> scopes)
        {
            _scopes = scopes;
        }

        public void Dispose()
        {
            for (int i = _scopes.Count - 1; i >= 0; i--) _scopes[i].Dispose();
        }
    }
}
