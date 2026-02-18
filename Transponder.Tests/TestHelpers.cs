using Transponder.Transports;
using Transponder.Transports.Abstractions;

namespace Transponder.Tests;

internal static class TestHelpers
{
    public static TransponderBus CreateTestBus(
        IMessageSerializer? serializer = null,
        ITransportHostProvider? hostProvider = null,
        TransponderBusRuntimeOptions? options = null)
    {
        var address = new Uri("http://test");
        serializer ??= new JsonMessageSerializer();

        if (hostProvider is null)
        {
            var testHost = new TestTransportHost(address);
            hostProvider = new TransportHostProvider([testHost]);
        }

        // Get all hosts from the provider by trying different schemes
        var hosts = new List<ITransportHost>();
        var testUri = new Uri("http://test");
        if (hostProvider.TryGetHost(testUri, out ITransportHost? host) && host is not null) hosts.Add(host);

        return new TransponderBus(
            address,
            hostProvider,
            hosts,
            serializer,
            options);
    }

    private sealed class TestTransportHost : TransportHostBase
    {
        public TestTransportHost(Uri address) : base(address)
        {
        }
    }
}
