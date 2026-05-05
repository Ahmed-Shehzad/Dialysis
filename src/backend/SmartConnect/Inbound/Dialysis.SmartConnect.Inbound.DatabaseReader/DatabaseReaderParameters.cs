using Dialysis.SmartConnect.ExtendedPlugins;

namespace Dialysis.SmartConnect.Inbound.DatabaseReader;

/// <summary>
/// Parsed, validated parameters for <see cref="DatabaseReaderSourceConnector"/>.
/// </summary>
internal sealed class DatabaseReaderParameters
{
    public required DatabaseProvider Provider { get; init; }
    public required string ConnectionStringName { get; init; }
    public required string PollSql { get; init; }
    public required string WatermarkColumn { get; init; }
    public required WatermarkType WatermarkType { get; init; }
    public required int PollIntervalSeconds { get; init; }
    public required bool DeleteAfterRead { get; init; }
    public string? DeleteSql { get; init; }

    public static DatabaseReaderParameters Parse(IReadOnlyDictionary<string, string> raw)
    {
        var providerStr = raw.GetValueOrDefault("Provider") ?? "";
        if (!Enum.TryParse<DatabaseProvider>(providerStr, true, out var provider))
        {
            throw new ArgumentException($"Provider '{providerStr}' is not valid. Use SqlServer or Postgres.");
        }

        var connName = raw.GetValueOrDefault("ConnectionStringName");
        if (string.IsNullOrWhiteSpace(connName))
        {
            throw new ArgumentException("ConnectionStringName is required.");
        }

        var pollSql = raw.GetValueOrDefault("PollSql");
        if (string.IsNullOrWhiteSpace(pollSql))
        {
            throw new ArgumentException("PollSql is required.");
        }

        var wmCol = raw.GetValueOrDefault("WatermarkColumn") ?? "";
        var wmTypeStr = raw.GetValueOrDefault("WatermarkType") ?? "Long";
        if (!Enum.TryParse<WatermarkType>(wmTypeStr, true, out var wmType))
        {
            throw new ArgumentException($"WatermarkType '{wmTypeStr}' is not valid. Use Long or DateTime.");
        }

        var interval = 60;
        if (raw.TryGetValue("PollIntervalSeconds", out var intStr) && int.TryParse(intStr, out var parsed) && parsed > 0)
        {
            interval = parsed;
        }

        var deleteAfterRead = false;
        if (raw.TryGetValue("DeleteAfterRead", out var delStr))
        {
            deleteAfterRead = string.Equals(delStr, "true", StringComparison.OrdinalIgnoreCase);
        }

        var deleteSql = raw.GetValueOrDefault("DeleteSql");

        return new DatabaseReaderParameters
        {
            Provider = provider,
            ConnectionStringName = connName!,
            PollSql = pollSql!,
            WatermarkColumn = wmCol,
            WatermarkType = wmType,
            PollIntervalSeconds = interval,
            DeleteAfterRead = deleteAfterRead,
            DeleteSql = deleteSql,
        };
    }
}

/// <summary>Type of watermark column for ordering / filtering new rows.</summary>
public enum WatermarkType
{
    Long,
    DateTime,
}
