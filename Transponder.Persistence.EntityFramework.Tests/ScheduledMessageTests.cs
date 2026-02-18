using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework.Tests;

public sealed class ScheduledMessageTests
{
    [Fact]
    public void Headers_Return_Empty_For_Null_Payload()
    {
        var record = new ScheduledMessageRecord { Headers = null };

        IReadOnlyDictionary<string, object?> headers = ((IScheduledMessage)record).Headers;

        Assert.Empty(headers);
    }
}
