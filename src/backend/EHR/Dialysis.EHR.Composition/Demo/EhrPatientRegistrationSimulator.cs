using Dialysis.EHR.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Composition.Demo;

/// <summary>
/// Development-only background service. Periodically registers a brand-new synthetic patient via the
/// <see cref="IPatientRepository"/> directly (same bypass pattern as <c>EhrDemoSeeder</c> and the
/// PDMS vitals ticker — system automation, not a user action). Lets the SPA cross-module integration
/// events feed continuously show new <c>PatientRegisteredIntegrationEvent</c> rows during a demo.
/// </summary>
public sealed class EhrPatientRegistrationSimulator(
    IServiceProvider services,
    ILogger<EhrPatientRegistrationSimulator> logger) : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(20);
    private static readonly string[] _familyNames =
    [
        "Andersen", "Bauer", "Carter", "Dubois", "Esposito", "Fernandez", "Gupta",
        "Hayashi", "Ivanov", "Johansson", "Kim", "Lopez", "Müller", "Novak",
    ];
    private static readonly string[] _givenNames =
    [
        "Liam", "Olivia", "Noah", "Emma", "Lucas", "Ava", "Mateo",
        "Mei", "Ahmed", "Sophia", "Aiden", "Maya", "Diego", "Yara",
    ];
    private static readonly Random _rng = new(31415);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EHR patient registration simulator started (every {Seconds}s).", _interval.TotalSeconds);
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken).ConfigureAwait(false); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "EHR patient registration simulator tick failed.");
            }
            try { await Task.Delay(_interval, stoppingToken).ConfigureAwait(false); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var patients = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();

        // Full Guid v7 hex (32 chars) — the first 12 hex are the 48-bit millisecond timestamp
        // (changes every tick), the remaining 20 are random. A 6-char prefix collided every tick
        // because the top 24 bits of an ms timestamp only flip every ~4.66h.
        var mrn = $"MRN-SIM-{Guid.CreateVersion7():N}";
        var family = _familyNames[_rng.Next(_familyNames.Length)];
        var given = _givenNames[_rng.Next(_givenNames.Length)];
        var year = 1940 + _rng.Next(70);
        var dob = new DateOnly(year, _rng.Next(1, 12), _rng.Next(1, 28));

        var patient = Patient.Register(
            Guid.CreateVersion7(),
            mrn,
            new HumanName(family, given),
            dob,
            sexAtBirthCode: _rng.Next(2) == 0 ? "male" : "female",
            preferredLanguageCode: "en-US");
        patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
