using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Namespace deliberately omits the `Hangfire` segment so `using Hangfire;` binds the global namespace.
namespace Dialysis.BuildingBlocks.Transponder.Hosting;

/// <summary>A module-contributed Hangfire recurring job: the job to run on a cron schedule.</summary>
public sealed record HangfireRecurringJobRegistration(string RecurringJobId, Job Job, string CronExpression);

/// <summary>
/// Lets a module register a persistent Hangfire recurring job in place of a periodic
/// <see cref="BackgroundService"/> timer. The job is installed into Hangfire storage at startup by
/// <see cref="HangfireRecurringJobInstaller"/> — but only when Hangfire is actually configured
/// (the host received ConnectionStrings:Hangfire). Hosts/tests without Hangfire simply skip it, so a
/// job never double-runs against a fallback timer.
/// </summary>
public static class HangfireRecurringJobExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a recurring job. Build <paramref name="job"/> with
        /// <c>Job.FromExpression&lt;TJob&gt;(j =&gt; j.RunAsync(CancellationToken.None))</c>; the job type
        /// is resolved from DI when Hangfire fires it, so it must be registered as a service.
        /// </summary>
        public IServiceCollection AddHangfireRecurringJob(string recurringJobId, Job job, string cronExpression)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(recurringJobId);
            ArgumentNullException.ThrowIfNull(job);
            ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

            services.AddSingleton(new HangfireRecurringJobRegistration(recurringJobId, job, cronExpression));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, HangfireRecurringJobInstaller>());
            return services;
        }
    }
}

/// <summary>
/// Installs every <see cref="HangfireRecurringJobRegistration"/> into Hangfire at startup, but only
/// when an <see cref="IRecurringJobManager"/> is available (i.e. Hangfire storage is configured).
/// </summary>
internal sealed class HangfireRecurringJobInstaller : IHostedService
{
    private readonly IReadOnlyList<HangfireRecurringJobRegistration> _registrations;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HangfireRecurringJobInstaller> _logger;

    public HangfireRecurringJobInstaller(
        IEnumerable<HangfireRecurringJobRegistration> registrations,
        IServiceProvider serviceProvider,
        ILogger<HangfireRecurringJobInstaller> logger)
    {
        _registrations = [.. registrations];
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var manager = _serviceProvider.GetService<IRecurringJobManager>();
        if (manager is null)
        {
            _logger.LogInformation(
                "Hangfire is not configured (no ConnectionStrings:Hangfire); {Count} recurring job(s) left unscheduled.",
                _registrations.Count);
            return Task.CompletedTask;
        }

        foreach (var registration in _registrations)
        {
            manager.AddOrUpdate(
                registration.RecurringJobId,
                registration.Job,
                registration.CronExpression,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
            _logger.LogInformation(
                "Scheduled Hangfire recurring job {RecurringJobId} ({Cron}).",
                registration.RecurringJobId,
                registration.CronExpression);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
