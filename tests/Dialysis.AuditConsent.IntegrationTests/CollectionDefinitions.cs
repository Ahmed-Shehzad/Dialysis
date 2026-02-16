using Xunit;

namespace Dialysis.AuditConsent.IntegrationTests;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<Dialysis.IntegrationFixtures.PostgresFixture>;
