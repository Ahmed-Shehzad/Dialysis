using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dialysis.Auth;

/// <summary>
/// Validates required production configuration at startup.
/// Fails fast if running in Production with missing or default Auth/connection settings.
/// </summary>
public static class ProductionConfigValidation
{
    /// <summary>
    /// Validates that Auth and other sensitive config are properly set when environment is Production.
    /// Call after <c>builder.Build()</c> and before <c>app.Run()</c>.
    /// </summary>
    public static WebApplication ValidateProductionConfig(this WebApplication app)
    {
        if (!string.Equals(app.Environment.EnvironmentName, Environments.Production, StringComparison.OrdinalIgnoreCase))
            return app;

        var config = app.Services.GetRequiredService<IConfiguration>();
        var authOptions = config.GetSection(AuthOptions.SectionName).Get<AuthOptions>();

        if (authOptions is null || !authOptions.IsProductionConfigured)
            throw new InvalidOperationException(
                "Production requires Auth:Authority and Auth:Audience to be configured for your IdP. " +
                "See docs/PRODUCTION-CONFIG.md.");

        return app;
    }
}
