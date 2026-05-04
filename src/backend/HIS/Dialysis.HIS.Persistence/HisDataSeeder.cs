using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIS.Persistence;

internal static class HisDataSeeder
{
    public static async Task EnsureSchedulingResourcesAsync(HisDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.SchedulingResources.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        db.SchedulingResources.AddRange(
            new SchedulingResource
            {
                Id = HisSeed.SchedulingResourceRoomDialysis1,
                KindCode = "room",
                DisplayName = "Dialysis bay 1",
                IsBookable = true,
            },
            new SchedulingResource
            {
                Id = HisSeed.SchedulingResourceEquipmentScale,
                KindCode = "equipment",
                DisplayName = "Patient scale A",
                IsBookable = true,
            },
            new SchedulingResource
            {
                Id = HisSeed.SchedulingResourceStaffNurseSlot,
                KindCode = "staff",
                DisplayName = "Nurse slot — morning",
                IsBookable = true,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded default {Count} scheduling resources.", 3);
    }

    public static async Task EnsureRaCapabilitySamplesAsync(HisDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.RaOrgCommunications.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var utc = DateTime.UtcNow;
        db.RaOrgCommunications.Add(
            new RaOrgCommunication
            {
                Id = HisSeed.RaDemoOrgMessageId,
                ThreadCode = "ward-handoff",
                Subject = "Night shift handoff",
                Body = "Demo organizational communication (Generic MIS, RA Fig. 6).",
                SentAtUtc = utc,
            });
        db.RaQualityWorkflowTasks.Add(
            new RaQualityWorkflowTask
            {
                Id = HisSeed.RaDemoQualityTaskId,
                TaskCode = "incident-review",
                Title = "Review dialysis catheter checklist",
                StatusCode = "open",
                OpenedAtUtc = utc,
            });
        db.RaFinancialErpLinks.Add(
            new RaFinancialErpLink
            {
                Id = HisSeed.RaDemoFinancialLinkId,
                SystemCode = "erp-demo",
                LastHandshakeAtUtc = utc,
                StatusCode = "connected",
            });
        db.RaWaitlistEntries.Add(
            new RaWaitlistEntry
            {
                Id = HisSeed.RaDemoWaitlistId,
                PatientId = HisSeed.RaDemoPatientId,
                ResourceKindCode = "room",
                Notes = "Demo waitlist entry (Planning & scheduling).",
                RequestedNotBeforeUtc = utc.Date,
                EnqueuedAtUtc = utc,
            });
        db.RaEhrDocumentExchangeRecords.Add(
            new RaEhrDocumentExchangeRecord
            {
                Id = HisSeed.RaDemoEhrDocId,
                PatientId = HisSeed.RaDemoPatientId,
                DocumentTypeCode = "summary",
                ExternalSystemCode = "ehr-stub",
                ExternalUri = "urn:his:demo:document:1",
                ExchangedAtUtc = utc,
            });
        db.RaPatientAlerts.Add(
            new RaPatientAlert
            {
                Id = HisSeed.RaDemoAlertId,
                PatientId = HisSeed.RaDemoPatientId,
                RuleCode = "bp-threshold",
                Severity = "medium",
                Message = "Demo rules-based alert (Patient monitoring).",
                RaisedAtUtc = utc,
                ClearedAtUtc = null,
            });
        db.RaMedicationDispensingRecords.Add(
            new RaMedicationDispensingRecord
            {
                Id = HisSeed.RaDemoDispensingId,
                MedicationOrderId = HisSeed.RaDemoMedicationOrderId,
                BarcodeToken = "DEMO-BARCODE-001",
                DispensedAtUtc = utc,
            });
        db.RaClinicalDecisionSupportEvaluations.Add(
            new RaClinicalDecisionSupportEvaluation
            {
                Id = HisSeed.RaDemoCdsId,
                PatientId = HisSeed.RaDemoPatientId,
                ChecksAppliedJson = """{"interactionsChecked":0,"demo":true}""",
                SafeToProceed = true,
                EvaluatedAtUtc = utc,
            });
        db.RaAnalyticsExportJobs.Add(
            new RaAnalyticsExportJob
            {
                Id = HisSeed.RaDemoAnalyticsJobId,
                PipelineCode = "quality-dashboard",
                RequestedAtUtc = utc,
                StatusCode = "queued",
            });
        db.RaFullTextSearchEntries.Add(
            new RaFullTextSearchEntry
            {
                Id = HisSeed.RaDemoFullTextId,
                CorpusCode = "patients",
                ExternalId = "demo-1",
                SearchText = "dialysis patient demo corpus entry",
                IndexedAtUtc = utc,
            });
        db.RaSecurityMechanismHardenings.Add(
            new RaSecurityMechanismHardening
            {
                Id = HisSeed.RaDemoSecurityMechId,
                MechanismCode = "password-policy",
                AppliedLevel = "development-stub",
                Notes = "Demo security mechanisms record; replace with IdP-backed policy in production.",
                AssessedAtUtc = utc,
            });
        db.RaSpecialistEncounterRecords.Add(
            new RaSpecialistEncounterRecord
            {
                Id = HisSeed.RaDemoSpecialistEncounterId,
                PatientId = HisSeed.RaDemoPatientId,
                SpecialtyCode = "nephrology",
                ExternalSystemCode = "referral-hub-stub",
                Summary = "Demo specialist encounter log (RA row 18).",
                RecordedAtUtc = utc,
            });
        db.RaResearchEducationActivities.Add(
            new RaResearchEducationActivity
            {
                Id = HisSeed.RaDemoResearchEducationActivityId,
                ActivityKindCode = "education",
                Title = "Demo staff training completion",
                ExternalReference = "urn:his:demo:education:1",
                RecordedAtUtc = utc,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded RA capability demo rows (schema his_ra).");
    }
}
