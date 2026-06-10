using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Bff.Events;
using Dialysis.PatientPortal.Bff.Notifications;
using Shouldly;
using Xunit;

namespace Dialysis.PatientPortal.Tests;

/// <summary>
/// Locks down the portal BFF's event → toast mapping: each consumer must route the notification
/// to the <c>patient:{id}</c> group of the patient named on the event, normalize the event's
/// unspecified-kind <c>OccurredOn</c> to UTC, and keep the payload PHI-light — the SignalR push is
/// a "go look" signal, never a clinical record (the SPA refetches through the synchronous,
/// permission-checked API).
/// </summary>
public sealed class NotificationConsumerTests
{
    private static readonly DateTime _occurredOn = new(2026, 6, 10, 8, 30, 0, DateTimeKind.Unspecified);

    private readonly RecordingNotifier _notifier = new();

    [Fact]
    public async Task Secure_Message_Reply_Is_Pushed_To_The_Patients_Group_Async()
    {
        var patientId = Guid.NewGuid();
        var consumer = new SecureMessageReceivedNotificationConsumer(_notifier);
        var @event = new PatientPortalSecureMessageReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: _occurredOn,
            SchemaVersion: 1,
            MessageId: Guid.NewGuid(),
            PatientId: patientId,
            ThreadId: Guid.NewGuid(),
            Subject: "Dialysis schedule question");

        await consumer.HandleAsync(MakeContext(@event));

        var push = _notifier.PatientPushes.ShouldHaveSingleItem();
        push.PatientId.ShouldBe(patientId.ToString());
        push.Notification.Type.ShouldBe("secure-message");
        push.Notification.Title.ShouldBe("Your care team replied");
        push.Notification.Summary.ShouldBe("Dialysis schedule question");
        push.Notification.PatientId.ShouldBe(patientId.ToString());
        _notifier.UserPushes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Secure_Message_Notification_Carries_No_Message_Or_Thread_Identifiers_Async()
    {
        var messageId = Guid.NewGuid();
        var threadId = Guid.NewGuid();
        var consumer = new SecureMessageReceivedNotificationConsumer(_notifier);
        var @event = new PatientPortalSecureMessageReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: _occurredOn,
            SchemaVersion: 1,
            MessageId: messageId,
            PatientId: Guid.NewGuid(),
            ThreadId: threadId,
            Subject: "Subject line");

        await consumer.HandleAsync(MakeContext(@event));

