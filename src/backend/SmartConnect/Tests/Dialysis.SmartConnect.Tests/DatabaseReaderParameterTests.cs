using Dialysis.SmartConnect.Inbound.DatabaseReader;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class DatabaseReaderParameterTests
{
    [Fact]
    public void Parse_valid_parameters()
    {
        var raw = new Dictionary<string, string>
        {
            ["Provider"] = "SqlServer",
            ["ConnectionStringName"] = "MyConn",
            ["PollSql"] = "SELECT * FROM Msgs WHERE Id > @watermark",
            ["WatermarkColumn"] = "Id",
            ["WatermarkType"] = "Long",
            ["PollIntervalSeconds"] = "30",
            ["DeleteAfterRead"] = "true",
            ["DeleteSql"] = "DELETE FROM Msgs WHERE Id <= @watermark",
        };

        var p = DatabaseReaderParameters.Parse(raw);

        Assert.Equal(Dialysis.SmartConnect.ExtendedPlugins.DatabaseProvider.SqlServer, p.Provider);
        Assert.Equal("MyConn", p.ConnectionStringName);
        Assert.Equal("SELECT * FROM Msgs WHERE Id > @watermark", p.PollSql);
        Assert.Equal("Id", p.WatermarkColumn);
        Assert.Equal(WatermarkType.Long, p.WatermarkType);
        Assert.Equal(30, p.PollIntervalSeconds);
        Assert.True(p.DeleteAfterRead);
        Assert.Equal("DELETE FROM Msgs WHERE Id <= @watermark", p.DeleteSql);
    }

    [Fact]
    public void Parse_throws_on_missing_provider()
    {
        var raw = new Dictionary<string, string>
        {
            ["ConnectionStringName"] = "X",
            ["PollSql"] = "SELECT 1",
        };

        Assert.Throws<ArgumentException>(() => DatabaseReaderParameters.Parse(raw));
    }

    [Fact]
    public void Parse_throws_on_missing_connection_string()
    {
        var raw = new Dictionary<string, string>
        {
            ["Provider"] = "Postgres",
            ["PollSql"] = "SELECT 1",
        };

        Assert.Throws<ArgumentException>(() => DatabaseReaderParameters.Parse(raw));
    }

    [Fact]
    public void Parse_throws_on_missing_poll_sql()
    {
        var raw = new Dictionary<string, string>
        {
            ["Provider"] = "Postgres",
            ["ConnectionStringName"] = "X",
        };

        Assert.Throws<ArgumentException>(() => DatabaseReaderParameters.Parse(raw));
    }

    [Fact]
    public void Parse_defaults_interval_to_60()
    {
        var raw = new Dictionary<string, string>
        {
            ["Provider"] = "Postgres",
            ["ConnectionStringName"] = "X",
            ["PollSql"] = "SELECT 1",
        };

        var p = DatabaseReaderParameters.Parse(raw);

        Assert.Equal(60, p.PollIntervalSeconds);
    }
}
