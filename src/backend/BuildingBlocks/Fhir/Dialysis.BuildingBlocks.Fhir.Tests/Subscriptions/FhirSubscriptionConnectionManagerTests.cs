using System.Text;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Subscriptions;

public sealed class FhirSubscriptionConnectionManagerTests
{
    [Fact]
    public async Task Pushes_Only_To_Connections_Bound_To_The_Subscription_Async()
    {
        var manager = new FhirSubscriptionConnectionManager();
        var boundSink = new RecordingSink();
        using (manager.Register("sub-a", boundSink))
        {
            var deliveredToA = await manager.PushAsync("sub-a", Bytes("hello"), CancellationToken.None);
            var deliveredToB = await manager.PushAsync("sub-b", Bytes("hello"), CancellationToken.None);

            deliveredToA.ShouldBe(1);
            deliveredToB.ShouldBe(0);
            boundSink.Payloads.Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Stops_Delivering_After_The_Registration_Is_Disposed_Async()
    {
        var manager = new FhirSubscriptionConnectionManager();
        var sink = new RecordingSink();
        var registration = manager.Register("sub-a", sink);

        (await manager.PushAsync("sub-a", Bytes("one"), CancellationToken.None)).ShouldBe(1);
        registration.Dispose();
        (await manager.PushAsync("sub-a", Bytes("two"), CancellationToken.None)).ShouldBe(0);
        sink.Payloads.Count.ShouldBe(1);
    }

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    private sealed class RecordingSink : IFhirSubscriptionSink
    {
        public List<string> Payloads { get; } = [];

        public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
        {
            Payloads.Add(Encoding.UTF8.GetString(payload.Span));
            return ValueTask.CompletedTask;
        }
    }
}
