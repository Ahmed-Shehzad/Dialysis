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

    /// <summary>Remote eIDAS-qualified signature where the private key lives in a TSP (CSC v2).</summary>
    RemoteQes = 3,
}

/// <summary>
/// PAdES conformance level embedded in the signature row. Determines whether a TSA
/// timestamp was added and whether revocation evidence (CRL / OCSP) was packed into
/// the DSS dictionary.
/// </summary>
public enum PadesLevel
{
    /// <summary>PAdES-B-B — baseline, no TSA, no DSS. Pre-#128 default.</summary>
    B = 1,

    /// <summary>PAdES-B-T — TSA-stamped signing time embedded.</summary>
    T = 2,

    /// <summary>PAdES-B-LT — TSA + DSS revocation evidence packed in.</summary>
    LT = 3,

    /// <summary>PAdES-B-LTA — LT plus a document-timestamp over the DSS to extend trust.</summary>
    LTA = 4,
}

/// <summary>Whether the signature is an advanced (AES) or qualified (QES) electronic signature.</summary>
public enum SignatureFormat
{
    /// <summary>Advanced electronic signature — local key, eIDAS Art. 26.</summary>
    Aes = 1,

    /// <summary>Qualified electronic signature — TSP-held key, eIDAS Art. 28.</summary>
    Qes = 2,
}

/// <summary>What kind of revocation evidence the DSS dictionary carries.</summary>
public enum RevocationEvidenceFormat
{
    None = 0,
    Crl = 1,
    Ocsp = 2,
    Both = 3,
}
