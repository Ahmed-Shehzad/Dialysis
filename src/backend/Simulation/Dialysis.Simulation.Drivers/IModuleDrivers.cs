namespace Dialysis.Simulation.Drivers;

/// <summary>Drives the EHR module's clinical write surface.</summary>
public interface IEhrDriver
{
    /// <summary>Registers a patient.</summary>
    Task<RegisteredPatient> RegisterPatientAsync(RegisterPatientCommand command, DriverContext context, CancellationToken cancellationToken);

    /// <summary>Starts an encounter.</summary>
    Task<StartedEncounter> StartEncounterAsync(StartEncounterCommand command, DriverContext context, CancellationToken cancellationToken);

    /// <summary>Closes the encounter (publishing diagnoses/procedures) and returns the captured charges.</summary>
    Task<ClosedEncounter> CloseEncounterAsync(CloseEncounterCommand command, DriverContext context, CancellationToken cancellationToken);

    /// <summary>Requests a referral.</summary>
    Task<RequestedReferral> RequestReferralAsync(RequestReferralCommand command, DriverContext context, CancellationToken cancellationToken);
}

/// <summary>Drives the HIS module's scheduling + patient-flow write surface.</summary>
public interface IHisDriver
{
    /// <summary>Books an appointment.</summary>
    Task<BookedAppointment> BookAppointmentAsync(BookAppointmentCommand command, DriverContext context, CancellationToken cancellationToken);

    /// <summary>Admits a patient to a ward.</summary>
    Task<AdmittedPatient> AdmitPatientAsync(AdmitPatientCommand command, DriverContext context, CancellationToken cancellationToken);

    /// <summary>Discharges an admitted patient.</summary>
    Task<DischargedPatient> DischargePatientAsync(DischargePatientCommand command, DriverContext context, CancellationToken cancellationToken);
}

/// <summary>Drives the Lab module's order + result surface.</summary>
public interface ILabDriver
{
    /// <summary>Places a lab order.</summary>
    Task<PlacedLabOrder> PlaceLabOrderAsync(PlaceLabOrderCommand command, DriverContext context, CancellationToken cancellationToken);

    /// <summary>Publishes a lab result for a placed order.</summary>
    Task<PublishedLabResult> PublishResultAsync(PublishLabResultCommand command, DriverContext context, CancellationToken cancellationToken);
}

/// <summary>Drives the HIE document store.</summary>
public interface IHieDriver
{
    /// <summary>Uploads a generated document.</summary>
    Task<UploadedDocument> UploadDocumentAsync(UploadDocumentCommand command, DriverContext context, CancellationToken cancellationToken);
}
