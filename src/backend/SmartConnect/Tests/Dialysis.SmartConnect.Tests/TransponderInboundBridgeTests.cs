using Dialysis.SmartConnect.Inbound.Transponder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class TransponderInboundBridgeTests
{
    [Fact]
    public void Bridge_is_NOT_registered_when_config_flag_is_absent()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        services.AddSmartConnectTransponderInboundBridgeIfEnabled(config);

        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(TransponderInboundTransportBridge));
    }

    [Fact]
    public void Bridge_is_registered_as_IHostedService_when_config_flag_is_true()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmartConnect:Inbound:TransponderBridge:Enabled"] = "true",
            })
            .Build();

        services.AddSmartConnectTransponderInboundBridgeIfEnabled(config);

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(TransponderInboundTransportBridge));
    }
}
