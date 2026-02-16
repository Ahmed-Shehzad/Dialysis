using Azure.Messaging.ServiceBus;
using Dialysis.Contracts.Events;
using Dialysis.Contracts.Ids;
using Dialysis.IntegrationFixtures;
using Dialysis.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.Messaging.IntegrationTests;

[Collection("ServiceBus")]
[Trait("Category", "ServiceBus")]
public sealed class ServiceBusIntegrationTests
{
    private readonly ServiceBusFixture _fixture;

    public ServiceBusIntegrationTests(ServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Transponder_publish_ObservationCreated_with_connection_string()
    {
        var connectionString = _fixture.ConnectionString;
        var address = new Uri("sb://dialysis/test");
        var services = new ServiceCollection();
        services.AddDialysisTransponder(address, connectionString);
        await using var provider = services.BuildServiceProvider();

        var publishEndpoint = provider.GetRequiredService<Transponder.Abstractions.IPublishEndpoint>();
        var evt = new ObservationCreated(
            Ulid.NewUlid(),
            "default",
            ObservationId.Create("obs-1"),
            PatientId.Create("p1"),
            EncounterId.Create("e1"),
            "8480-6",
            "120",
            DateTimeOffset.UtcNow,
            null);

        await publishEndpoint.PublishAsync(evt);
        await Task.Delay(500);

        await using var client = new ServiceBusClient(connectionString);
        await using var receiver = client.CreateReceiver("observation-created", "prediction-subscription");
        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

        message.ShouldNotBeNull();
    }
}
