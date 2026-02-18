namespace Transponder;

/// <summary>
/// Groups source and destination addresses for message routing.
/// </summary>
public sealed record MessageAddressing(
    Uri? SourceAddress = null,
    Uri? DestinationAddress = null);
