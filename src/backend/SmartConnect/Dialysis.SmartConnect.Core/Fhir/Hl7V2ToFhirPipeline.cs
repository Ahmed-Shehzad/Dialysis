using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir;

/// <summary>
/// Dispatches a parsed <see cref="Hl7V2Message"/> to all registered <see cref="IFhirV2MessageMapper{T}"/>
/// implementations whose <c>TriggerEvent</c> matches <c>MSH-9</c> on the message. Returns the produced
/// FHIR resources (zero or more, depending on which mappers are registered) — callers may build a
/// transaction Bundle or fan them out to integration events.
/// </summary>
public sealed class Hl7V2ToFhirPipeline
{
    private readonly IEnumerable<IFhirV2MessageMapperWrapper> _mappers;
    /// <summary>
    /// Dispatches a parsed <see cref="Hl7V2Message"/> to all registered <see cref="IFhirV2MessageMapper{T}"/>
    /// implementations whose <c>TriggerEvent</c> matches <c>MSH-9</c> on the message. Returns the produced
    /// FHIR resources (zero or more, depending on which mappers are registered) — callers may build a
    /// transaction Bundle or fan them out to integration events.
    /// </summary>
    public Hl7V2ToFhirPipeline(IEnumerable<IFhirV2MessageMapperWrapper> mappers) => _mappers = mappers;
    public IReadOnlyList<Resource> Transform(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var trigger = ReadTriggerEvent(message);
        if (string.IsNullOrEmpty(trigger))
            return Array.Empty<Resource>();

        var produced = new List<Resource>();
        foreach (var wrapper in _mappers)
        {
            if (string.Equals(wrapper.TriggerEvent, trigger, StringComparison.OrdinalIgnoreCase))
                produced.Add(wrapper.Map(message));
        }
        return produced;
    }

    /// <summary>
    /// MSH-9 carries the message type. Convention: <c>MSH-9.1^MSH-9.2</c> (e.g., <c>ADT^A01</c>).
    /// Falls back to the raw MSH-9 value when components are absent.
    /// </summary>
    private static string ReadTriggerEvent(Hl7V2Message message)
    {
        var type = message.GetValue("MSH.9.1");
        var trigger = message.GetValue("MSH.9.2");
        if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(trigger))
            return $"{type}^{trigger}";
        return message.GetValue("MSH.9") ?? string.Empty;
    }
}

/// <summary>
/// Untyped projection of <see cref="IFhirV2MessageMapper{TResource}"/> so the pipeline can hold a
/// heterogeneous collection. Registered automatically by the SmartConnect FHIR composition.
/// </summary>
public interface IFhirV2MessageMapperWrapper
{
    string TriggerEvent { get; }

    Resource Map(Hl7V2Message message);
}

internal sealed class FhirV2MessageMapperWrapper<TResource> : IFhirV2MessageMapperWrapper
    where TResource : Resource
{
    private readonly IFhirV2MessageMapper<TResource> _inner;
    public FhirV2MessageMapperWrapper(IFhirV2MessageMapper<TResource> inner) => _inner = inner;
    public string TriggerEvent => _inner.TriggerEvent;

    public Resource Map(Hl7V2Message message) => _inner.Map(message);
}
