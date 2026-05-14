using Dialysis.BuildingBlocks.Fhir;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.CQRS;
using Dialysis.Module.Hosting.Pipeline;
using Dialysis.PDMS.Core;
using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions;
using Dialysis.PDMS.TreatmentSessions.Features.AbortSession;
using Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;
using Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;
using Dialysis.PDMS.TreatmentSessions.Features.RecordReading;
using Dialysis.PDMS.TreatmentSessions.Features.ScheduleSession;
using Dialysis.PDMS.TreatmentSessions.Features.StartSession;
using Dialysis.PDMS.TreatmentSessions.Projections;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.PDMS.Composition;

public static class PdmsCompositionExtensions
{
    public static IServiceCollection AddPatientDataManagementSystem(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxRelay = false,
        bool enableFhirEndpoints = false,
        Action<FhirBuilder>? configureFhir = null,
        Action<IServiceCollection>? configureTransponderTransport = null)
    {
        services.AddPdmsCore();
        services.AddPdmsPersistence(configurePersistence);

        services.AddSingleton<HaemodialysisSessionOpenEhrProjector>();

        services.AddTransponder(t =>
        {
            t.AddConsumer<DialysisMachineTreatmentSnapshotIntegrationEvent, TreatmentSnapshotConsumer>();
            t.AddConsumer<DialysisMachineAlarmIntegrationEvent, TreatmentAlarmConsumer>();
        });
        configureTransponderTransport?.Invoke(services);

        services.AddCqrs(c =>
        {
            c.AddFromAssembliesOf(typeof(PdmsTreatmentSessionsMarker));

            c.AddCommandBehavior<ScheduleSessionCommand, Guid, AuthorizationPipelineBehavior<ScheduleSessionCommand, Guid>>();
            c.AddCommandBehavior<StartSessionCommand, Unit, AuthorizationPipelineBehavior<StartSessionCommand, Unit>>();
            c.AddCommandBehavior<RecordReadingCommand, Guid, AuthorizationPipelineBehavior<RecordReadingCommand, Guid>>();
            c.AddCommandBehavior<CompleteSessionCommand, Unit, AuthorizationPipelineBehavior<CompleteSessionCommand, Unit>>();
            c.AddCommandBehavior<AbortSessionCommand, Unit, AuthorizationPipelineBehavior<AbortSessionCommand, Unit>>();
        });

        if (enableOutboxRelay)
            services.AddTransponderOutboxRelay<PdmsDbContext>();

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
