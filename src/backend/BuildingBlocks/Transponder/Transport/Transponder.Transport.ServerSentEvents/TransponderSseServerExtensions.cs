using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>Maps HTTP POST publish and GET <c>text/event-stream</c> subscribe for <see cref="TransponderSseIngressRelay"/>.</summary>
public static class TransponderSseServerExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers <see cref="TransponderSseIngressOptions"/> and <see cref="TransponderSseIngressRelay"/>.</summary>
        public IServiceCollection AddTransponderSseIngressServer(
            Action<TransponderSseIngressOptions>? configure = null)
        {
            services.AddOptions<TransponderSseIngressOptions>();
            if (configure is not null)
                services.Configure(configure);

            services.AddSingleton<TransponderSseIngressRelay>();
            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>Maps <c>{PathPrefix}/publish</c> (POST JSON) and <c>{PathPrefix}/subscribe</c> (GET SSE).</summary>
        public WebApplication MapTransponderSseIngress()
        {
            var options = app.Services.GetRequiredService<IOptions<TransponderSseIngressOptions>>().Value;
            var prefix = options.PathPrefix.TrimEnd('/');

            app.MapPost(
                    $"{prefix}/publish",
                    async (TransponderSseEnvelopeDto? dto, HttpContext httpContext, TransponderSseIngressRelay relay, IServiceProvider services, CancellationToken cancellationToken) =>
                    {
                        if (dto is null || string.IsNullOrEmpty(dto.RoutingKey))
                            return Results.BadRequest();

                        if (services.GetService<ITransponderSseAuthorizer>() is { } authorizer)
                            await authorizer.AuthorizePublishAsync(httpContext, dto, cancellationToken).ConfigureAwait(false);

                        if (services.GetService<ITransponderSsePublishJournal>() is { } journal)
                            await journal.AppendAsync(dto, httpContext, cancellationToken).ConfigureAwait(false);

                        await relay.BroadcastAsync(dto, cancellationToken).ConfigureAwait(false);
                        return Results.NoContent();
                    })
                .DisableAntiforgery();

            app.MapGet(
                $"{prefix}/subscribe",
                async (HttpContext httpContext, TransponderSseIngressRelay relay, IServiceProvider services, CancellationToken ct) =>
                {
                    if (services.GetService<ITransponderSseAuthorizer>() is { } authorizer)
                        await authorizer.AuthorizeSubscribeAsync(httpContext, ct).ConfigureAwait(false);

                    await relay.SubscribeAsync(httpContext).ConfigureAwait(false);
                });

            return app;
        }
    }
}
