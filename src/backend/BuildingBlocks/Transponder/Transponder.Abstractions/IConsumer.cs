namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Handles deliveries of <typeparamref name="TMessage"/> when registered with Transponder.
/// </summary>
public interface IConsumer<TMessage>
    where TMessage : class
{
    Task HandleAsync(ConsumeContext<TMessage> context);
}
