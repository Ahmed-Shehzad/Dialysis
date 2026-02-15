namespace Transponder.Persistence.EntityFramework.PostgreSql.Tests;

public sealed class PostgreSqlStorageOptionsTests
{
    [Fact]
    public void Defaults_Are_Set()
    {
        var options = new PostgreSqlStorageOptions();

        Assert.Equal("public", options.Schema);
        Assert.True(options.UseAdvisoryLocks);
        Assert.True(options.UseSkipLocked);
    }
}
