using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;
using Dialysis.EHR.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;
using Dialysis.Module.Contracts.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.EHR.Composition.Demo;

/// <summary>
/// Development-only seeder. Populates the EHR DB with a small set of representative patients,
/// problems, allergies, medications, vitals and immunizations so the SPA demo has something
/// to render. Runs only when <c>Ehr:Demo:Enabled=true</c>. Idempotent — re-running is a no-op
/// once the well-known demo MRNs are present.
/// </summary>
public sealed class EhrDemoSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EhrDemoSeeder> _logger;
    /// <summary>
    /// Development-only seeder. Populates the EHR DB with a small set of representative patients,
    /// problems, allergies, medications, vitals and immunizations so the SPA demo has something
    /// to render. Runs only when <c>Ehr:Demo:Enabled=true</c>. Idempotent — re-running is a no-op
    /// once the well-known demo MRNs are present.
    /// </summary>
    public EhrDemoSeeder(IServiceProvider services, ILogger<EhrDemoSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Stable demo provider id surfaced to the SPA as the authoring provider for notes / encounters.
    /// Forwards to the shared <see cref="DemoDataCatalog"/> so every module agrees on the value.
    /// </summary>
    public static Guid DemoProviderId => DemoDataCatalog.DemoProviderId;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();

        if (!await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("EHR demo seeder: database not reachable, skipping.");
            return;
        }

        try
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EHR demo seeder: migrations failed, attempting seed against existing schema.");
        }

        var patients = scope.ServiceProvider.GetRequiredService<IPatientRepository>();
        var providers = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
        var allergies = scope.ServiceProvider.GetRequiredService<IAllergyRepository>();
        var problems = scope.ServiceProvider.GetRequiredService<IProblemListRepository>();
        var meds = scope.ServiceProvider.GetRequiredService<IMedicationStatementRepository>();
        var vitals = scope.ServiceProvider.GetRequiredService<IVitalSignRepository>();

        // Well-known demo provider. The Add Note dialog on the EHR chart uses this id as
        // the authoring provider until real auth-claim → provider-id mapping lands. Stable
        // across restarts so existing notes / encounters keep their author.
        var demoProvider = await providers
            .FindByNpiAsync(DemoDataCatalog.DemoProviderNpi, cancellationToken)
            .ConfigureAwait(false);
        if (demoProvider is null)
        {
            providers.Add(Provider.Register(
                DemoDataCatalog.DemoProviderId,
                DemoDataCatalog.DemoProviderNpi,
                new HumanName("Demo", "Provider"),
                ProviderKind.Physician,
                specialtyCode: "163WN0300X",
                licenseNumber: null));
        }

        var demoPatients = DemoDataCatalog.Patients;
        var existing = await db.Patients
            .Where(p => demoPatients.Select(d => d.Mrn).Contains(p.MedicalRecordNumber))
            .Select(p => p.MedicalRecordNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var seededAny = demoProvider is null;
        foreach (var demo in demoPatients)
        {
            if (existing.Contains(demo.Mrn)) continue;

            var patient = Patient.Register(
                demo.Id,
                demo.Mrn,
                new HumanName(demo.Family, demo.Given),
                demo.Dob,
                demo.Sex,
                preferredLanguageCode: demo.Language);
            patients.Add(patient);

            problems.Add(ProblemListItem.Record(
                Guid.CreateVersion7(), patient.Id,
                new Coding("http://hl7.org/fhir/sid/icd-10-cm", "N18.6", "End stage renal disease"),
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3))));

            allergies.Add(Allergy.Record(
                Guid.CreateVersion7(), patient.Id,
                new Coding("http://www.nlm.nih.gov/research/umls/rxnorm", "7980", "Penicillin G"),
                AllergySeverity.Moderate, AllergyVerificationStatus.Confirmed,
                "Rash"));

            meds.Add(MedicationStatement.Record(
                Guid.CreateVersion7(), patient.Id,
                new Coding("http://www.nlm.nih.gov/research/umls/rxnorm", "29046", "Lisinopril 10 MG"),
                "10 mg", "Once daily",
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6))));

            vitals.Add(VitalSignReading.Record(
                Guid.CreateVersion7(), patient.Id,
                new Coding("http://loinc.org", "8480-6", "Systolic blood pressure"),
                152m, "mm[Hg]",
                DateTime.UtcNow.AddDays(-2)));
            vitals.Add(VitalSignReading.Record(
                Guid.CreateVersion7(), patient.Id,
                new Coding("http://loinc.org", "8462-4", "Diastolic blood pressure"),
                92m, "mm[Hg]",
                DateTime.UtcNow.AddDays(-2)));

            seededAny = true;
        }

        if (seededAny)
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("EHR demo seeder: seeded {Count} demo patients.", demoPatients.Count - existing.Count);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
