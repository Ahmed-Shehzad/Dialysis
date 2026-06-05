using System.Text;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.SmartConnect.Contracts.Integration;
using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Lab;

namespace Dialysis.SmartConnect.Api.Lab;

/// <summary>
/// Host-side bridge that turns an inbound lab result routed onto the bus into the Lab context's
/// typed <see cref="LabResultReceivedIntegrationEvent"/>. A SmartConnect inbound flow that receives
/// an HL7 v2 <c>ORU^R01</c> from a partner LIS routes it to the <c>transponder-bus</c> outbound
/// adapter with routing hint <see cref="LabResultRoutingHint"/>; this consumer picks those payloads
/// up, parses them with <see cref="Hl7V2OruToLabResultMapper"/>, and republishes the result as the
/// Lab-owned typed event for <c>LabResultReceivedConsumer</c> to record against the placing order.
///
/// This is the one point where SmartConnect couples to a module contract — kept in the host (not the
/// standalone Core) and gated by the routing hint so non-lab routed payloads pass straight through.
/// </summary>
public sealed class LabResultBridgeConsumer : IConsumer<SmartConnectRoutedPayloadIntegrationEvent>
{
    /// <summary>Routing hint a flow stamps on a routed lab-result payload to select this bridge.</summary>
    public const string LabResultRoutingHint = "lab.result";

    private readonly ILogger<LabResultBridgeConsumer> _logger;

    /// <summary>Creates the bridge with its logger.</summary>
    public LabResultBridgeConsumer(ILogger<LabResultBridgeConsumer> logger) => _logger = logger;

    /// <inheritdoc />
    public async Task HandleAsync(ConsumeContext<SmartConnectRoutedPayloadIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        if (!string.Equals(message.RoutingHint, LabResultRoutingHint, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var frame = TryParse(message.Payload);
        if (frame is null)
        {
            _logger.LogWarning(
                "Lab-result routed payload (flow {FlowId}) was not a parseable ORU^R01; ignoring.",
                message.FlowId);
            return;
        }

        var typed = new LabResultReceivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PlacerOrderNumber: frame.PlacerOrderNumber,
            FillerOrderNumber: frame.FillerOrderNumber,
            PatientId: ParsePatientId(frame.PatientIdentifier),
            Status: frame.IsFinal ? LabOrderStatus.Resulted : LabOrderStatus.InProgress,
            Observations: [.. frame.Observations.Select(ToContract)],
            ResultedAtUtc: frame.ResultedAtUtc);

        await context.Bus.PublishAsync(typed, context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Bridged ORU result for placer order {PlacerOrderNumber} ({Count} observation(s)) to the Lab context.",
            frame.PlacerOrderNumber,
            frame.Observations.Count);
    }

    private static LabResultFrame? TryParse(byte[] payload)
    {
        if (payload is null || payload.Length == 0)
        {
            return null;
        }

        var text = Encoding.UTF8.GetString(payload);
        if (!text.StartsWith("MSH", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            return Hl7V2OruToLabResultMapper.TryMap(Hl7V2Message.Parse(text));
        }
        catch (FormatException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Guid ParsePatientId(string patientIdentifier) =>
        Guid.TryParse(patientIdentifier, out var id) ? id : Guid.Empty;

    private static LabObservationContract ToContract(LabResultObservation o) =>
        new(o.Code, o.Display, o.Value, o.Unit, o.ReferenceRange, MapInterpretation(o.Interpretation));

    /// <summary>Maps an HL7 table-0078 abnormal flag to the Lab interpretation enum.</summary>
    private static LabResultInterpretation MapInterpretation(string? abnormalFlag) =>
        (abnormalFlag ?? string.Empty).ToUpperInvariant() switch
        {
            "L" => LabResultInterpretation.Low,
            "H" => LabResultInterpretation.High,
            "LL" => LabResultInterpretation.CriticalLow,
            "HH" => LabResultInterpretation.CriticalHigh,
            "N" or "" => LabResultInterpretation.Normal,
            _ => LabResultInterpretation.Abnormal,
        };
}
