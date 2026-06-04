using Dialysis.BuildingBlocks.ClinicianNotification;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.OnCall;

/// <summary>
/// Round-trip tests for the cross-channel dispatcher. The two scenarios it must get right are
/// (a) fall through to the next sender on the same channel when one fails, and (b) surface a
/// useful failure result when no sender is registered for the requested channel.
/// </summary>
public sealed class ClinicianNotificationDispatcherTests
{
    [Fact]
    public async Task First_Successful_Sender_Short_Circuits_The_Pool_Async()
    {
        var dispatcher = new ClinicianNotificationDispatcher(
            [new StubSender("sms", succeed: true, name: "first"),
             new StubSender("sms", succeed: true, name: "second")],
            NullLogger<ClinicianNotificationDispatcher>.Instance);

        var outcomes = await dispatcher.DispatchAsync(
            [Request("sms", "+1234")], CancellationToken.None);

        outcomes.Single().Result.Delivered.ShouldBeTrue();
    }

    [Fact]
    public async Task Failing_First_Sender_Falls_Through_To_Second_Async()
    {
        var fallback = new StubSender("sms", succeed: true, name: "twilio");
        var dispatcher = new ClinicianNotificationDispatcher(
            [new StubSender("sms", succeed: false, name: "broken"), fallback],
            NullLogger<ClinicianNotificationDispatcher>.Instance);

        var outcomes = await dispatcher.DispatchAsync(
            [Request("sms", "+1234")], CancellationToken.None);

        outcomes.Single().Result.Delivered.ShouldBeTrue();
        fallback.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task No_Sender_For_Channel_Returns_Configured_Failure_Async()
    {
        var dispatcher = new ClinicianNotificationDispatcher(
            [new StubSender("email", succeed: true, name: "smtp")],
            NullLogger<ClinicianNotificationDispatcher>.Instance);

        var outcomes = await dispatcher.DispatchAsync(
            [Request("sms", "+1234")], CancellationToken.None);

        outcomes.Single().Result.Delivered.ShouldBeFalse();
        outcomes.Single().Result.FailureReason.ShouldNotBeNull().ShouldContain("No sender registered");
    }

    private static ClinicianNotificationRequest Request(string channel, string address) => new(
        Channel: channel,
        Address: address,
        Subject: "Test",
        Body: "Body",
        DeepLink: null,
        Priority: NotificationPriority.Critical,
        Metadata: new Dictionary<string, string>());

    private sealed class StubSender : IClinicianNotificationSender
    {
        private readonly string _channel;
        private readonly bool _succeed;
        private readonly string _name;
        public StubSender(string channel, bool succeed, string name)
        {
            _channel = channel;
            _succeed = succeed;
            _name = name;
        }
        public string ChannelCode => _channel;
        public bool WasCalled { get; private set; }
        public Task<ClinicianNotificationResult> SendAsync(ClinicianNotificationRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(_succeed
                ? new ClinicianNotificationResult(true, $"{_name}-id", null)
                : new ClinicianNotificationResult(false, null, $"{_name}-failed"));
        }
    }
}
