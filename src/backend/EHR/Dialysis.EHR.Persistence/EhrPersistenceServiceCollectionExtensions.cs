using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.PatientChart.Ports;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.EHR.Persistence.Stores;
using Dialysis.EHR.Registration.Ports;
using Dialysis.EHR.Scheduling.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.EHR.Persistence;

public static class EhrPersistenceServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the EHR <see cref="EhrDbContext"/> (PostgreSQL by default), <see cref="IUnitOfWork"/>,
        /// Transponder outbox/inbox, and every repository for the seven EHR bounded contexts.
        /// </summary>
        public IServiceCollection AddEhrPersistence(
            Action<DbContextOptionsBuilder>? configure = null)
        {
            services.AddOptions<TransponderPersistenceOptions>()
                .Configure(o => o.Schema = "ehr");

            services.AddDbContext<EhrDbContext>((sp, options) =>
            {
                if (configure is not null)
                {
                    configure(options);
                }

                var interceptor = sp.GetService<AuditSaveChangesInterceptor>();
                if (interceptor is not null)
                    options.AddInterceptors(interceptor);

                var integrationEventOutbox = sp.GetService<IntegrationEventOutboxSaveChangesInterceptor>();
                if (integrationEventOutbox is not null)
                    options.AddInterceptors(integrationEventOutbox);
            });

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EhrDbContext>());
            services.AddTransponderEfOutboxAndInbox<EhrDbContext>();
            services.AddModuleIntegrationEventOutbox();
            services.AddScoped<Dialysis.BuildingBlocks.DataProtection.Erasure.IPatientEraser,
                Dialysis.EHR.Persistence.Erasure.EhrPatientEraser>();

            // Registration
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IProviderRepository, ProviderRepository>();
            services.AddScoped<ICareTeamRepository, CareTeamRepository>();

            // PatientChart
            services.AddScoped<IAllergyRepository, AllergyRepository>();
            services.AddScoped<IProblemListRepository, ProblemListRepository>();
            services.AddScoped<IVitalSignRepository, VitalSignRepository>();
            services.AddScoped<IImmunizationRepository, ImmunizationRepository>();
            services.AddScoped<IMedicationStatementRepository, MedicationStatementRepository>();
            services.AddScoped<ICarePlanRepository, CarePlanRepository>();

            // Scheduling
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IProviderAvailabilityRepository, ProviderAvailabilityRepository>();

            // Portal
            services.AddScoped<IPortalAppointmentRequestRepository, PortalAppointmentRequestRepository>();
            services.AddScoped<ISecureMessageRepository, SecureMessageRepository>();

            // ClinicalNotes
            services.AddScoped<IEncounterRepository, EncounterRepository>();
            services.AddScoped<IClinicalNoteRepository, ClinicalNoteRepository>();
            services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
            services.AddScoped<ILabOrderRepository, LabOrderRepository>();
            services.AddScoped<ILabResultRepository, LabResultRepository>();
            services.AddScoped<IImagingOrderRepository, ImagingOrderRepository>();
            services.AddScoped<IReferralRepository, ReferralRepository>();

            // Billing
            services.AddScoped<IPayerRepository, PayerRepository>();
            services.AddScoped<IChargeRepository, ChargeRepository>();
            services.AddScoped<IBillableEncounterRepository, EfBillableEncounterRepository>();
            services.AddScoped<IClaimRepository, ClaimRepository>();
            services.AddScoped<IRemittanceRepository, RemittanceRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            // Integration
            services.AddScoped<IHospitalEventRepository, HospitalEventRepository>();
            services.AddScoped<IPharmacyTransmissionRepository, PharmacyTransmissionRepository>();
            services.AddScoped<ILabTransmissionRepository, LabTransmissionRepository>();
            services.AddScoped<IInsurerTransmissionRepository, InsurerTransmissionRepository>();

            return services;
        }
    }
}
