using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Subscriptions;

public sealed class EmailSmsChannelDispatcherTests
{
    [Fact]
    public async Task Email_Dispatcher_Delegates_To_Registered_Notifier_Async()
    {
        var notifier = new RecordingEmailNotifier();
        var services = new ServiceCollection().AddSingleton<IEmailNotifier>(notifier).BuildServiceProvider();
        var dispatcher = new EmailNotificationDispatcher(
            services,
            NullLogger<EmailNotificationDispatcher>.Instance);

        await dispatcher.DispatchAsync(
            NewSub(SubscriptionChannelType.Email, "clinician@example.test"),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Observation { Id = "o1" },
            CancellationToken.None);

        notifier.Sent.Count.ShouldBe(1);
        notifier.Sent[0].ToAddress.ShouldBe("clinician@example.test");
        notifier.Sent[0].FhirBundleJson.ShouldContain("Bundle");
    }

    [Fact]
    public async Task Email_Channel_Is_Inert_When_No_Notifier_Registered_Async()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var dispatcher = new EmailNotificationDispatcher(
            services,
            NullLogger<EmailNotificationDispatcher>.Instance);

        // Must not throw — the channel is simply inert.
        await dispatcher.DispatchAsync(
            NewSub(SubscriptionChannelType.Email, "x@example.test"),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Observation { Id = "o1" },
            CancellationToken.None);
    }

    [Fact]
    public async Task Sms_Dispatcher_Sends_Concise_Alert_To_Registered_Notifier_Async()
    {
        var notifier = new RecordingSmsNotifier();
        var services = new ServiceCollection().AddSingleton<ISmsNotifier>(notifier).BuildServiceProvider();
        var dispatcher = new SmsNotificationDispatcher(services, NullLogger<SmsNotificationDispatcher>.Instance);

        await dispatcher.DispatchAsync(
            NewSub(SubscriptionChannelType.Sms, "+15551234567"),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Observation { Id = "o9" },
            CancellationToken.None);

        notifier.Sent.Count.ShouldBe(1);
        notifier.Sent[0].ToNumber.ShouldBe("+15551234567");
        notifier.Sent[0].Message.ShouldContain("Observation/o9");
    }

    [Fact]
    public async Task Sms_Channel_Is_Inert_When_No_Notifier_Registered_Async()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var dispatcher = new SmsNotificationDispatcher(services, NullLogger<SmsNotificationDispatcher>.Instance);

        await dispatcher.DispatchAsync(
            NewSub(SubscriptionChannelType.Sms, "+1"),
            new Dictionary<string, string>(StringComparer.Ordinal),
            payloadResource: null,
            CancellationToken.None);
    }

    private static FhirSubscriptionRegistration NewSub(SubscriptionChannelType channel, string endpoint) => new(
        Id: "s1",
        TopicUrl: "https://dialysis.local/fhir/SubscriptionTopic/lab-result",
        ChannelType: channel,
        ChannelEndpoint: endpoint,
        ChannelHeader: null,
        FilterParameters: new Dictionary<string, string>(StringComparer.Ordinal),
        Status: SubscriptionStatus.Active);

    private sealed class RecordingEmailNotifier : IEmailNotifier
    {
        public List<SubscriptionEmailNotification> Sent { get; } = [];

        public ValueTask SendAsync(SubscriptionEmailNotification notification, CancellationToken cancellationToken)
        {
            Sent.Add(notification);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSmsNotifier : ISmsNotifier
    {
        public List<SubscriptionSmsNotification> Sent { get; } = [];

        public ValueTask SendAsync(SubscriptionSmsNotification notification, CancellationToken cancellationToken)
        {
            Sent.Add(notification);
            return ValueTask.CompletedTask;
        }
    }
}
