using Transponder.Abstractions;

namespace Transponder;

/// <summary>
/// Configures the Transponder bus.
/// </summary>
public sealed class TransponderBusOptions
{
    public TransponderBusOptions(Uri address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public Uri Address { get; }

    public TransponderTransportRegistrationOptions TransportBuilder { get; } = new();

    public TimeSpan DefaultRequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public Func<Type, Uri?>? RequestAddressResolver { get; set; }

    public string RequestPathPrefix { get; set; } = TransponderRequestAddressResolver.DefaultRequestPathPrefix;

    public Func<Type, string>? RequestPathFormatter { get; set; }

    public Func<IServiceProvider, TransponderBus, IMessageScheduler>? SchedulerFactory { get; set; }

    public OutboxDispatchOptions? OutboxOptions { get; internal set; }

    public TransponderBusOptions UseSagaOrchestration(Action<SagaRegistrationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        TransportBuilder.UseSagaOrchestration(configure);
        return this;
    }

    public TransponderBusOptions UseSagaChoreography(Action<SagaRegistrationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        TransportBuilder.UseSagaChoreography(configure);
        return this;
    }
}
