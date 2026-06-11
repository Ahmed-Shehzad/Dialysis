using Dialysis.BuildingBlocks.Transponder.Diagnostics;
using Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder.Reactive.Signals;

/// <summary>Opt-in registration for the reactive signals surface and its diagnostics pilot.</summary>
public static class TransponderReactiveSignalsServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the <see cref="SignalStore"/>, the diagnostics signal graph
        /// (<see cref="TransponderDiagnosticsSignals"/>), the signals-backed
        /// <see cref="ITransponderStateObserver"/> consumed by transports and the outbox
        /// relay, and the hosted service bridging health to the
        /// <c>Dialysis.Transponder.Signals</c> meter and the log. Add that meter name to
        /// <c>ModuleTelemetryOptions.AdditionalMeters</c> to export it.
        /// </summary>
        public IServiceCollection AddTransponderReactiveSignals(
            Action<TransponderSignalsDiagnosticsOptions>? configure = null)
        {
            if (configure is not null)
            {
                services.Configure(configure);
            }
            else
            {
                services.AddOptions<TransponderSignalsDiagnosticsOptions>();
            }

            services.TryAddSingleton<SignalStore>();
            services.TryAddSingleton<TransponderDiagnosticsSignals>();
            services.TryAddSingleton<ITransponderStateObserver, SignalTransponderStateObserver>();
            services.AddHostedService<TransponderSignalsDiagnosticsHostedService>();
            return services;
        }
    }
}
