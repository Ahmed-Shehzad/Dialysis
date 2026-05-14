using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns;

/// <summary>Declares which message contracts this application consumes from the SQS queue.</summary>
public sealed class AwsSqsSubscriptionBuilder(IServiceCollection services, AwsSqsSubscriptionRegistry registry)
{
    public AwsSqsSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        return this;
    }
}
