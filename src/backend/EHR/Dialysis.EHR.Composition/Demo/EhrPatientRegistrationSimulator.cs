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
public sealed class EhrPatientRegistrationSimulator : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EhrPatientRegistrationSimulator> _logger;
    /// <summary>
    /// Development-only background service. Periodically registers a brand-new synthetic patient via the
    /// <see cref="IPatientRepository"/> directly (same bypass pattern as <c>EhrDemoSeeder</c> and the
    /// PDMS vitals ticker — system automation, not a user action). Lets the SPA cross-module integration
    /// events feed continuously show new <c>PatientRegisteredIntegrationEvent</c> rows during a demo.
    /// </summary>
    public EhrPatientRegistrationSimulator(IServiceProvider services,
        ILogger<EhrPatientRegistrationSimulator> logger)
    {
        _services = services;
        _logger = logger;
    }
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(20);

    private static readonly (string Family, string Origin)[] _families =
    [
        ("Andersen", "NO"), ("Bauer", "DE"), ("Carter", "US"), ("Dubois", "FR"),
        ("Esposito", "IT"), ("Fernandez", "ES"), ("Gupta", "IN"), ("Hayashi", "JP"),
        ("Ivanov", "RU"), ("Johansson", "SE"), ("Kim", "KR"), ("Lopez", "MX"),
        ("Müller", "DE"), ("Novak", "CZ"), ("Okafor", "NG"), ("Pereira", "BR"),
        ("Qureshi", "PK"), ("Rossi", "IT"), ("Sato", "JP"), ("Tanaka", "JP"),
        ("Ueno", "JP"), ("Vargas", "MX"), ("Watanabe", "JP"), ("Yilmaz", "TR"),
        ("Zhao", "CN"), ("Patel", "IN"), ("Nguyen", "VN"), ("OConnor", "IE"),
        ("Schneider", "DE"), ("Lindqvist", "SE"),
    ];

    private static readonly (string Given, string Sex)[] _given =
    [
        ("Liam", "male"), ("Olivia", "female"), ("Noah", "male"), ("Emma", "female"),
        ("Lucas", "male"), ("Ava", "female"), ("Mateo", "male"), ("Mei", "female"),
        ("Ahmed", "male"), ("Sophia", "female"), ("Aiden", "male"), ("Maya", "female"),
        ("Diego", "male"), ("Yara", "female"), ("Hiroshi", "male"), ("Anika", "female"),
        ("Kwame", "male"), ("Zara", "female"), ("Ravi", "male"), ("Ines", "female"),
        ("Bjorn", "male"), ("Leila", "female"), ("Tomas", "male"), ("Chiara", "female"),
        ("Idris", "male"), ("Priya", "female"), ("Jamal", "male"), ("Sana", "female"),
    ];

    private static readonly string[] _middleNames =
    [
        "Alex", "Jordan", "Sam", "Taylor", "Riley", "Cameron", "Reese", "Sage", "", "", "",
    ];

    private static readonly (string Language, string[] Origins)[] _languages =
    [
        ("en-US", ["US", "IE", "NG"]),
        ("de-DE", ["DE", "CZ"]),
        ("fr-FR", ["FR"]),
        ("it-IT", ["IT"]),
        ("es-ES", ["ES", "MX", "BR"]),
        ("ja-JP", ["JP"]),
        ("ko-KR", ["KR"]),
        ("ru-RU", ["RU"]),
        ("zh-CN", ["CN", "VN"]),
        ("hi-IN", ["IN", "PK"]),
        ("tr-TR", ["TR"]),
        ("sv-SE", ["SE", "NO"]),
        ("pt-BR", ["BR"]),
    ];

    private static readonly Random _rng = new(31415);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EHR patient registration simulator started (every {Seconds}s).", _interval.TotalSeconds);
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
                _logger.LogWarning(ex, "EHR patient registration simulator tick failed.");
            }
            try { await Task.Delay(_interval, stoppingToken).ConfigureAwait(false); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var patients = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();

        // Full Guid v7 hex (32 chars) — the first 12 hex are the 48-bit millisecond timestamp
        // (changes every tick), the remaining 20 are random.
        var mrn = $"MRN-SIM-{Guid.CreateVersion7():N}";

        var (family, origin) = _families[_rng.Next(_families.Length)];
        var (given, sex) = _given[_rng.Next(_given.Length)];
        var middle = _middleNames[_rng.Next(_middleNames.Length)];

        // Realistic age distribution — dialysis patients skew older.
        // Pull from the last 90 years, biased to 40–80 by rolling twice and taking the older.
        var thisYear = DateTime.UtcNow.Year;
        var rollA = thisYear - _rng.Next(18, 90);
        var rollB = thisYear - _rng.Next(40, 85);
        var year = Math.Min(rollA, rollB);
        var month = _rng.Next(1, 13);
        var maxDay = DateTime.DaysInMonth(year, month);
        var dob = new DateOnly(year, month, _rng.Next(1, maxDay + 1));

        var language = _languages
            .FirstOrDefault(l => l.Origins.Contains(origin))
            .Language ?? "en-US";

        var humanName = string.IsNullOrEmpty(middle)
            ? new HumanName(family, given)
            : new HumanName(family, given, middle);

        var patient = Patient.Register(
            Guid.CreateVersion7(),
            mrn,
            humanName,
            dob,
            sexAtBirthCode: sex,
            preferredLanguageCode: language);
        patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Simulator registered patient {Family}, {Given} ({Mrn}) DOB {Dob} lang {Lang}.",
            family, given, mrn, dob, language);
    }
}
