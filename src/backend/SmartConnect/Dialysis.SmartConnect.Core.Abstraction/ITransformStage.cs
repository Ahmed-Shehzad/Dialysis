namespace Dialysis.SmartConnect;

/// <summary>
/// Mutates the message payload (and optionally metadata) before an <see cref="IOutboundAdapter"/> send.
/// </summary>
public interface ITransformStage
{
    string Kind { get; }

    Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken);
}
