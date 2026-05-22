using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 <c>ORU^R40^ORU_R40</c> OBX segment to a FHIR R4 <see cref="Observation"/>.
/// Dialysis Machine Implementation Guide rev 4.0 §6 (Reporting Treatment Information) uses
/// <c>ORU^R40</c> as the trigger for PCD-01 patient-care-device messages; the wire structure
/// is otherwise identical to <c>ORU^R01</c>, so this mapper delegates to the existing
/// <see cref="OruR01ToObservationMapper"/> for the field-level work.
/// </summary>
/// <remarks>
/// Registering both triggers (R01 + R40) lets us accept upstreams from either older PCD-01
/// senders (R01) or the rev 4 senders the IG mandates (R40), without divergent code paths.
/// The Map() implementation is intentionally identity-via-delegation so future changes to
/// OBX handling are made once and apply to both triggers.
/// </remarks>
public sealed class OruR40ToObservationMapper : IFhirV2MessageMapper<Observation>
{
    private readonly OruR01ToObservationMapper _inner = new();

    public string TriggerEvent => "ORU^R40";

    public Observation Map(Hl7V2Message message) => _inner.Map(message);
}
