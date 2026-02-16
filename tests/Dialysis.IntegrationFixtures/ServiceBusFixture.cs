using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Xunit;

namespace Dialysis.IntegrationFixtures;

[CollectionDefinition("ServiceBus")]
public sealed class ServiceBusCollection : ICollectionFixture<ServiceBusFixture>;

/// <summary>
/// Shared Azure Service Bus Emulator container for integration tests.
/// Uses Config.json to pre-create observation-created and hypotension-risk-raised topics with subscriptions.
/// </summary>
public sealed class ServiceBusFixture : IAsyncLifetime
{
    private readonly INetwork _network;
    private readonly IContainer _sqlContainer;
    private readonly IContainer _emulatorContainer;

    public ServiceBusFixture()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("N"))
            .Build();

        _sqlContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", "StrongPassword!1")
            .WithNetwork(_network)
            .WithNetworkAliases("sqledge")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        var configPath = Path.Combine(AppContext.BaseDirectory, "ServiceBusConfig.json");
        if (!File.Exists(configPath))
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "ServiceBusConfig.json");
        }

        var emulatorBuilder = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SQL_SERVER", "sqledge")
            .WithEnvironment("MSSQL_SA_PASSWORD", "StrongPassword!1")
            .WithNetwork(_network)
            .WithNetworkAliases("servicebus-emulator")
            .WithPortBinding(5672, 5672)
            .WithPortBinding(5300, 5300)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged(".*Emulator Service is Successfully Up!.*"));

        if (File.Exists(configPath))
        {
            emulatorBuilder = emulatorBuilder.WithBindMount(configPath, "/ServiceBus_Emulator/ConfigFiles/Config.json");
        }

        _emulatorContainer = emulatorBuilder.Build();
    }

    public string ConnectionString =>
        "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await _sqlContainer.StartAsync();
        await _emulatorContainer.StartAsync();
        await Task.Delay(3000);
    }

    public async Task DisposeAsync()
    {
        await _emulatorContainer.DisposeAsync();
        await _sqlContainer.DisposeAsync();
        await _network.DeleteAsync();
    }
}
