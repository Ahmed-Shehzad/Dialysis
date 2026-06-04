using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Declares which message contracts this application consumes from the gRPC ingress stream.</summary>
public sealed class GrpcSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly GrpcSubscriptionRegistry _registry;
    /// <summary>Declares which message contracts this application consumes from the gRPC ingress stream.</summary>
    public GrpcSubscriptionBuilder(IServiceCollection services, GrpcSubscriptionRegistry registry)
    {
        _services = services;
        _registry = registry;
    }
    public GrpcSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        _registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        return this;
    }
}
