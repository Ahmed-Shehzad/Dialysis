using System.Text;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Subscriptions;

public sealed class FhirSubscriptionReplayTests
{
    [Fact]
    public async Task Buffers_When_No_Connection_And_Replays_On_Reconnect_Async()
    {
        var manager = new FhirSubscriptionConnectionManager(replayCapacity: 10);

        // No connection bound — these are buffered.
        await manager.PushAsync("sub-a", Bytes("one"), CancellationToken.None);
        await manager.PushAsync("sub-a", Bytes("two"), CancellationToken.None);

        var sink = new RecordingSink();
        using (manager.Register("sub-a", sink))
        {
            await manager.FlushReplayAsync("sub-a", sink, CancellationToken.None);
        }

        sink.Payloads.ShouldBe(["one", "two"]);
    }

    [Fact]
    public async Task Replay_Buffer_Is_Bounded_To_Capacity_Async()
    {
        var manager = new FhirSubscriptionConnectionManager(replayCapacity: 2);

        await manager.PushAsync("sub-a", Bytes("1"), CancellationToken.None);
        await manager.PushAsync("sub-a", Bytes("2"), CancellationToken.None);
        await manager.PushAsync("sub-a", Bytes("3"), CancellationToken.None);

        var sink = new RecordingSink();
        using (manager.Register("sub-a", sink))
        {
            await manager.FlushReplayAsync("sub-a", sink, CancellationToken.None);
        }

        // Oldest ("1") dropped past capacity.
        sink.Payloads.ShouldBe(["2", "3"]);
    }

    [Fact]
    public async Task Does_Not_Buffer_When_A_Connection_Is_Bound_Async()
    {
        var manager = new FhirSubscriptionConnectionManager(replayCapacity: 10);
        var live = new RecordingSink();
        using (manager.Register("sub-a", live))
        {
            await manager.PushAsync("sub-a", Bytes("live"), CancellationToken.None);
        }

        // A later reconnect should see nothing buffered (it was delivered live).
        var reconnect = new RecordingSink();
        using (manager.Register("sub-a", reconnect))
        {
            await manager.FlushReplayAsync("sub-a", reconnect, CancellationToken.None);
        }

        live.Payloads.ShouldBe(["live"]);
        reconnect.Payloads.ShouldBeEmpty();
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
