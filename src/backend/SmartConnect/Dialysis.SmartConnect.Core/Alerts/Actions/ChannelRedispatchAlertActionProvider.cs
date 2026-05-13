using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace Dialysis.SmartConnect.Alerts.Actions;

/// <summary>
/// Re-dispatches a synthetic <see cref="IntegrationMessage"/> to a designated SmartConnect flow when
/// the alert fires. Lets operators handle alerts via the existing flow infrastructure (retry, fan-out, queue).
/// Properties JSON: <c>{"targetFlowId":"&lt;guid&gt;","payloadTemplate":"..."}</c>.
/// The new message carries <c>smartconnect.alert.ruleId</c>, <c>smartconnect.alert.eventId</c>, and
/// <c>smartconnect.alert.errorType</c> metadata so the receiving flow can route on the alert origin.
/// </summary>
public sealed class ChannelRedispatchAlertActionProvider(Func<IFlowRuntime> runtimeAccessor, TimeProvider time) : IAlertActionProvider
{
    public const string KindValue = "channel-redispatch";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public string Kind => KindValue;

    public async Task<AlertActionResult> ExecuteAsync(
        AlertEvent evt,
        AlertRule rule,
        AlertActionSlot slot,
        CancellationToken cancellationToken)
    {
        RedispatchProperties? props;
        try
        {
            props = string.IsNullOrWhiteSpace(slot.PropertiesJson)
                ? null
                : JsonSerializer.Deserialize<RedispatchProperties>(slot.PropertiesJson, JsonOpts);
        }
        catch (JsonException ex)
        {
            return AlertActionResult.Failure($"Invalid channel-redispatch action properties JSON: {ex.Message}");
        }
        if (props is null || props.TargetFlowId == Guid.Empty)
        {
            return AlertActionResult.Failure("Channel re-dispatch action requires a non-empty 'targetFlowId'.");
        }

        var payload = AlertVariables.Render(
            props.PayloadTemplate ?? $"{{\"alertId\":\"${{alertId}}\",\"errorType\":\"${{errorType}}\",\"errorDetail\":\"${{errorDetail}}\"}}",
            evt,
            rule);

        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("smartconnect.alert.ruleId", rule.Id.ToString())
            .Add("smartconnect.alert.eventId", evt.Id.ToString())
            .Add("smartconnect.alert.errorType", evt.ErrorType.ToString())
            .Add("smartconnect.alert.sourceFlowId", evt.FlowId?.ToString() ?? string.Empty);

        var message = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = props.TargetFlowId,
            CorrelationId = evt.CorrelationId ?? evt.Id.ToString(),
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = time.GetUtcNow(),
            Metadata = metadata,
        };

        try
        {
            var runtime = runtimeAccessor();
            var result = await runtime.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
            return result.Succeeded
                ? AlertActionResult.Success($"Dispatched to flow {props.TargetFlowId}")
                : AlertActionResult.Failure(result.Error ?? "Re-dispatch returned failure.");
        }
        catch (Exception ex)
        {
            return AlertActionResult.Failure(ex.Message);
        }
    }

    private sealed class RedispatchProperties
    {
        public Guid TargetFlowId { get; set; }
        public string? PayloadTemplate { get; set; }
    }
}
