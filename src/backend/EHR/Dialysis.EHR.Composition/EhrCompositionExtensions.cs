using Dialysis.BuildingBlocks.Fhir;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Billing;
using Dialysis.EHR.Core;
using Dialysis.EHR.Integration;
using Dialysis.EHR.Integration.Adapters;
using Dialysis.EHR.Integration.Consumers;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.Projections;
using Dialysis.EHR.PatientChart;
using Dialysis.EHR.PatientChart.Projections;
using Dialysis.EHR.PatientPortal;
using Dialysis.EHR.Persistence;
using Dialysis.EHR.Registration;
using Dialysis.EHR.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.EHR.Composition;

public static class EhrCompositionExtensions
{
    /// <summary>
    /// Wires the EHR module's domain services, persistence, CQRS, and Transponder consumers.
    /// Cross-cutting (auth, telemetry, OpenAPI, etc.) is added separately via <c>AddModuleHost</c>.
    /// </summary>
    public static IServiceCollection AddElectronicHealthRecord(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxRelay = false,
        bool enableFhirEndpoints = false,
        Action<FhirBuilder>? configureFhir = null,
        Action<IServiceCollection>? configureTransponderTransport = null)
    {
        services.AddEhrCore();
        services.AddEhrPersistence(configurePersistence);

        services.TryAddScoped<IPharmacyGateway, NoopPharmacyGateway>();
        services.TryAddScoped<ILabGateway, NoopLabGateway>();
        services.TryAddScoped<IInsurerGateway, NoopInsurerGateway>();

        services.AddSingleton<VitalSignOpenEhrProjector>();
        services.AddSingleton<LabResultOpenEhrProjector>();

        services.AddTransponder(t =>
        {
            t.AddConsumer<PrescriptionOrderedIntegrationEvent, PrescriptionOrderedConsumer>();
            t.AddConsumer<LabOrderPlacedIntegrationEvent, LabOrderPlacedConsumer>();
            t.AddConsumer<ClaimSubmittedIntegrationEvent, ClaimSubmittedConsumer>();
        });
        configureTransponderTransport?.Invoke(services);

        services.AddCqrs(c =>
        {
            c.AddFromAssembliesOf(
                typeof(EhrRegistrationMarker),
                typeof(EhrPatientChartMarker),
                typeof(EhrSchedulingMarker),
                typeof(EhrPatientPortalMarker),
                typeof(EhrClinicalNotesMarker),
                typeof(EhrBillingMarker),
                typeof(EhrIntegrationMarker));

            EhrCommandRegistrations.RegisterAuthorizationBehaviors(c);
        });

        if (enableOutboxRelay)
            services.AddTransponderOutboxRelay<EhrDbContext>();

        if (enableFhirEndpoints)
        {
            services.AddFhir(fhir =>
            {
                fhir.UseBaseUrl("/fhir");
                configureFhir?.Invoke(fhir);
            });
        }

        return services;
    }
}
