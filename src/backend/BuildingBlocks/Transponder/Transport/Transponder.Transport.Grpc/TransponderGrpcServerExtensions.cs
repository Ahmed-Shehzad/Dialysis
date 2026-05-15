using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Registers the gRPC ingress relay used by <see cref="GrpcTransponderServiceCollectionExtensions.AddTransponderGrpc"/> clients.</summary>
public static class TransponderGrpcServerExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="TransponderGrpcIngressHub"/> and <see cref="TransponderGrpcIngressService"/> with gRPC server infrastructure.</summary>
        /// <param name="configure">Optional limits and diagnostics for <see cref="Grpc.AspNetCore.Server.GrpcServiceOptions"/>.</param>
        public IServiceCollection AddTransponderGrpcIngressServer(
            Action<TransponderGrpcIngressOptions>? configure = null)
        {
            services.AddOptions<TransponderGrpcIngressOptions>();
            if (configure is not null)
                services.Configure(configure);

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPostConfigureOptions<GrpcServiceOptions>, TransponderGrpcIngressPostConfigureGrpcServiceOptions>());

            services.AddGrpc();
            services.AddSingleton<TransponderGrpcIngressHub>();
            services.AddSingleton<TransponderGrpcIngressService>();
            return services;
        }
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Maps <see cref="TransponderGrpcIngressService"/> (HTTP/2 required).</summary>
        public IEndpointRouteBuilder MapTransponderGrpcIngress()
        {
            endpoints.MapGrpcService<TransponderGrpcIngressService>();
            return endpoints;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>Maps <see cref="TransponderGrpcIngressService"/>.</summary>
        public WebApplication MapTransponderGrpcIngress()
        {
            app.MapGrpcService<TransponderGrpcIngressService>();
            return app;
        }
    }
}
