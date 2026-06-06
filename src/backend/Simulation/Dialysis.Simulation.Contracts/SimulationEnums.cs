namespace Dialysis.Simulation.Contracts;

/// <summary>
/// Lifecycle of a <c>SimulationSession</c>. A session starts <see cref="Created"/>, moves to
/// <see cref="Running"/> while the engine walks the scenario steps, and ends <see cref="Completed"/>
/// or <see cref="Failed"/>.
/// </summary>
public enum SimulationSessionStatus
{
    /// <summary>Session created; the scenario has not been run yet.</summary>
    Created = 0,

    /// <summary>The engine is executing the scenario steps.</summary>
    Running = 1,

    /// <summary>Every scenario step completed and the workflow reached COMPLETED.</summary>
    Completed = 2,

    /// <summary>A step exhausted its retries; the workflow transitioned to FAILED.</summary>
    Failed = 3,
}

/// <summary>
/// The patient-journey workflow state machine (Brandolini event-storming process states). A scenario
/// declares an ordered subset of edges over these states; not every scenario visits every state.
/// </summary>
public enum WorkflowState
{
    /// <summary>Session created, nothing driven yet.</summary>
    Created = 0,

    /// <summary>Patient registered in the EHR.</summary>
    Registered = 1,

    /// <summary>An appointment was booked in the HIS.</summary>
    AppointmentBooked = 2,

    /// <summary>An encounter/visit was started.</summary>
    EncounterStarted = 3,

    /// <summary>The patient was admitted (inpatient).</summary>
    Admitted = 4,

    /// <summary>The patient is in intensive care.</summary>
    Icu = 5,

    /// <summary>A lab order was placed.</summary>
    LabOrdered = 6,

    /// <summary>A lab result is available.</summary>
    ResultAvailable = 7,

    /// <summary>Charges captured and billing is ready.</summary>
    BillingReady = 8,

    /// <summary>Clinical/billing documents were generated.</summary>
    DocumentsReady = 9,

    /// <summary>The patient was discharged.</summary>
    Discharged = 10,

    /// <summary>The scenario finished successfully.</summary>
    Completed = 11,

    /// <summary>The workflow failed and stopped.</summary>
    Failed = 12,
}
