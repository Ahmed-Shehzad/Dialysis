using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework.Tests;

public sealed class ScheduledMessageEntityTests
{
    [Fact]
    public void Headers_Return_Empty_For_Null_Payload()
    {
        var entity = new ScheduledMessageEntity { Headers = null };

        var headers = ((IScheduledMessage)entity).Headers;

        Assert.Empty(headers);
    }
}
