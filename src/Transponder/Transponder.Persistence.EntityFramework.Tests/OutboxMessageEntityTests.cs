using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework.Tests;

public sealed class OutboxMessageEntityTests
{
    [Fact]
    public void Headers_Returns_Empty_For_Invalid_Json()
    {
        var entity = new OutboxMessageEntity { Headers = "{invalid" };

        var headers = ((IOutboxMessage)entity).Headers;

        Assert.Empty(headers);
    }

    [Fact]
    public void Headers_Are_Case_Insensitive()
    {
        var entity = new OutboxMessageEntity { Headers = "{\"Key\":\"value\"}" };

        var headers = ((IOutboxMessage)entity).Headers;

        Assert.True(headers.ContainsKey("key"));
    }
}
