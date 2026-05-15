using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

public static class GrpcTransponderServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces the default in-process <see cref="ITransponderBus"/> with a gRPC client to a <see cref="TransponderGrpcIngressService"/> relay.
        /// Call after <see cref="TransponderServiceCollectionExtensions.AddTransponder"/>.
        /// </summary>
        public IServiceCollection AddTransponderGrpc(
            Action<TransponderGrpcClientOptions> configureOptions,
            Action<GrpcSubscriptionBuilder>? configureSubscriptions = null)
        {
            ArgumentNullException.ThrowIfNull(configureOptions);
            services.AddLogging();
            services.Configure(configureOptions);

            var registry = new GrpcSubscriptionRegistry();
            configureSubscriptions?.Invoke(new GrpcSubscriptionBuilder(services, registry));
            new GrpcSubscriptionBuilder(services, registry).Listen<TransponderMessageChunk>();
            services.AddSingleton(registry);

            services.RemoveDescriptorsFor(typeof(ITransponderBus));
            services.AddSingleton<ITransponderTransport, GrpcTransponderTransport>();
            services.AddSingleton<ITransponderBus, GrpcTransponderBus>();
            services.AddHostedService<GrpcTransponderConsumerHostedService>();
            return services;
        }
    }
}
