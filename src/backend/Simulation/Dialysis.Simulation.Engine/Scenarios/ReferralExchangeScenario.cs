using System.Text;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Drivers;

namespace Dialysis.Simulation.Engine.Scenarios;

/// <summary>
/// Referral Exchange → HIE Document Sharing. Registers a patient, opens an encounter, requests a
/// referral to a partner organization (which drives the real HIE care-summary push), and generates the
/// referral packet document.
/// </summary>
public sealed class ReferralExchangeScenario : IScenario
{
    /// <summary>Creates the scenario over the module drivers.</summary>
    public ReferralExchangeScenario(IEhrDriver ehr, IHieDriver hie)
    {
        ArgumentNullException.ThrowIfNull(ehr);
        ArgumentNullException.ThrowIfNull(hie);

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

            new ScenarioStep("Start encounter", WorkflowState.EncounterStarted, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await ehr.StartEncounterAsync(
                    new StartEncounterCommand(patientId, ctx.ProviderId, "AMB", null), ctx.Driver, ct).ConfigureAwait(false);
                ctx.EncounterId = result.EncounterId;
                return StepResult.ForRecord("EncounterCreated", "Encounter", result.EncounterId, "ehr", "ehr.encounter");
            }),

            new ScenarioStep("Request referral", WorkflowState.EncounterStarted, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await ehr.RequestReferralAsync(
                    new RequestReferralCommand(patientId, "partner-hospital", ctx.ProviderId, "Specialist evaluation"),
                    ctx.Driver, ct).ConfigureAwait(false);
                return StepResult.ForRecord("ReferralCreated", "Referral", result.ReferralId, "ehr", "ehr.referral");
            }),

            new ScenarioStep("Share referral packet", WorkflowState.DocumentsReady, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var doc = await hie.UploadDocumentAsync(
                    new UploadDocumentCommand(patientId, "Referral", "Referral Packet", "text/plain",
                        Encoding.UTF8.GetBytes($"Referral packet for patient {patientId:N}")),
                    ctx.Driver, ct).ConfigureAwait(false);
                return StepResult.ForRecord("DocumentShared", "Document", doc.DocumentId, "hie", "hie.document");
            }),

            new ScenarioStep("Complete", WorkflowState.Completed, 0, (ctx, _) =>
                Task.FromResult(StepResult.Marker("SimulationCompleted", ctx.Session.Id))),
        ];
    }

    /// <inheritdoc />
    public string Id => "referral-exchange";

    /// <inheritdoc />
    public string Name => "Referral Exchange → HIE Document Sharing";

    /// <inheritdoc />
    public string Description => "Register a patient, open an encounter, request a partner referral, and share the referral packet through the HIE.";

    /// <inheritdoc />
    public IReadOnlyList<ScenarioStep> Steps { get; }
}
