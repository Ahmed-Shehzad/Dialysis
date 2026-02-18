namespace Transponder.Tests;

public sealed class JsonMessageSerializerTests
{
    private sealed record TestMessage(string Name, int Count);

    [Fact]
    public void Serialize_And_Deserialize_Roundtrip()
    {
        var serializer = new JsonMessageSerializer();
        var message = new TestMessage("hello", 3);

        ReadOnlyMemory<byte> payload = serializer.Serialize(message, typeof(TestMessage));
        var deserialized = (TestMessage)serializer.Deserialize(payload.Span, typeof(TestMessage));

        Assert.Equal(message, deserialized);
    }
}
