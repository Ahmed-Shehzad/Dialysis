using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dialysis.DataSimulator;

/// <summary>Application entry point.</summary>
public partial class Program
{
    /// <summary>Builds and runs the host.</summary>
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // This process exists solely to run two independent, self-healing seeding loops. The .NET default
        // (BackgroundServiceExceptionBehavior.StopHost) couples them: an unhandled fault in either loop tears the
        // whole process down — including the healthy other loop. The loops already log-and-continue past per-call
        // failures (see CancellationClassifier), so an escaped exception is unexpected; surface it loudly but keep
        // the process and the sibling loop alive rather than killing the simulator.
        builder.Services.Configure<HostOptions>(o =>
            o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

        builder.Services.Configure<DataSimulatorOptions>(builder.Configuration.GetSection(DataSimulatorOptions.SectionName));

        builder.Services.AddSingleton<PatientGenerator>();
        builder.Services.AddSingleton<IAccessTokenProvider, ClientCredentialsTokenProvider>();
        builder.Services.AddTransient<BearerTokenHandler>();
        builder.Services.AddHttpClient("token");

        static Uri Base(IServiceProvider sp, Func<ModuleAddressOptions, string> pick) =>
            new(pick(sp.GetRequiredService<IOptions<DataSimulatorOptions>>().Value.Modules));

        builder.Services.AddHttpClient<IEhrClient, EhrClient>((sp, c) => c.BaseAddress = Base(sp, m => m.Ehr))
            .AddHttpMessageHandler<BearerTokenHandler>();
        builder.Services.AddHttpClient<IHisClient, HisClient>((sp, c) => c.BaseAddress = Base(sp, m => m.His))
            .AddHttpMessageHandler<BearerTokenHandler>();
        builder.Services.AddHttpClient<ILabClient, LabClient>((sp, c) => c.BaseAddress = Base(sp, m => m.Lab))
            .AddHttpMessageHandler<BearerTokenHandler>();
        builder.Services.AddHttpClient<IHieClient, HieClient>((sp, c) => c.BaseAddress = Base(sp, m => m.Hie))
            .AddHttpMessageHandler<BearerTokenHandler>();
        builder.Services.AddHttpClient<IPdmsClient, PdmsClient>((sp, c) => c.BaseAddress = Base(sp, m => m.Pdms))
            .AddHttpMessageHandler<BearerTokenHandler>();
        builder.Services.AddHttpClient<ISmartConnectClient, SmartConnectClient>((sp, c) => c.BaseAddress = Base(sp, m => m.SmartConnect))
            .AddHttpMessageHandler<BearerTokenHandler>();

        builder.Services.AddHostedService<ContinuousDataWorker>();
        // Live intradialytic vitals for in-progress sessions (chairside SignalR stream).
        builder.Services.AddHostedService<PdmsVitalsTickerService>();

        var host = builder.Build();
        await host.RunAsync().ConfigureAwait(false);
    }
}
