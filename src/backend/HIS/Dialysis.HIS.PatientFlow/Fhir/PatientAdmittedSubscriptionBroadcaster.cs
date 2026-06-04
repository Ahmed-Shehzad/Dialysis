using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIS.PatientFlow.Fhir;

/// <summary>
/// Fans out <see cref="PatientAdmittedIntegrationEvent"/> to FHIR Subscriptions registered for
/// the <c>encounter-admission-discharge</c> topic. Subscriber filters can include
/// <c>patient</c> (matches the admitted patient id) and <c>ward</c>.
/// </summary>
public sealed class PatientAdmittedSubscriptionBroadcaster : IConsumer<PatientAdmittedIntegrationEvent>
{
    private readonly SubscriptionBroadcaster _broadcaster;
    /// <summary>
    /// Fans out <see cref="PatientAdmittedIntegrationEvent"/> to FHIR Subscriptions registered for
    /// the <c>encounter-admission-discharge</c> topic. Subscriber filters can include
    /// <c>patient</c> (matches the admitted patient id) and <c>ward</c>.
    /// </summary>
    public PatientAdmittedSubscriptionBroadcaster(SubscriptionBroadcaster broadcaster) => _broadcaster = broadcaster;
    public const string TopicUrl = "https://dialysis.local/fhir/SubscriptionTopic/encounter-admission-discharge";

    public async Task HandleAsync(ConsumeContext<PatientAdmittedIntegrationEvent> context)
    {
        var ev = context.Message;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["patient"] = ev.PatientId.ToString(),
            ["ward"] = ev.WardCode,
            ["action"] = "admitted",
        };

        var encounter = new Encounter
        {
            Id = ev.AdmissionId.ToString(),
            Status = Encounter.EncounterStatus.InProgress,
            Subject = new ResourceReference($"Patient/{ev.PatientId}"),
            Period = new Period { StartElement = new FhirDateTime(ev.AdmittedAtUtc) },
            Location =
            [
                new Encounter.LocationComponent { Location = new ResourceReference($"Location/{ev.WardCode}") },
            ],
        };

        await _broadcaster.BroadcastAsync(TopicUrl, attributes, encounter, context.CancellationToken).ConfigureAwait(false);
    }
}
