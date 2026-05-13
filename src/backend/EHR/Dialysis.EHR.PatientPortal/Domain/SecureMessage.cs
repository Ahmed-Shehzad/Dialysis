using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.PatientPortal.Domain;

public enum SecureMessageDirection
{
    PatientToProvider = 1,
    ProviderToPatient = 2,
}

public sealed class SecureMessage : AggregateRoot<Guid>
{
    private SecureMessage()
    {
    }

    public SecureMessage(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid? TargetProviderId { get; private set; }

    public Guid ThreadId { get; private set; }

    public SecureMessageDirection Direction { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DateTime SentAtUtc { get; private set; }

    public DateTime? ReadAtUtc { get; private set; }

    public static SecureMessage Send(
        Guid id,
        Guid threadId,
        Guid patientId,
        Guid? targetProviderId,
        SecureMessageDirection direction,
        string subject,
        string body,
        DateTime sentAtUtc)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var message = new SecureMessage(id)
        {
            PatientId = patientId,
            TargetProviderId = targetProviderId,
            ThreadId = threadId == Guid.Empty ? Guid.CreateVersion7() : threadId,
            Direction = direction,
            Subject = subject.Trim(),
            Body = body,
            SentAtUtc = sentAtUtc,
        };

        message.RaiseIntegrationEvent(new PatientPortalSecureMessageSentIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            MessageId: id,
            PatientId: patientId,
            TargetProviderId: targetProviderId,
            Subject: message.Subject));

        return message;
    }

    public void MarkRead(DateTime readAtUtc)
    {
        if (ReadAtUtc.HasValue) return;
        ReadAtUtc = readAtUtc;
    }
}
