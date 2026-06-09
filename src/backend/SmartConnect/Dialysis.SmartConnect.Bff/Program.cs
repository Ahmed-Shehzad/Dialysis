using Dialysis.Module.Bff;
using Dialysis.ServiceDefaults;

namespace Dialysis.SmartConnect.Bff;

/// <summary>Application entry point.</summary>
public partial class Program
{
    /// <summary>Builds and runs the host.</summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.AddModuleBff();

        var app = builder.Build();
        app.MapDefaultEndpoints();
        app.MapModuleBff();

        await app.RunAsync().ConfigureAwait(false);
    }
}
