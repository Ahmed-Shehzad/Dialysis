using Dialysis.HIS.Security.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Security;

public static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// Registers authorization services and <see cref="ICurrentUser"/> (scoped). Bind <see cref="HisAuthenticationOptions"/> from <c>His:Authentication</c>.
    /// When <see cref="HisAuthenticationOptions.Authority"/> is unset, <see cref="HttpContextCurrentUser"/> grants all <see cref="HisPermissions"/> for local development.
    /// </summary>
    public static IServiceCollection AddHisSecurityCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HisAuthenticationOptions>(configuration.GetSection("His:Authentication"));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<IHisAuthorizationService, HisAuthorizationService>();
        return services;
    }
}
