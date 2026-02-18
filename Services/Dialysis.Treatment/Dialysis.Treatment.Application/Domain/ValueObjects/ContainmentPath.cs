namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// Parsed IEEE 11073 containment path from OBX-4 dotted sub-ID (e.g. "1.1.3.1").
/// Structure: MDS (1) > VMD (1.1) > Channel (1.1.3) > Metric (1.1.3.1).
/// </summary>
public sealed record ContainmentPath
{
    public string Raw { get; }
    public int MdsId { get; }
    public int VmdId { get; }
    public int? ChannelId { get; }
    public int? MetricId { get; }
    public ContainmentLevel Level { get; }

    private ContainmentPath(string raw, int mdsId, int vmdId, int? channelId, int? metricId, ContainmentLevel level)
    {
        Raw = raw;
        MdsId = mdsId;
        VmdId = vmdId;
        ChannelId = channelId;
        MetricId = metricId;
        Level = level;
    }

    /// <summary>
    /// Parses OBX-4 dotted notation (e.g. "1.1.3.1") into structured containment path.
    /// </summary>
    public static ContainmentPath? TryParse(string? subId)
    {
        if (string.IsNullOrWhiteSpace(subId)) return null;

        string[] parts = subId.Split('.');
        if (parts.Length == 0) return null;

        if (!int.TryParse(parts[0], out int mds) || mds < 0) return null;
        int vmd = parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 0;
        int? channel = parts.Length > 2 && int.TryParse(parts[2], out int c) ? c : null;
        int? metric = parts.Length > 3 && int.TryParse(parts[3], out int m) ? m : null;

        ContainmentLevel level = parts.Length switch
        {
            1 => ContainmentLevel.Mds,
            2 => ContainmentLevel.Vmd,
            3 => ContainmentLevel.Channel,
            >= 4 => ContainmentLevel.Metric,
            _ => ContainmentLevel.Mds
        };

        return new ContainmentPath(subId, mds, vmd, channel, metric, level);
    }

    /// <summary>
    /// Maps channel number to IEEE 11073 channel name (per Dialysis Implementation Guide).
    /// </summary>
    public static string? GetChannelName(int channelId) =>
        channelId switch
        {
            1 => "Machine",
            2 => "Anticoag",
            3 => "Blood Pump",
            4 => "Dialysate",
            5 => "Filter",
            6 => "Convective",
            7 => "Safety",
            8 => "Therapy Outcomes",
            9 => "UF",
            10 => "NIBP",
            11 => "Pulse Oximeter",
            12 => "Blood Chemistry",
            _ => null
        };
}