        var payload = JsonSerializer.Serialize(_notifier.PatientPushes.Single().Notification);
        payload.ShouldNotContain(messageId.ToString());
        payload.ShouldNotContain(threadId.ToString());
    }

    [Fact]
    public async Task Approved_Appointment_Request_Produces_An_Approved_Toast_With_The_Staff_Note_Async()
    {
        var patientId = Guid.NewGuid();
        var consumer = new AppointmentResolvedNotificationConsumer(_notifier);
        var @event = MakeAppointmentEvent(patientId, approved: true, staffNote: "Booked for Tuesday 09:00");

        await consumer.HandleAsync(MakeContext(@event));

        var push = _notifier.PatientPushes.ShouldHaveSingleItem();
        push.PatientId.ShouldBe(patientId.ToString());
        push.Notification.Type.ShouldBe("appointment-request");
        push.Notification.Title.ShouldBe("Appointment request approved");
        push.Notification.Summary.ShouldBe("Booked for Tuesday 09:00");
    }

    [Fact]
    public async Task Declined_Appointment_Request_Produces_A_Declined_Toast_Async()
    {
        var consumer = new AppointmentResolvedNotificationConsumer(_notifier);
        var @event = MakeAppointmentEvent(Guid.NewGuid(), approved: false, staffNote: null);

        await consumer.HandleAsync(MakeContext(@event));

        var push = _notifier.PatientPushes.ShouldHaveSingleItem();
        push.Notification.Title.ShouldBe("Appointment request declined");
        push.Notification.Summary.ShouldBeNull();
    }

    [Fact]
    public async Task After_Visit_Summary_Publishes_A_Metadata_Only_Toast_Async()
    {
        var patientId = Guid.NewGuid();
        var summaryId = Guid.NewGuid();
        var consumer = new AfterVisitSummaryPublishedNotificationConsumer(_notifier);
        var @event = new AfterVisitSummaryPublishedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: _occurredOn,
            SchemaVersion: 1,
            SummaryId: summaryId,
            PatientId: patientId,
            VisitDateUtc: new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc));

        await consumer.HandleAsync(MakeContext(@event));

        var push = _notifier.PatientPushes.ShouldHaveSingleItem();
        push.PatientId.ShouldBe(patientId.ToString());
        push.Notification.Type.ShouldBe("after-visit-summary");
        push.Notification.Title.ShouldBe("Your visit summary is ready");
        push.Notification.Link.ShouldBe("/portal/");
        JsonSerializer.Serialize(push.Notification).ShouldNotContain(summaryId.ToString());
    }

    [Fact]
    public async Task Occurred_On_Is_Normalised_To_Utc_Async()
    {
        var consumer = new SecureMessageReceivedNotificationConsumer(_notifier);
        var @event = new PatientPortalSecureMessageReceivedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: _occurredOn,
            SchemaVersion: 1,
            MessageId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            ThreadId: Guid.NewGuid(),
            Subject: "Subject");

        await consumer.HandleAsync(MakeContext(@event));

        var occurredOn = _notifier.PatientPushes.Single().Notification.OccurredOn;
        occurredOn.Offset.ShouldBe(TimeSpan.Zero);
        occurredOn.UtcDateTime.ShouldBe(_occurredOn);
    }

    [Fact]
    public async Task Consumers_Reject_A_Null_Context_Async()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            new SecureMessageReceivedNotificationConsumer(_notifier).HandleAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() =>
            new AppointmentResolvedNotificationConsumer(_notifier).HandleAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(() =>
            new AfterVisitSummaryPublishedNotificationConsumer(_notifier).HandleAsync(null!));
    }

    [Fact]
    public async Task Push_Honours_The_Deliverys_Cancellation_Token_Async()
    {
        using var cts = new CancellationTokenSource();
        var consumer = new AppointmentResolvedNotificationConsumer(_notifier);
        var @event = MakeAppointmentEvent(Guid.NewGuid(), approved: true, staffNote: null);

        await consumer.HandleAsync(MakeContext(@event, cts.Token));

        _notifier.PatientPushes.ShouldHaveSingleItem().CancellationToken.ShouldBe(cts.Token);
    }

    private static PatientPortalAppointmentResolvedIntegrationEvent MakeAppointmentEvent(
        Guid patientId, bool approved, string? staffNote) =>
        new(
            EventId: Guid.NewGuid(),
            OccurredOn: _occurredOn,
            SchemaVersion: 1,
            RequestId: Guid.NewGuid(),
            PatientId: patientId,
            Approved: approved,
            CreatedAppointmentId: approved ? Guid.NewGuid() : null,
            StaffNote: staffNote);

    private static ConsumeContext<TMessage> MakeContext<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class =>
        new(message, cancellationToken, new NoopBus());

    /// <summary>Records every push so tests can assert routing and payload shape.</summary>
    private sealed class RecordingNotifier : IBffNotifier
    {
        public List<(string PatientId, BffNotification Notification, CancellationToken CancellationToken)> PatientPushes { get; } = [];
        public List<(string UserSubject, BffNotification Notification)> UserPushes { get; } = [];

        public Task PushToPatientAsync(string patientId, BffNotification notification, CancellationToken cancellationToken = default)
        {
            PatientPushes.Add((patientId, notification, cancellationToken));
            return Task.CompletedTask;
        }

        public Task PushToUserAsync(string userSubject, BffNotification notification, CancellationToken cancellationToken = default)
        {
            UserPushes.Add((userSubject, notification));
            return Task.CompletedTask;
        }
    }

    private sealed class NoopBus : ITransponderBus
    {
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;

        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
            where TMessage : class => Task.CompletedTask;
    }
}
