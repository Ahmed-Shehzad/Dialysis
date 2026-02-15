using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Dialysis.Auth;

public static class JwtAuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        var options = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Read", p => p.RequireClaim("scope", "dialysis.read", "dialysis.admin"));
            options.AddPolicy("Write", p => p.RequireClaim("scope", "dialysis.write", "dialysis.admin"));
            options.AddPolicy("Admin", p => p.RequireClaim("scope", "dialysis.admin"));
            options.AddPolicy("Research", p => p.RequireClaim("scope", "dialysis.research", "dialysis.admin"));
        });

        return services;
    }

    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
