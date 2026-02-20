using Dialysis.Alarm.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.PostgreSql;

using Verifier.Exceptions;

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

    public static void UseAlarmExceptionHandler(this IApplicationBuilder app)
    {
        _ = app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                Exception? exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                if (exception is ValidationException validationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    var errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                    await context.Response.WriteAsJsonAsync(new { errors });
                    return;
                }
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            });
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
