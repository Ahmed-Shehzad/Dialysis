using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Drivers;
using Dialysis.Simulation.Engine.Documents;

namespace Dialysis.Simulation.Engine.Scenarios;

/// <summary>
/// Inpatient Surgery → Procedure → Billing → Discharge Summary. Admits the patient, escalates to ICU
/// (driven as a second admission, since the modules expose no transfer endpoint), opens an inpatient
/// encounter, captures procedure charges, generates the discharge summary, and discharges.
/// </summary>
public sealed class InpatientSurgeryScenario : IScenario
{
    /// <summary>Creates the scenario over the module drivers and the PDF renderer.</summary>
    public InpatientSurgeryScenario(IEhrDriver ehr, IHisDriver his, IHieDriver hie, IPdfDocumentRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(ehr);
        ArgumentNullException.ThrowIfNull(his);
        ArgumentNullException.ThrowIfNull(hie);
        ArgumentNullException.ThrowIfNull(renderer);

        Steps =
        [
            new ScenarioStep("Register patient", WorkflowState.Registered, 2, async (ctx, ct) =>
            {
                var j = ctx.Session.PatientJourney;
                var result = await ehr.RegisterPatientAsync(
                    new RegisterPatientCommand(j.MedicalRecordNumber, j.FamilyName, j.GivenName, j.DateOfBirth, j.SexAtBirthCode),
                    ctx.Driver, ct).ConfigureAwait(false);
                ctx.RealPatientId = result.PatientId;
                j.LinkRealPatient(result.PatientId);
                return StepResult.ForRecord("PatientRegistered", "Patient", result.PatientId, "ehr", "ehr.patient");
            }),

            new ScenarioStep("Admit patient", WorkflowState.Admitted, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await his.AdmitPatientAsync(new AdmitPatientCommand(patientId, "MED"), ctx.Driver, ct).ConfigureAwait(false);
                ctx.AdmissionId = result.AdmissionId;
                return StepResult.ForRecord("PatientAdmitted", "Admission", result.AdmissionId, "his", "his.admission");
            }),

            new ScenarioStep("Escalate to ICU", WorkflowState.Icu, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await his.AdmitPatientAsync(new AdmitPatientCommand(patientId, "ICU"), ctx.Driver, ct).ConfigureAwait(false);
                ctx.AdmissionId = result.AdmissionId;
                return StepResult.ForRecord("PatientTransferred", "Admission", result.AdmissionId, "his", "his.admission.icu");
            }),

            new ScenarioStep("Start encounter", WorkflowState.EncounterStarted, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await ehr.StartEncounterAsync(
                    new StartEncounterCommand(patientId, ctx.ProviderId, "IMP", null), ctx.Driver, ct).ConfigureAwait(false);
                ctx.EncounterId = result.EncounterId;
                return StepResult.ForRecord("EncounterCreated", "Encounter", result.EncounterId, "ehr", "ehr.encounter");
            }),

            new ScenarioStep("Close encounter & capture procedure", WorkflowState.BillingReady, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var encounterId = ScenarioGuards.RequireId(ctx.EncounterId, "encounter");
                var result = await ehr.CloseEncounterAsync(
                    new CloseEncounterCommand(encounterId, patientId, ctx.ProviderId, ["K80.20"], ["47562"]),
                    ctx.Driver, ct).ConfigureAwait(false);
                ctx.Charges = result.Charges;
                return StepResult.ForRecord("ChargeCaptured", "Encounter", encounterId, "ehr", "ehr.encounter.closed",
                    $"charges={result.Charges.Count}");
            }),

            new ScenarioStep("Generate discharge summary", WorkflowState.DocumentsReady, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var bytes = await SimulationDocumentFactory.RenderAsync(renderer, ctx, "DischargeSummary", "Discharge Summary",
                    [new("Patient", patientId.ToString("N")), new("Diagnosis", "K80.20"), new("Procedure", "47562")], ct).ConfigureAwait(false);
                var doc = await hie.UploadDocumentAsync(
                    new UploadDocumentCommand(patientId, "DischargeSummary", "Discharge Summary", "application/pdf", bytes),
                    ctx.Driver, ct).ConfigureAwait(false);
                return StepResult.ForRecord("DocumentGenerated", "Document", doc.DocumentId, "hie", "hie.document");
            }),

            new ScenarioStep("Discharge patient", WorkflowState.Discharged, 2, async (ctx, ct) =>
            {
                var admissionId = ScenarioGuards.RequireId(ctx.AdmissionId, "admission");
                var result = await his.DischargePatientAsync(new DischargePatientCommand(admissionId), ctx.Driver, ct).ConfigureAwait(false);
                return StepResult.ForRecord("PatientDischarged", "Admission", result.AdmissionId, "his", "his.admission.discharged");
            }),

            new ScenarioStep("Complete", WorkflowState.Completed, 0, (ctx, _) =>
                Task.FromResult(StepResult.Marker("SimulationCompleted", ctx.Session.Id))),
        ];
    }

    /// <inheritdoc />
    public string Id => "inpatient-surgery";

    /// <inheritdoc />
    public string Name => "Inpatient Surgery → Procedure → Billing → Discharge Summary";

    /// <inheritdoc />
    public string Description => "Admit, escalate to ICU, perform an inpatient encounter with a procedure, capture charges, generate the discharge summary, and discharge.";

    /// <inheritdoc />
    public IReadOnlyList<ScenarioStep> Steps { get; }
}
