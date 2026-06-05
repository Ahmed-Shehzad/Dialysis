using Dialysis.Module.Bff;
using Dialysis.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddModuleBff();

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapModuleBff();

await app.RunAsync().ConfigureAwait(false);
