using Dialysis.Simulation.Contracts;

namespace Dialysis.Simulation.Drivers.InMemory;

/// <summary>
/// In-memory <see cref="IEhrDriver"/> that returns deterministic ids derived from the call lineage,
/// so a scenario runs end-to-end without the Aspire stack. The HTTP driver (live mode) replaces this.
/// </summary>
public sealed class InMemoryEhrDriver : IEhrDriver
{
    /// <inheritdoc />
    public Task<RegisteredPatient> RegisterPatientAsync(RegisterPatientCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var id = DeterministicGuid.From($"{context.CorrelationId}:ehr.patient:{command.MedicalRecordNumber}");
        return Task.FromResult(new RegisteredPatient(id));
    }

    /// <inheritdoc />
    public Task<StartedEncounter> StartEncounterAsync(StartEncounterCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var id = DeterministicGuid.From($"{context.CorrelationId}:ehr.encounter:{command.PatientId:N}");
        return Task.FromResult(new StartedEncounter(id));
    }

    /// <inheritdoc />
    public Task<ClosedEncounter> CloseEncounterAsync(CloseEncounterCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var charges = new List<CapturedCharge>();
        foreach (var cpt in command.ProcedureCptCodes)
            charges.Add(new CapturedCharge(cpt, 150.00m, $"Procedure {cpt}"));
        // One evaluation-and-management charge per encounter even with no procedure.
        if (charges.Count == 0)
            charges.Add(new CapturedCharge("99213", 95.00m, "Office/outpatient visit"));
        return Task.FromResult(new ClosedEncounter(command.EncounterId, charges));
    }

    /// <inheritdoc />
    public Task<RequestedReferral> RequestReferralAsync(RequestReferralCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var id = DeterministicGuid.From($"{context.CorrelationId}:ehr.referral:{command.PatientId:N}:{command.DestinationPartnerId}");
        return Task.FromResult(new RequestedReferral(id));
    }
}

/// <summary>In-memory <see cref="IHisDriver"/> returning deterministic ids.</summary>
public sealed class InMemoryHisDriver : IHisDriver
{
    /// <inheritdoc />
    public Task<BookedAppointment> BookAppointmentAsync(BookAppointmentCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var id = DeterministicGuid.From($"{context.CorrelationId}:his.appointment:{command.PatientId:N}");
        return Task.FromResult(new BookedAppointment(id));
    }

    /// <inheritdoc />
    public Task<AdmittedPatient> AdmitPatientAsync(AdmitPatientCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var id = DeterministicGuid.From($"{context.CorrelationId}:his.admission:{command.PatientId:N}:{command.WardCode}");
        return Task.FromResult(new AdmittedPatient(id));
    }

    /// <inheritdoc />
    public Task<DischargedPatient> DischargePatientAsync(DischargePatientCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(new DischargedPatient(command.AdmissionId));
    }
}

/// <summary>In-memory <see cref="ILabDriver"/> returning deterministic ids + placer order numbers.</summary>
public sealed class InMemoryLabDriver : ILabDriver
{
    /// <inheritdoc />
    public Task<PlacedLabOrder> PlaceLabOrderAsync(PlaceLabOrderCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var id = DeterministicGuid.From($"{context.CorrelationId}:lab.order:{command.PatientId:N}");
        var placer = "LAB-" + id.ToString("N")[..12].ToUpperInvariant();
        return Task.FromResult(new PlacedLabOrder(id, placer));
    }

    /// <inheritdoc />
    public Task<PublishedLabResult> PublishResultAsync(PublishLabResultCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(new PublishedLabResult(command.PlacerOrderNumber));
    }
}

/// <summary>In-memory <see cref="IHieDriver"/> returning deterministic document ids.</summary>
public sealed class InMemoryHieDriver : IHieDriver
{
    /// <inheritdoc />
    public Task<UploadedDocument> UploadDocumentAsync(UploadDocumentCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var id = DeterministicGuid.From($"{context.CorrelationId}:hie.document:{command.PatientId:N}:{command.Kind}:{command.Title}");
        return Task.FromResult(new UploadedDocument(id));
    }
}
