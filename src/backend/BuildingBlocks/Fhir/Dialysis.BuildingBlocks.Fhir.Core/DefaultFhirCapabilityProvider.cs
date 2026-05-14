using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Reflects the <see cref="FhirResourceRegistry"/> into <c>CapabilityStatement.rest.resource[]</c>.
/// </summary>
public sealed class DefaultFhirCapabilityProvider(FhirResourceRegistry registry) : IFhirCapabilityProvider
{
    public IReadOnlyList<CapabilityStatement.ResourceComponent> DescribeResources()
    {
        var result = new List<CapabilityStatement.ResourceComponent>(registry.Entries.Count);
        foreach (var (typeName, capability) in registry.Entries)
        {
            var component = new CapabilityStatement.ResourceComponent
            {
                Type = typeName,
                Profile = capability.SupportedProfiles.FirstOrDefault(),
                Interaction = new List<CapabilityStatement.ResourceInteractionComponent>(),
            };

            if (capability.SupportsRead)
            {
                component.Interaction.Add(new CapabilityStatement.ResourceInteractionComponent
                {
                    Code = CapabilityStatement.TypeRestfulInteraction.Read,
                });
            }

            if (capability.SupportsSearch)
            {
                component.Interaction.Add(new CapabilityStatement.ResourceInteractionComponent
                {
                    Code = CapabilityStatement.TypeRestfulInteraction.SearchType,
                });
            }

            if (capability.SupportedProfiles.Count > 1)
            {
                component.SupportedProfile = capability.SupportedProfiles.Skip(1).ToList();
            }

            result.Add(component);
        }

        return result;
    }
}
