using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Optional JWT authentication for management routes when <c>SmartConnect:Management:Jwt:Authority</c> (or similar) is configured.</summary>
public static class ManagementSecurityExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication if configuration section <c>SmartConnect:Management:Jwt</c> defines <c>Authority</c> and <c>Audience</c>.
    /// Call <c>app.UseAuthentication(); app.UseAuthorization();</c> before mapping routes; add <c>.RequireAuthorization()</c> to management groups as needed.
    /// </summary>
    public static IServiceCollection AddSmartConnectManagementJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization();

        var jwt = configuration.GetSection("SmartConnect:Management:Jwt");
        var authority = jwt["Authority"];
        var audience = jwt["Audience"];
        if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
        {
            return services;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.TokenValidationParameters.ValidateAudience = true;
            });
        return services;
    }
}
