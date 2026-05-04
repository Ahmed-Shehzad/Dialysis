namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Marker for a command message (intent to change state). Transport and outbox semantics may treat commands differently from events.
/// </summary>
public interface ICommand : IMessage
{
}
