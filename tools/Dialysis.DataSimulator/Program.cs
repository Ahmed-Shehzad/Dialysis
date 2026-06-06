using Dialysis.DataSimulator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddHostedService<ContinuousDataWorker>();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
