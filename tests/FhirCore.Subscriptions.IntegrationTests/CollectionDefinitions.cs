using Xunit;

namespace FhirCore.Subscriptions.IntegrationTests;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<Dialysis.IntegrationFixtures.PostgresFixture>;
