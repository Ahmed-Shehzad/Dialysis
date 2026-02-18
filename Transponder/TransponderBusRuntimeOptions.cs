using Microsoft.Extensions.Logging;

using Transponder.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder;

public sealed class TransponderBusRuntimeOptions
{
    public Func<Type, Uri?>? RequestAddressResolver { get; init; }

    public TimeSpan? DefaultRequestTimeout { get; init; }

    public Func<TransponderBus, IMessageScheduler>? SchedulerFactory { get; init; }

    public IEnumerable<IReceiveEndpoint>? ReceiveEndpoints { get; init; }

    public OutboxDispatcher? OutboxDispatcher { get; init; }

    public IEnumerable<ITransponderMessageScopeProvider>? MessageScopeProviders { get; init; }

    public ILoggerFactory? LoggerFactory { get; init; }
}
