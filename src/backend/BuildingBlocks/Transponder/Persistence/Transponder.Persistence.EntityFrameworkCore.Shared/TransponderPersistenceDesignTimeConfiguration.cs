using Microsoft.Extensions.Configuration;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Builds <see cref="IConfiguration"/> for EF Core design-time (<c>dotnet ef</c>) the same way a typical client host does:
/// optional <c>appsettings.json</c> / environment-specific json from the process working directory, then environment variables (including hierarchical keys such as <c>Transponder__Persistence__Schema</c>).
/// </summary>
public static class TransponderPersistenceDesignTimeConfiguration
{
    public const string DefaultSectionName = "Transponder:Persistence";

    /// <summary>
    /// <paramref name="basePath"/> defaults to <see cref="Directory.GetCurrentDirectory"/> (use <c>dotnet ef --startup-project &lt;client host&gt;</c> so the client app folder contains <c>appsettings.json</c>).
    /// </summary>
    public static IConfigurationRoot Build(string? basePath = null, string? environmentName = null)
    {
        var env = environmentName
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var path = basePath ?? Directory.GetCurrentDirectory();

        return new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }
}
