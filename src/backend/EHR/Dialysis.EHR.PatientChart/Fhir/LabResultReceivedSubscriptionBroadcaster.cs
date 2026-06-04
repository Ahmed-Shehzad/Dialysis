using System.Globalization;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Integration;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.PatientChart.Fhir;

/// <summary>
/// Fans out <see cref="LabResultReceivedIntegrationEvent"/> to FHIR Subscriptions registered for
/// the <c>lab-result</c> topic, projecting the result to a FHIR R4 <c>Observation</c>. Subscriber
/// filters can include <c>patient</c>, <c>code</c> (the LOINC code, for threshold alerts such as
/// glucose &gt; 200 mg/dL), and <c>abnormal</c> (the HL7 abnormal flag).
/// </summary>
public sealed class LabResultReceivedSubscriptionBroadcaster : IConsumer<LabResultReceivedIntegrationEvent>
{
    private readonly SubscriptionBroadcaster _broadcaster;
    /// <summary>
    /// Fans out <see cref="LabResultReceivedIntegrationEvent"/> to FHIR Subscriptions registered for
    /// the <c>lab-result</c> topic, projecting the result to a FHIR R4 <c>Observation</c>. Subscriber
    /// filters can include <c>patient</c>, <c>code</c> (the LOINC code, for threshold alerts such as
    /// glucose &gt; 200 mg/dL), and <c>abnormal</c> (the HL7 abnormal flag).
    /// </summary>
    public LabResultReceivedSubscriptionBroadcaster(SubscriptionBroadcaster broadcaster) => _broadcaster = broadcaster;
    public const string TopicUrl = "https://dialysis.local/fhir/SubscriptionTopic/lab-result";

    private const string UcumSystem = "http://unitsofmeasure.org";

    public async Task HandleAsync(ConsumeContext<LabResultReceivedIntegrationEvent> context)
    {
        var ev = context.Message;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["patient"] = ev.PatientId.ToString(),
            ["code"] = ev.LoincCode,
            ["abnormal"] = ev.AbnormalFlag,
        };

        var observation = new Observation
        {
            Id = ev.LabResultId.ToString(),
            Status = ObservationStatus.Final,
            Subject = new ResourceReference($"Patient/{ev.PatientId}"),
            BasedOn = [new ResourceReference($"ServiceRequest/{ev.LabOrderId}")],
            Effective = new FhirDateTime(ev.ObservedAtUtc),
            Code = new CodeableConcept(EhrCodeSystems.Loinc, ev.LoincCode),
        };

        if (decimal.TryParse(ev.ValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
        {
            observation.Value = new Quantity
            {
                Value = numeric,
                Unit = ev.UnitCode,
                System = string.IsNullOrWhiteSpace(ev.UnitCode) ? null : UcumSystem,
                Code = ev.UnitCode,
            };
        }
        else
        {
            observation.Value = new FhirString(ev.ValueText);
        }

        if (!string.IsNullOrWhiteSpace(ev.ReferenceRangeText))
        {
            observation.ReferenceRange.Add(new Observation.ReferenceRangeComponent { Text = ev.ReferenceRangeText });
        }

        if (!string.IsNullOrWhiteSpace(ev.AbnormalFlag))
        {
            observation.Interpretation.Add(new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation", ev.AbnormalFlag));
        }

        await _broadcaster.BroadcastAsync(TopicUrl, attributes, observation, context.CancellationToken).ConfigureAwait(false);
    }
}
