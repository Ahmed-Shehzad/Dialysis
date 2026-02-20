using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Options;

/// <summary>
/// Startup config validation. Fails fast when required config is missing.
/// </summary>
public static class StartupConfigValidationExtensions
{
    /// <summary>
    /// Validates JWT Bearer Authority when not in Development.
    /// Call after AddConfiguration and before Build().
    /// </summary>
    public static IServiceCollection AddJwtBearerStartupValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services.AddOptions<JwtBearerStartupOptions>()
            .Bind(configuration.GetSection(JwtBearerStartupOptions.SectionName))
            .ValidateOnStart();

        _ = services.AddSingleton<IValidateOptions<JwtBearerStartupOptions>, JwtBearerStartupValidator>();
        return services;
    }
}

internal sealed class JwtBearerStartupValidator : IValidateOptions<JwtBearerStartupOptions>
{
    private readonly IHostEnvironment _env;

    public JwtBearerStartupValidator(IHostEnvironment env)
    {
        _env = env;
    }

    public ValidateOptionsResult Validate(string? name, JwtBearerStartupOptions options)
    {
        if (_env.IsDevelopment())
            return ValidateOptionsResult.Success;

        if (string.IsNullOrWhiteSpace(options?.Authority))
        {
            return ValidateOptionsResult.Fail(
                "Authentication:JwtBearer:Authority is required when not in Development.");
        }

        return ValidateOptionsResult.Success;
    }
}
