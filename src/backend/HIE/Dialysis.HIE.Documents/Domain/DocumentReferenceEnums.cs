namespace Dialysis.HIE.Documents.Domain;

/// <summary>FHIR <c>DocumentReference.status</c> plus a soft-delete flavour used internally.</summary>
public enum DocumentReferenceStatus
{
    Current = 1,
    Superseded = 2,
    EnteredInError = 3,
}

/// <summary>Which subsystem produced the document — drives audit and retention defaults.</summary>
public enum DocumentReferenceSource
{
    /// <summary>Indexed from a PDMS Reporting clinical document (discharge letter, etc.).</summary>
    PdmsReporting = 1,

    /// <summary>Received from a partner via the HIE inbound FHIR surface.</summary>
    HieInbound = 2,

    /// <summary>Uploaded by an admin through the documents-management UI.</summary>
    AdminUpload = 3,
}

/// <summary>Which signing cert produced the signature.</summary>
public enum DocumentSignerKind
{
    Platform = 1,
    User = 2,
}
