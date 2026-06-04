using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns;

/// <summary>Declares which message contracts this application consumes from the SQS queue.</summary>
public sealed class AwsSqsSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly AwsSqsSubscriptionRegistry _registry;
    /// <summary>Declares which message contracts this application consumes from the SQS queue.</summary>
    public AwsSqsSubscriptionBuilder(IServiceCollection services, AwsSqsSubscriptionRegistry registry)
    {
        _services = services;
        _registry = registry;
    }
    public AwsSqsSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        _registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        return this;
    }
}
