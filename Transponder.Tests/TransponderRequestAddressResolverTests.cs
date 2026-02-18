namespace Transponder.Tests;

public sealed class TransponderRequestAddressResolverTests
{
    private sealed class TestMessage
    {
    }

    private sealed class Outer
    {
        public interface INner
        {
            string Marker { get; }
        }
    }

    [Fact]
    public void Create_Builds_Address_With_Custom_Prefix()
    {
        var busAddress = new Uri("https://localhost/api");
        Func<Type, Uri?> resolver = TransponderRequestAddressResolver.Create(
            busAddress,
            "custom",
            _ => "sample-message");

        Uri? resolved = resolver(typeof(TestMessage));

        Assert.Equal(new Uri("https://localhost/api/custom/sample-message"), resolved);
    }

    [Fact]
    public void Create_With_Address_List_Defaults_To_First_Address()
    {
        Uri[] addresses =
        [
            new("https://host-a/api"),
            new("https://host-b/api")
        ];

        Func<Type, Uri?> resolver = TransponderRequestAddressResolver.Create(addresses);

        Uri? resolved = resolver(typeof(TestMessage));

        Assert.Equal("host-a", resolved?.Host);
    }

    [Fact]
    public void Create_With_Address_List_RoundRobins()
    {
        Uri[] addresses =
        [
            new("https://host-a/api"),
            new("https://host-b/api")
        ];

        Func<Type, Uri?> resolver = TransponderRequestAddressResolver.Create(
            addresses,
            RemoteAddressStrategy.RoundRobin);

        Uri? first = resolver(typeof(TestMessage));
        Uri? second = resolver(typeof(TestMessage));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first.Host, second.Host);
    }

    [Fact]
    public void DefaultPathFormatter_Sanitizes_Type_Name()
    {
        string segment = TransponderRequestAddressResolver.DefaultPathFormatter(typeof(Outer.INner));

        Assert.DoesNotContain('.', segment);
        Assert.DoesNotContain('+', segment);
        Assert.Contains("Outer", segment);
        Assert.Contains("INner", segment);
    }
}
