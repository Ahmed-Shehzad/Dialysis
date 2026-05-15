using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.PDMS.Contracts.Integration;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.PDMS.TreatmentSessions.Fhir;

/// <summary>
/// Fans out <see cref="IntradialyticAdverseEventIntegrationEvent"/> to FHIR Subscriptions
/// registered for the <c>dialysis-adverse-event</c> topic, projecting the event to a FHIR R4
/// <c>AdverseEvent</c> for real-time care-team alerting. Subscriber filters can include
/// <c>patient</c>, <c>kind</c> (the adverse-event SNOMED code), and <c>severity</c>.
/// </summary>
public sealed class IntradialyticAdverseEventSubscriptionBroadcaster(SubscriptionBroadcaster broadcaster)
    : IConsumer<IntradialyticAdverseEventIntegrationEvent>
{
    public const string TopicUrl = "https://dialysis.local/fhir/SubscriptionTopic/dialysis-adverse-event";

    private const string SnomedSystem = "http://snomed.info/sct";

    public async Task HandleAsync(ConsumeContext<IntradialyticAdverseEventIntegrationEvent> context)
    {
        var ev = context.Message;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["patient"] = ev.PatientId.ToString(),
            ["kind"] = ev.EventKindCode,
            ["severity"] = ev.Severity,
        };

        var adverseEvent = new AdverseEvent
        {
            Id = $"{ev.SessionId}-{ev.EventKindCode}",
            Subject = new ResourceReference($"Patient/{ev.PatientId}"),
            Actuality = AdverseEvent.AdverseEventActuality.Actual,
            Event = new CodeableConcept(SnomedSystem, ev.EventKindCode),
            Date = new FhirDateTime(ev.ObservedAtUtc).ToString(),
            Severity = new CodeableConcept { Text = ev.Severity },
            SuspectEntity =
            [
                new AdverseEvent.SuspectEntityComponent
                {
                    Instance = new ResourceReference($"Procedure/{ev.SessionId}"),
                },
            ],
        };

        if (!string.IsNullOrWhiteSpace(ev.Notes))
        {
            adverseEvent.Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{System.Net.WebUtility.HtmlEncode(ev.Notes)}</div>",
            };
        }

        await broadcaster.BroadcastAsync(TopicUrl, attributes, adverseEvent, context.CancellationToken).ConfigureAwait(false);
    }
}
