namespace Transponder.Persistence.EntityFramework.SqlServer.Tests;

public sealed class SqlServerStorageOptionsTests
{
    [Fact]
    public void Defaults_Are_Set()
    {
        var options = new SqlServerStorageOptions();

        Assert.Equal("dbo", options.Schema);
        Assert.True(options.UseSnapshotIsolation);
        Assert.Null(options.OutboxLockHint);
    }
}
