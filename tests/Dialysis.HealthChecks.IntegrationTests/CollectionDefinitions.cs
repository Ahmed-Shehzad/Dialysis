using Xunit;

namespace Dialysis.HealthChecks.IntegrationTests;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<Dialysis.IntegrationFixtures.PostgresFixture>;
