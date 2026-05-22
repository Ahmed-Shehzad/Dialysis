namespace Dialysis.SmartConnect;

/// <summary>
/// Well-known metadata keys that the ledger projects into dedicated indexed columns (slice
/// C2). Inbound transports populate these so the operator dashboard's filter dropdowns can
/// query without scanning the full <c>MetadataJson</c> blob. Convention: lowercase
/// dotted keys under the <c>smartconnect.</c> namespace, mirroring the
/// <see cref="BatchMetadataKeys"/> pattern from slice D.
/// </summary>
public static class LedgerSearchKeys
{
    /// <summary>Logical message type — e.g. HL7v2's <c>ORU^R40^ORU_R40</c>, NCPDP's
    /// transaction code <c>B1</c>, or a FHIR resource type like <c>Observation</c>.</summary>
    public const string MessageType = "smartconnect.message-type";

    /// <summary>Stable upstream identifier — for HL7v2 the convention is
    /// <c>{sendingApp}@{sendingFacility}</c> (matching the clock-skew monitor's source id);
    /// for NCPDP the BIN; for HTTP the partner identifier.</summary>
    public const string SenderId = "smartconnect.sender";
}
