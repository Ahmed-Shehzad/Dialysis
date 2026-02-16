using Xunit;

namespace Dialysis.E2ETests;

[CollectionDefinition("ServiceBus")]
public sealed class ServiceBusCollection : ICollectionFixture<Dialysis.IntegrationFixtures.ServiceBusFixture>;
