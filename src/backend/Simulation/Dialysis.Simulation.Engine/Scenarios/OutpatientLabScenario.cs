using System.Text;
using Dialysis.Simulation.Contracts;
using Dialysis.Simulation.Drivers;

namespace Dialysis.Simulation.Engine.Scenarios;

/// <summary>
/// Outpatient Visit → Lab → Invoice → Receipt. Registers a patient, books an appointment, opens an
/// encounter, orders + results a lab, closes the encounter to capture charges, and generates the
/// invoice + receipt documents.
/// </summary>
public sealed class OutpatientLabScenario : IScenario
{
    private static readonly DateTime _slotStart = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>Creates the scenario over the module drivers.</summary>
    public OutpatientLabScenario(IEhrDriver ehr, IHisDriver his, ILabDriver lab, IHieDriver hie)
    {
        ArgumentNullException.ThrowIfNull(ehr);
        ArgumentNullException.ThrowIfNull(his);
        ArgumentNullException.ThrowIfNull(lab);
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

            new ScenarioStep("Book appointment", WorkflowState.AppointmentBooked, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await his.BookAppointmentAsync(
                    new BookAppointmentCommand(patientId, ctx.ProviderId, _slotStart, _slotStart.AddMinutes(30)),
                    ctx.Driver, ct).ConfigureAwait(false);
                ctx.AppointmentId = result.AppointmentId;
                return StepResult.ForRecord("AppointmentCreated", "Appointment", result.AppointmentId, "his", "his.appointment");
            }),

            new ScenarioStep("Start encounter", WorkflowState.EncounterStarted, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await ehr.StartEncounterAsync(
                    new StartEncounterCommand(patientId, ctx.ProviderId, "AMB", ctx.AppointmentId),
                    ctx.Driver, ct).ConfigureAwait(false);
                ctx.EncounterId = result.EncounterId;
                return StepResult.ForRecord("EncounterCreated", "Encounter", result.EncounterId, "ehr", "ehr.encounter");
            }),

            new ScenarioStep("Place lab order", WorkflowState.LabOrdered, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var result = await lab.PlaceLabOrderAsync(
                    new PlaceLabOrderCommand(patientId,
                        [new LabTestRequest("718-7", "Hemoglobin"), new LabTestRequest("2160-0", "Creatinine")],
                        "Serum"),
                    ctx.Driver, ct).ConfigureAwait(false);
                ctx.LabOrderId = result.OrderId;
                ctx.PlacerOrderNumber = result.PlacerOrderNumber;
                return StepResult.ForRecord("LabOrderPlaced", "LabOrder", result.OrderId, "lab", "lab.order", result.PlacerOrderNumber);
            }),

            new ScenarioStep("Publish lab result", WorkflowState.ResultAvailable, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var placer = ScenarioGuards.RequireValue(ctx.PlacerOrderNumber, "placerOrderNumber");
                await lab.PublishResultAsync(
                    new PublishLabResultCommand(placer, patientId,
                        [new LabObservation("718-7", "Hemoglobin", "9.1", "g/dL", "13.5-17.5")]),
                    ctx.Driver, ct).ConfigureAwait(false);
                return StepResult.Marker("LabResultPublished", ScenarioGuards.RequireId(ctx.LabOrderId, "labOrder"), placer);
            }),

            new ScenarioStep("Close encounter & capture charges", WorkflowState.BillingReady, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var encounterId = ScenarioGuards.RequireId(ctx.EncounterId, "encounter");
                var result = await ehr.CloseEncounterAsync(
                    new CloseEncounterCommand(encounterId, patientId, ctx.ProviderId, ["E11.9", "I10"], []),
                    ctx.Driver, ct).ConfigureAwait(false);
                ctx.Charges = result.Charges;
                return StepResult.ForRecord("ChargeCaptured", "Encounter", encounterId, "ehr", "ehr.encounter.closed",
                    $"charges={result.Charges.Count}");
            }),

            new ScenarioStep("Generate invoice & receipt", WorkflowState.DocumentsReady, 2, async (ctx, ct) =>
            {
                var patientId = ScenarioGuards.RequireId(ctx.RealPatientId, "patient");
                var total = ctx.Charges.Sum(c => c.Amount);
                var invoice = await hie.UploadDocumentAsync(
                    new UploadDocumentCommand(patientId, "Invoice", "Outpatient Invoice", "text/plain",
                        Encoding.UTF8.GetBytes($"Invoice for patient {patientId:N} — total {total:0.00}")),
                    ctx.Driver, ct).ConfigureAwait(false);
                await hie.UploadDocumentAsync(
                    new UploadDocumentCommand(patientId, "Receipt", "Payment Receipt", "text/plain",
                        Encoding.UTF8.GetBytes($"Receipt for patient {patientId:N} — paid {total:0.00}")),
                    ctx.Driver, ct).ConfigureAwait(false);
                return StepResult.ForRecord("DocumentGenerated", "Document", invoice.DocumentId, "hie", "hie.document");
            }),

            new ScenarioStep("Complete", WorkflowState.Completed, 0, (ctx, _) =>
                Task.FromResult(StepResult.Marker("SimulationCompleted", ctx.Session.Id))),
        ];
    }

    /// <inheritdoc />
    public string Id => "outpatient-lab";

    /// <inheritdoc />
    public string Name => "Outpatient Visit → Lab → Invoice → Receipt";

    /// <inheritdoc />
    public string Description => "Register a patient, book an appointment, order and result a lab, capture charges, and generate the invoice + receipt.";

    /// <inheritdoc />
    public IReadOnlyList<ScenarioStep> Steps { get; }
}
