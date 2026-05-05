namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>Supported database providers for <see cref="DatabaseOutboundAdapter"/>.</summary>
public enum DatabaseProvider
{
    SqlServer = 0,
    Postgres = 1,
}

/// <summary>Where a parameter value is sourced from when binding the SQL command.</summary>
public enum DatabaseParameterSource
{
    /// <summary>Raw payload bytes (binary).</summary>
    Payload = 0,

    /// <summary>Payload bytes decoded as UTF-8 text.</summary>
    PayloadAsString = 1,

    /// <summary>Value of <see cref="IntegrationMessage.Metadata"/> for the configured key (<c>Path</c>).</summary>
    Metadata = 2,

    /// <summary><see cref="IntegrationMessage.CorrelationId"/>.</summary>
    CorrelationId = 3,

    /// <summary><see cref="IntegrationMessage.FlowId"/>.</summary>
    FlowId = 4,

    /// <summary><see cref="IntegrationMessage.Id"/>.</summary>
    MessageId = 5,

    /// <summary><see cref="IntegrationMessage.ReceivedAtUtc"/>.</summary>
    ReceivedAtUtc = 6,

    /// <summary>Literal string from <c>Path</c>.</summary>
    Literal = 7,
}

public sealed class DatabaseParameterBinding
{
    public string Name { get; set; } = "";

    public DatabaseParameterSource Source { get; set; } = DatabaseParameterSource.PayloadAsString;

    /// <summary>Metadata key (Source=Metadata) or literal value (Source=Literal).</summary>
    public string? Path { get; set; }
}

public sealed class DatabaseOutboundParameters
{
    public DatabaseProvider Provider { get; set; }

    /// <summary>Configuration connection-string name; never inline a connection string in pipeline JSON.</summary>
    public string? ConnectionStringName { get; set; }

    public string? Sql { get; set; }

    public List<DatabaseParameterBinding> Parameters { get; set; } = [];

    public int CommandTimeoutSeconds { get; set; } = 30;
}
