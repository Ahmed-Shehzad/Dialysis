using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void Headers_Returns_Empty_For_Invalid_Json()
    {
        var record = new OutboxMessageRecord { Headers = "{invalid" };

        IReadOnlyDictionary<string, object?> headers = ((IOutboxMessage)record).Headers;

        Assert.Empty(headers);
    }

    [Fact]
    public void Headers_Are_Case_Insensitive()
    {
        var record = new OutboxMessageRecord { Headers = "{\"Key\":\"value\"}" };

        IReadOnlyDictionary<string, object?> headers = ((IOutboxMessage)record).Headers;

        Assert.True(headers.ContainsKey("key"));
    }
}
