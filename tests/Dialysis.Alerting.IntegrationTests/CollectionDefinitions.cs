using Xunit;

namespace Dialysis.Alerting.IntegrationTests;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<Dialysis.IntegrationFixtures.PostgresFixture>;
