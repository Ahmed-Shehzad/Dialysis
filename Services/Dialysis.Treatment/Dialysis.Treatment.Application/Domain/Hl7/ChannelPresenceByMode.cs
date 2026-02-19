using System.Collections.Frozen;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Hl7;

/// <summary>
/// Determines which IEEE 11073 channels are present based on therapy mode (IMPLEMENTATION_PLAN § 3.3.5).
/// </summary>
public static class ChannelPresenceByMode
{
    /// <summary>
    /// Channel IDs per ContainmentPath (1.1.1 Machine, 1.1.2 Anticoag, etc.).
    /// </summary>
    public const int Machine = 1;
    public const int Anticoag = 2;
    public const int BloodPump = 3;
    public const int Dialysate = 4;
    public const int Filter = 5;
    public const int Convective = 6;
    public const int Safety = 7;
    public const int TherapyOutcomes = 8;
    public const int Uf = 9;

    private static readonly FrozenDictionary<string, FrozenSet<int>> PresenceMap = BuildPresenceMap();

    private static FrozenDictionary<string, FrozenSet<int>> BuildPresenceMap()
    {
        var map = new Dictionary<string, FrozenSet<int>>(StringComparer.OrdinalIgnoreCase);

        map["IDL"] = [Machine];
        map["SVC"] = [Machine];

        map["HD"] = [Machine, Anticoag, BloodPump, Dialysate, Filter, Safety, TherapyOutcomes, Uf];
        map["HDF"] = [Machine, Anticoag, BloodPump, Dialysate, Filter, Convective, Safety, TherapyOutcomes, Uf];
        map["HF"] = [Machine, Anticoag, BloodPump, Filter, Convective, Safety, TherapyOutcomes, Uf];
        map["IUF"] = [Machine, Anticoag, BloodPump, Filter, Safety, TherapyOutcomes, Uf];

        map["SLED"] = [Machine, Anticoag, BloodPump, Dialysate, Filter, Safety, TherapyOutcomes, Uf];
        map["HP"] = [Machine, Anticoag, BloodPump, Filter, Safety, TherapyOutcomes, Uf];

        return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the set of channel IDs present for the given mode of operation.
    /// For Idle/Service, only Machine channel is present.
    /// </summary>
    public static IReadOnlySet<int> GetChannelsForMode(ModeOfOperation mode)
    {
        return PresenceMap.GetValueOrDefault(mode.Value, [Machine]);
    }

    /// <summary>
    /// Returns the set of channel IDs present for the given treatment modality.
    /// </summary>
    public static IReadOnlySet<int> GetChannelsForModality(TreatmentModality modality)
    {
        return PresenceMap.GetValueOrDefault(modality.Value, [Machine]);
    }

    /// <summary>
    /// Returns true if the channel (1.1.x) is present for the given modality.
    /// </summary>
    public static bool IsChannelPresent(int channelId, TreatmentModality modality)
    {
        FrozenSet<int> channels = PresenceMap.GetValueOrDefault(modality.Value, [Machine]);
        return channels.Contains(channelId);
    }

    /// <summary>
    /// Returns true if the channel is present for Idle/Service mode.
    /// </summary>
    public static bool IsChannelPresent(int channelId, ModeOfOperation mode)
    {
        FrozenSet<int> channels = PresenceMap.GetValueOrDefault(mode.Value, [Machine]);
        return channels.Contains(channelId);
    }
}
