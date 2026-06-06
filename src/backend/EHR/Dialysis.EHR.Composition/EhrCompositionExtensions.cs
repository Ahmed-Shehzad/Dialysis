using Dialysis.BuildingBlocks.ClinicianNotification;
using Dialysis.BuildingBlocks.Fhir;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.DeIdentification;
using Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;
using Dialysis.EHR.PatientChart.Fhir;
using Dialysis.EHR.Registration.Fhir;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.BuildingBlocks.DataProtection;
using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.EHR.Billing;
using Dialysis.EHR.Billing.Consumers;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Persistence.Billing;
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
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Wires the EHR module's domain services, persistence, CQRS, and Transponder consumers.
        /// Cross-cutting (auth, telemetry, OpenAPI, etc.) is added separately via <c>AddModuleHost</c>.
        /// </summary>
        public IServiceCollection AddElectronicHealthRecord(
            IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configurePersistence = null,
            bool enableOutboxRelay = false,
            bool enableFhirEndpoints = false,
            bool enableFhirAuditPersistence = false,
            bool enableFhirBulkDataPersistence = false,
            bool enableFhirBulkDataExport = false,
            bool enableFhirSmartOnFhir = false,
            bool enableFhirSubscriptionsPersistence = false,
            bool enableFhirSubscriptions = false,
            Action<FhirBuilder>? configureFhir = null,
            Action<IServiceCollection>? configureTransponderTransport = null)
        {
            services.AddEhrCore();
            services.AddEhrPersistence(configurePersistence);

            // Point-of-care clinical safety checks (medication↔allergy, duplicate medication / lab) run
            // against the patient's own chart at order entry. Deterministic and in-context.
            services.Configure<ClinicalSafetyOptions>(configuration.GetSection(ClinicalSafetyOptions.SectionName));
            services.AddScoped<IClinicalSafetyChecker, ClinicalSafetyChecker>();

            // Quality / MIPS care-gap prompts — config-driven, empty by default.
            services.Configure<Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.QualityMeasureOptions>(
                configuration.GetSection(Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.QualityMeasureOptions.SectionName));
            services.AddScoped<Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.IQualityMeasureEvaluator,
                Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.QualityMeasureEvaluator>();

            // Point-of-care clinical decision support — condition-specific prompts, config-driven, empty by default.
            services.Configure<Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport.CdsOptions>(
                configuration.GetSection(Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport.CdsOptions.SectionName));
            services.AddScoped<Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport.IClinicalDecisionSupportEvaluator,
                Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport.ClinicalDecisionSupportEvaluator>();

            // Population condition-control measures + at-risk outreach — config-driven, dispatch off by default.
            services.Configure<Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.ControlMeasureOptions>(
                configuration.GetSection(Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.ControlMeasureOptions.SectionName));
            services.Configure<Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.OutreachOptions>(
                configuration.GetSection(Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.OutreachOptions.SectionName));
            services.AddScoped<Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.IConditionControlEvaluator,
                Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.ConditionControlEvaluator>();
            services.AddScoped<Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.IOutreachContactResolver,
                Dialysis.EHR.ClinicalNotes.Features.QualityMeasures.ConfiguredFallbackOutreachContactResolver>();
            services.AddClinicianNotification();

            // Revenue cycle: auto-capture professional charges on encounter close (opt-in, default off).
            services.Configure<EncounterChargeAutomationOptions>(
                configuration.GetSection(EncounterChargeAutomationOptions.SectionName));

            // E/M coding assist — suggests the visit level + flags under-coding. Config-driven, empty → no suggestion.
            services.Configure<Dialysis.EHR.Billing.Coding.EmCodingOptions>(
                configuration.GetSection(Dialysis.EHR.Billing.Coding.EmCodingOptions.SectionName));
            services.AddScoped<Dialysis.EHR.Billing.Coding.IEvaluationManagementCoder,
                Dialysis.EHR.Billing.Coding.EvaluationManagementCoder>();

            // Charge-review edits (frequency / coverage / ABN / under-coding) — config-driven, empty by default.
            services.Configure<Dialysis.EHR.Billing.ChargeEdits.ChargeEditOptions>(
                configuration.GetSection(Dialysis.EHR.Billing.ChargeEdits.ChargeEditOptions.SectionName));
            services.AddScoped<Dialysis.EHR.Billing.ChargeEdits.IChargeEditChecker,
                Dialysis.EHR.Billing.ChargeEdits.ChargeEditChecker>();

            services.AddEuDataProtection("ehr", registry =>
            {
                registry.RegisterActivity(
                    activityName: "ehr.chart.read",
                    basis: LawfulBasis.HealthcareProvision,
                    categories: DataCategory.Identifying | DataCategory.ClinicalHealth,
                    purpose: "Display a patient's clinical chart to authorised clinicians.",
                    retentionKey: "clinical.record",
                    recipientCategories: ["Treating clinicians"]);
                registry.RegisterActivity(
                    activityName: "ehr.billing.charge.capture",
                    basis: LawfulBasis.Contract,
                    categories: DataCategory.Identifying | DataCategory.Financial,
                    purpose: "Capture per-session billable charges following CMS / KBV coding rules.",
                    retentionKey: "billing.record",
                    recipientCategories: ["Payers (via clearinghouse)"]);
                registry.RegisterActivity(
                    activityName: "ehr.billing.claim.submit",
                    basis: LawfulBasis.Contract,
                    categories: DataCategory.Identifying | DataCategory.Financial,
                    purpose: "Submit ANSI ASC X12N 837P claims to the payer clearinghouse.",
                    retentionKey: "billing.record",
                    recipientCategories: ["Clearinghouse", "Payer"]);
                registry.RegisterActivity(
                    activityName: "ehr.billing.ack.receive",
                    basis: LawfulBasis.Contract,
                    categories: DataCategory.Financial | DataCategory.Operational,
                    purpose: "Receive 999 / 277CA acknowledgements from the clearinghouse and advance the claim state machine.",
                    retentionKey: "billing.record");
            });

            services.TryAddScoped<IPharmacyGateway, NoopPharmacyGateway>();
            services.TryAddScoped<ILabGateway, NoopLabGateway>();
            services.TryAddScoped<IInsurerGateway, NoopInsurerGateway>();

            services.AddSingleton<VitalSignOpenEhrProjector>();
            services.AddSingleton<LabResultOpenEhrProjector>();

            services.AddTransponder(t =>
            {
                t.AddConsumer<PrescriptionOrderedIntegrationEvent, PrescriptionOrderedConsumer>();
                t.AddConsumer<LabOrderPlacedIntegrationEvent, LabOrderPlacedConsumer>();
                // Imaging study correlated back to its order (by accession) → link + complete it.
                t.AddConsumer<ImagingStudyLinkedIntegrationEvent, ImagingStudyLinkedConsumer>();
                // Advisory AI imaging finding → attach to the order, pending clinician sign-off.
                t.AddConsumer<ImagingAiFindingProducedIntegrationEvent, ImagingAiFindingProducedConsumer>();
                t.AddConsumer<ClaimSubmittedIntegrationEvent, ClaimSubmittedConsumer>();
                // Cross-module: PDMS completes a session → capture the itemised dialysis charge
                // and emit the invoice-ready event that HIE Documents renders into an AcroForm PDF.
                t.AddConsumer<DialysisSessionChargeReadyIntegrationEvent, DialysisSessionChargeReadyConsumer>();
                // ClinicalNotes closes an encounter → auto-capture one Charge per procedure CPT
                // (gated by Ehr:Billing:EncounterChargeAutomation:Enabled, default off).
                t.AddConsumer<EncounterClosedIntegrationEvent, EncounterClosedChargeConsumer>();
                // Revenue-cycle worklist read model: record every closed encounter, and flip its
                // HasCharge flag when a charge for it is captured (always on).
                t.AddConsumer<EncounterClosedIntegrationEvent, EncounterClosedBillableProjector>();
                t.AddConsumer<ChargeCapturedIntegrationEvent, ChargeCapturedBillableProjector>();
                // Care coordination: project HIS admit/discharge + HIE external encounters into the
                // hospital-event read model that drives the proactive follow-up worklist.
                t.AddConsumer<PatientAdmittedIntegrationEvent, PatientAdmittedHospitalEventProjector>();
                t.AddConsumer<PatientDischargedIntegrationEvent, PatientDischargedHospitalEventProjector>();
                t.AddConsumer<Dialysis.HIE.Contracts.Integration.ExternalEncounterIngestedIntegrationEvent, ExternalEncounterHospitalEventProjector>();
                // Patient safety: project PDMS intradialytic adverse events into the cross-patient
                // surveillance read model that drives the safety-signal dashboard.
                t.AddConsumer<IntradialyticAdverseEventIntegrationEvent, Dialysis.EHR.Integration.Consumers.AdverseEventProjector>();
                // Cross-module: mirror HIS check-ins so HIS-originated patients exist in EHR.
                t.AddConsumer<PatientCheckedInIntegrationEvent, EhrPatientFromHisCheckInConsumer>();
                t.AddConsumer<WalkInRegisteredIntegrationEvent, EhrPatientFromHisWalkInConsumer>();

                if (enableFhirSubscriptions)
                {
                    t.AddConsumer<LabResultReceivedIntegrationEvent, LabResultReceivedSubscriptionBroadcaster>();
                    t.AddConsumer<ImagingAiFindingProducedIntegrationEvent, ImagingAiFindingSubscriptionBroadcaster>();
                }
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

            // Billing ports — `EHR:Billing:Persistence:Provider` selects between the
            // EF-backed variants (production: persistent across restarts and replicas)
            // and the configurable / in-memory variants (dev / tests). The TryAdd*
            // calls leave operators free to register their own implementations.
            var billingProvider = configuration["EHR:Billing:Persistence:Provider"] ?? "Postgres";
            if (billingProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                services.TryAddSingleton<ICptFeeSchedule, ConfigurableCptFeeSchedule>();
                services.TryAddSingleton<IChargeIdempotencyStore, InMemoryChargeIdempotencyStore>();
                services.TryAddSingleton<ICptFeeScheduleAdminRepository, InMemoryCptFeeScheduleAdminRepository>();
            }
            else
            {
                services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<EhrDbContext>());
                services.TryAddScoped<ICptFeeSchedule, EfCptFeeSchedule>();
                services.TryAddScoped<IChargeIdempotencyStore, EfChargeIdempotencyStore>();
                services.TryAddScoped<ICptFeeScheduleAdminRepository, EfCptFeeScheduleAdminRepository>();
            }

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

            if (enableFhirAuditPersistence)
                services.AddFhirAuditEntityFrameworkStore<EhrDbContext>();
            if (enableFhirBulkDataPersistence)
                services.AddFhirBulkDataEntityFrameworkStore<EhrDbContext>();
            if (enableFhirSubscriptionsPersistence)
                services.AddFhirSubscriptionsEntityFrameworkStore<EhrDbContext>();

            if (enableFhirBulkDataExport)
            {
                var storageRoot = configuration["Ehr:Fhir:BulkData:StorageRoot"]
                    ?? Path.Combine(Path.GetTempPath(), "dialysis-ehr-bulk-data");
                services.AddFhirBulkData(storageRoot);
                services.AddFhirBulkDataOrchestrator();
                // PHI-safe analytics export: the Safe Harbor de-identifier the export runner applies
                // when a job is requested with _deIdentify (fail-closed if missing).
                services.AddFhirDeIdentification();
                services.AddFhirBulkDataFeeder<EhrPatientFhirFeeder, Hl7.Fhir.Model.Patient>();
                services.AddFhirBulkDataFeeder<EhrVitalSignObservationFeeder, Hl7.Fhir.Model.Observation>();
                services.AddFhirBulkDataFeeder<EhrAllergyIntoleranceFeeder, Hl7.Fhir.Model.AllergyIntolerance>();
                services.AddFhirBulkDataFeeder<EhrImmunizationFeeder, Hl7.Fhir.Model.Immunization>();
                services.AddFhirBulkDataFeeder<EhrMedicationStatementFeeder, Hl7.Fhir.Model.MedicationStatement>();
                services.AddFhirBulkDataFeeder<EhrCarePlanFeeder, Hl7.Fhir.Model.CarePlan>();
            }

            if (enableFhirSmartOnFhir)
            {
                services.AddFhirSmartOnFhir(configuration.GetSection("Ehr:Fhir:Smart"));
            }

            if (enableFhirSubscriptions)
            {
                services.AddFhirSubscriptions(topics => topics.Add(new SubscriptionTopicDescriptor(
                    Url: LabResultReceivedSubscriptionBroadcaster.TopicUrl,
                    Title: "Lab result received",
                    Description: "Fires when a lab result is received for an EHR patient. Filter by patient, LOINC code, or abnormal flag.",
                    FilterParameterNames: ["patient", "code", "abnormal"])));
            }

            return services;
        }
    }
}
