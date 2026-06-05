using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// Transform stage that turns an upstream <c>LabOrderPlacedIntegrationEvent</c> (carried as a JSON
/// payload) into an HL7 v2.5 <c>ORM^O01</c> general-order message, ready for MLLP dispatch through
/// <c>TcpOutboundAdapter</c> to an external Laboratory Information System.
///
/// Decoupling note: the stage deserialises into a local <see cref="EventDto"/> rather than
/// referencing the Lab contracts assembly — SmartConnect ships as a standalone package and must not
/// take a module dependency. Property matching is case-insensitive so it tolerates either the
/// PascalCase Transponder envelope or a camelCase re-serialisation, and the priority enum is read
/// whether the outbox serialised it as a number (0 routine / 1 stat) or a string.
///
/// Fail-soft: a payload that isn't the expected JSON shape (no tests, missing placer/patient,
/// unparseable) passes through unchanged so a downstream route can handle it — mirroring
/// <c>MedicationAdministeredToHl7RasTransformStage</c>.
/// </summary>
public sealed class LabOrderPlacedToHl7OrmTransformStage : ITransformStage
{
    private readonly TimeProvider _clock;

    /// <summary>Creates the stage with the host clock used for the MSH timestamp and fallback control id.</summary>
    public LabOrderPlacedToHl7OrmTransformStage(TimeProvider clock) => _clock = clock;

    /// <summary>Flow-route stage kind selecting HL7 v2 ORM transmission of a placed lab order.</summary>
    public const string KindValue = "lab.order.placed.to.hl7-orm";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <inheritdoc />
    public string Kind => KindValue;

    /// <inheritdoc />
    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken)
        => Task.FromResult(Transform(message));

    private IntegrationMessage Transform(IntegrationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var frame = LabOrderEventParser.TryParse(message.Payload.Span, _jsonOptions);
        if (frame is null)
        {
            return message;
        }

        var controlId = string.IsNullOrWhiteSpace(message.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : message.CorrelationId;

        var wire = Hl7V2OrmO01Builder.Build(frame, controlId, _clock.GetUtcNow().UtcDateTime);
        return message.CloneWithPayload(Encoding.UTF8.GetBytes(wire), PayloadFormat.Utf8Text);
    }
}
