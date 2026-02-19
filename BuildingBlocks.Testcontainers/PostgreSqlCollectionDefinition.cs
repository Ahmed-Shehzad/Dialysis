#pragma warning disable IDE0005
using Xunit;
#pragma warning restore IDE0005

namespace BuildingBlocks.Testcontainers;

/// <summary>
/// Collection name for Testcontainers PostgreSQL. Use [Collection(PostgreSqlCollection.Name)] on test classes.
/// </summary>
public static class PostgreSqlCollection
{
    public const string Name = "PostgreSqlCollection";
}

/// <summary>
/// xUnit collection definition. Include via Compile Link so it compiles into the test assembly.
/// xUnit requires the collection definition and tests to be in the same assembly.
/// </summary>
[CollectionDefinition(PostgreSqlCollection.Name)]
public sealed class PostgreSqlCollectionDefinition : ICollectionFixture<PostgreSqlFixture>;
