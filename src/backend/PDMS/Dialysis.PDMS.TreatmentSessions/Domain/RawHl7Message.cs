using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.TreatmentSessions.Domain;

public enum Hl7MessageDirection
{
    Inbound = 1,
    Outbound = 2,
}

public enum Hl7ProcessingStatus
{
    Received = 1,
    Parsed = 2,
    Persisted = 3,
    Failed = 4,
}

/// <summary>
/// Audit copy of an HL7 v2 message as it crossed the PDMS boundary. Stored for replay, reparse, and
/// forensic investigation. Retention is governed by the existing pruner pattern in a later cleanup pass.
/// </summary>
public sealed class RawHl7Message : Entity<Guid>
{
    private RawHl7Message()
    {
    }

    public RawHl7Message(Guid id) : base(id)
    {
    }

    public Guid? MachineId { get; private set; }

    public Guid? SessionId { get; private set; }

    public string MessageType { get; private set; } = default!;

    public string MessageControlId { get; private set; } = default!;

    public DateTime ReceivedAtUtc { get; private set; }

    public Hl7MessageDirection Direction { get; private set; }

    public byte[] Payload { get; private set; } = default!;

    public Hl7ProcessingStatus ProcessingStatus { get; private set; }

    public static RawHl7Message Capture(
        Guid id,
        Guid? machineId,
        Guid? sessionId,
        string messageType,
        string messageControlId,
        DateTime receivedAtUtc,
        Hl7MessageDirection direction,
        byte[] payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        if (messageType.Length > 16) throw new ArgumentException("Message type too long.", nameof(messageType));
        ArgumentException.ThrowIfNullOrWhiteSpace(messageControlId);
        if (messageControlId.Length > 50)
            throw new ArgumentException("Message control id exceeds the rev4 extended limit of 50.", nameof(messageControlId));
        ArgumentNullException.ThrowIfNull(payload);

        return new RawHl7Message(id)
        {
            MachineId = machineId,
            SessionId = sessionId,
            MessageType = messageType.Trim(),
            MessageControlId = messageControlId.Trim(),
            ReceivedAtUtc = receivedAtUtc,
            Direction = direction,
            Payload = payload,
            ProcessingStatus = Hl7ProcessingStatus.Received,
        };
    }

    public void MarkParsed() => ProcessingStatus = Hl7ProcessingStatus.Parsed;
    public void MarkPersisted() => ProcessingStatus = Hl7ProcessingStatus.Persisted;
    public void MarkFailed() => ProcessingStatus = Hl7ProcessingStatus.Failed;
}
