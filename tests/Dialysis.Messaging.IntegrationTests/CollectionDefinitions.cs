using Xunit;

namespace Dialysis.Messaging.IntegrationTests;

[CollectionDefinition("ServiceBus")]
public sealed class ServiceBusCollection : ICollectionFixture<Dialysis.IntegrationFixtures.ServiceBusFixture>;
