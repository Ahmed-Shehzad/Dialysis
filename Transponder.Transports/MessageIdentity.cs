namespace Transponder.Transports;

/// <summary>
/// Groups the identity fields that correlate and track messages across the system.
/// </summary>
public sealed record MessageIdentity(
    Ulid? MessageId = null,
    Ulid? CorrelationId = null,
    Ulid? ConversationId = null);
