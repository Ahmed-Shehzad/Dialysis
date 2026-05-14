using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir;

/// <summary>
/// Maps a parsed HL7 v2 message into a FHIR R4 resource for a specific trigger event
/// (e.g., <c>ADT^A01</c>, <c>ORU^R01</c>). Built on the cross-cutting
/// <see cref="IFhirResourceMapper{TSource, TResource}"/> contract so consumers can ignore
/// the v2 origin downstream.
/// </summary>
public interface IFhirV2MessageMapper<out TResource> : IFhirResourceMapper<Hl7V2Message, TResource>
    where TResource : Resource
{
    /// <summary>HL7 v2 message type + trigger event this mapper handles, e.g. <c>"ADT^A01"</c>.</summary>
    string TriggerEvent { get; }
}
