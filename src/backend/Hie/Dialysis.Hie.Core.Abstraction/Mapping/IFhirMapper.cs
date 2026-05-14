using Hl7.Fhir.Model;

namespace Dialysis.Hie.Core.Abstraction.Mapping;

/// <summary>
/// Maps a module integration event to a FHIR resource for outbound exchange.
/// Implementations live in <c>Dialysis.Hie.Outbound</c> and are registered as scoped services.
/// </summary>
/// <typeparam name="TEvent">Source integration event type from another module's <c>.Contracts</c> assembly.</typeparam>
/// <typeparam name="TResource">Target Firely FHIR resource type.</typeparam>
public interface IFhirMapper<in TEvent, out TResource>
    where TEvent : class
    where TResource : Resource
{
    TResource Map(TEvent integrationEvent);
}
