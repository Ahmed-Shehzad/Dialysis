using System.Globalization;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// Builds FHIR R4 <c>ServiceRequest</c> resources from a <see cref="LabOrderFrame"/> — the FHIR
/// transport counterpart of <see cref="Hl7V2OrmO01Builder"/>. One <c>ServiceRequest</c> is emitted
/// per requested test (each carries the shared placer order number as a business identifier so the
/// returned <c>Observation</c> can be matched), and the set is wrapped in a collection
/// <c>Bundle</c> for single-payload dispatch.
/// </summary>
public static class LabServiceRequestBuilder
{
    private const string LoincSystem = "http://loinc.org";
    private const string PlacerIdentifierSystem = "urn:dialysis:lab:placer-order-number";

    /// <summary>Builds one <c>ServiceRequest</c> for a single requested test.</summary>
    public static ServiceRequest BuildOne(LabOrderFrame order, LabTestRequest test)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(test);

        var request = new ServiceRequest
        {
            Status = RequestStatus.Active,
            Intent = RequestIntent.Order,
            Priority = order.IsStat ? RequestPriority.Stat : RequestPriority.Routine,
            Subject = new ResourceReference($"Patient/{order.PatientIdentifier}"),
            AuthoredOn = order.OrderedAtUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture),
            Code = new CodeableConcept
            {
                Coding = [new Coding(LoincSystem, test.Code, test.Display)],
            },
        };

        request.Identifier.Add(new Identifier
        {
            System = PlacerIdentifierSystem,
            Value = order.PlacerOrderNumber,
        });

        if (!string.IsNullOrWhiteSpace(order.Specimen))
        {
            request.Specimen.Add(new ResourceReference { Display = order.Specimen });
        }

        return request;
    }

    /// <summary>Builds a collection <c>Bundle</c> with one <c>ServiceRequest</c> per requested test.</summary>
    public static Bundle BuildBundle(LabOrderFrame order, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(order);

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Timestamp = timestamp,
        };

        foreach (var test in order.Tests)
        {
            bundle.Entry.Add(new Bundle.EntryComponent { Resource = BuildOne(order, test) });
        }

        return bundle;
    }
}
