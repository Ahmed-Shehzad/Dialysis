var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    gateway = "dialysis",
    routes = new[] { "/identity", "/his", "/smartconnect", "/ehr", "/pdms", "/portal" },
}));

app.MapReverseProxy();

await app.RunAsync().ConfigureAwait(false);
