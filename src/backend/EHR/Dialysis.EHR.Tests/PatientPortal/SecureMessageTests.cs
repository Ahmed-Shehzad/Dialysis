using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.PatientPortal.Domain;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.PatientPortal;

public sealed class SecureMessageTests
{
    private static readonly DateTime _now = new(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Send_Starts_A_Thread_And_Raises_Sent()
    {
        var message = SecureMessage.Send(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(),
            SecureMessageDirection.PatientToProvider, "Question", "Body", _now);

        message.ThreadId.ShouldNotBe(Guid.Empty);
        message.IntegrationEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PatientPortalSecureMessageSentIntegrationEvent>();
    }

    [Fact]
    public void Reply_Reuses_The_Thread_And_Raises_Received()
    {
        var threadId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var reply = SecureMessage.Reply(
            Guid.NewGuid(), threadId, patientId, Guid.NewGuid(), "Re: Question", "Here's your answer", _now);

        reply.ThreadId.ShouldBe(threadId);
        reply.Direction.ShouldBe(SecureMessageDirection.ProviderToPatient);
        var raised = reply.IntegrationEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PatientPortalSecureMessageReceivedIntegrationEvent>();
        raised.ThreadId.ShouldBe(threadId);
        raised.PatientId.ShouldBe(patientId);
    }

    [Fact]
    public void Reply_Requires_An_Existing_Thread()
    {
        Should.Throw<ArgumentException>(() => SecureMessage.Reply(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Re", "Body", _now));
    }

    [Fact]
    public void MarkRead_Is_Idempotent()
    {
        var message = SecureMessage.Send(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            SecureMessageDirection.ProviderToPatient, "Hi", "Body", _now);

        message.MarkRead(_now);
        message.MarkRead(_now.AddHours(1));

        message.ReadAtUtc.ShouldBe(_now);
    }
}
