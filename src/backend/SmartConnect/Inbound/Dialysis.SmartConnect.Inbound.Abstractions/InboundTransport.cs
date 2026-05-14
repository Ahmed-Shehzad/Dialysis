using Dialysis.SmartConnect.Persistence;

namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Default <see cref="IInboundTransport"/>: preflight flow existence/state when possible, then <see cref="IFlowRuntime.DispatchAsync"/>.
/// </summary>
public sealed class InboundTransport(
    IFlowRuntime flowRuntime,
    IIntegrationFlowRepository? flows) : IInboundTransport
{
    public async Task<InboundReceiveResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken)
    {
        if (flows is not null)
        {
            var flow = await flows.GetByIdAsync(message.FlowId, cancellationToken).ConfigureAwait(false);
            if (flow is null)
            {
                return new InboundReceiveResult
                {
                    Succeeded = false,
                    Error = "Integration flow was not found.",
                    SuggestedHttpStatus = 404,
                };
            }

            if (flow.RuntimeState is not FlowRuntimeState.Started)
            {
                return new InboundReceiveResult
                {
                    Succeeded = false,
                    Error = flow.RuntimeState == FlowRuntimeState.Paused
                        ? "Integration flow is paused."
                        : "Integration flow is not started.",
                    SuggestedHttpStatus = 409,
                };
            }
        }

        var result = await flowRuntime.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var status = result.Error switch
            {
                "Integration flow was not found." => 404,
                "Integration flow is not started." => 409,
                _ => 500,
            };

            return new InboundReceiveResult
            {
                Succeeded = false,
                Error = result.Error,
                OutboundRoutesAttempted = result.OutboundRoutesAttempted,
                SuggestedHttpStatus = status,
            };
        }

        return new InboundReceiveResult
        {
            Succeeded = true,
            Error = result.Error,
            OutboundRoutesAttempted = result.OutboundRoutesAttempted,
            SuggestedHttpStatus = 200,
        };
    }
}
