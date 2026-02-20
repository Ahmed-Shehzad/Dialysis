using Dialysis.Alarm.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.PostgreSql;


namespace Dialysis.Alarm.Api;

internal static class ProgramExtensions
{
    public static void AddAlarmJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.Authority = configuration["Authentication:JwtBearer:Authority"];
                opts.Audience = configuration["Authentication:JwtBearer:Audience"] ?? "api://dialysis-pdms";
                opts.RequireHttpsMetadata = configuration.GetValue("Authentication:JwtBearer:RequireHttpsMetadata", true);
                opts.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        string? token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token))
                            ctx.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });
    }

    public async static Task ApplyMigrationsIfDevelopmentAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        using IServiceScope scope = app.Services.CreateScope();
        AlarmDbContext db = scope.ServiceProvider.GetRequiredService<AlarmDbContext>();
        await db.Database.MigrateAsync();

        var transponderFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PostgreSqlTransponderDbContext>>();
        await using (var transponderDb = await transponderFactory.CreateDbContextAsync())
            await transponderDb.Database.MigrateAsync();
    }
}
