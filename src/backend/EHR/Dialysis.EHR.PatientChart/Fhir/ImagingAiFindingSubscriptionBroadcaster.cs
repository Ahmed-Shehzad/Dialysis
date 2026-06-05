using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.PatientChart.Fhir;

/// <summary>
/// Projects an <see cref="ImagingAiFindingProducedIntegrationEvent"/> to a FHIR R4
/// <c>Observation</c> and fans it to Subscriptions on the <c>imaging-ai-finding</c> topic. The
/// observation is deliberately <see cref="ObservationStatus.Preliminary"/> with an
/// <c>unconfirmed</c> note — AI output is advisory and requires human sign-off before it is final.
/// Subscriber filters: <c>patient</c>, <c>code</c> (the finding code), <c>interpretation</c>.
/// </summary>
public sealed class ImagingAiFindingSubscriptionBroadcaster : IConsumer<ImagingAiFindingProducedIntegrationEvent>
{
    private readonly SubscriptionBroadcaster _broadcaster;
    public ImagingAiFindingSubscriptionBroadcaster(SubscriptionBroadcaster broadcaster) => _broadcaster = broadcaster;

    /// <summary>FHIR SubscriptionTopic this broadcaster fans out on.</summary>
    public const string TopicUrl = "https://dialysis.local/fhir/SubscriptionTopic/imaging-ai-finding";

    public async Task HandleAsync(ConsumeContext<ImagingAiFindingProducedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ev = context.Message;

        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["patient"] = ev.PatientId.ToString(),
            ["code"] = ev.FindingCode,
            ["interpretation"] = ev.Interpretation,
        };

        var observation = new Observation
        {
            // Advisory, never auto-final — the human-in-the-loop sign-off lives on the EHR order.
            Status = ObservationStatus.Preliminary,
            Code = new CodeableConcept(ev.FindingSystem, ev.FindingCode, ev.FindingDisplay),
            Value = string.IsNullOrWhiteSpace(ev.Summary) ? new FhirString(ev.FindingDisplay) : new FhirString(ev.Summary),
            Method = new CodeableConcept { Text = $"AI model {ev.ModelId} (requires human review)" },
        };

        if (ev.PatientId != Guid.Empty)
        {
            observation.Subject = new ResourceReference($"Patient/{ev.PatientId}");
        }

        if (!string.IsNullOrWhiteSpace(ev.StudyInstanceUid))
        {
            observation.DerivedFrom.Add(new ResourceReference($"ImagingStudy/{ev.StudyInstanceUid}"));
        }

        observation.Interpretation.Add(new CodeableConcept { Text = ev.Interpretation });
        observation.Note.Add(new Annotation
        {
            Text = new Markdown($"AI-generated finding (confidence {ev.Confidence:0.00}). Advisory — pending clinician sign-off."),
        });

        await _broadcaster.BroadcastAsync(TopicUrl, attributes, observation, context.CancellationToken).ConfigureAwait(false);
    }
}
