namespace Dialysis.Treatment.Infrastructure.AlarmApi;

public sealed class AlarmApiOptions
{
    public const string SectionName = "AlarmApi";

    /// <summary>
    /// Base URL for Alarm API (e.g. http://gateway:5000 or http://alarm-api:8080).
    /// When null or empty, threshold breach consumer only logs (no cross-context alarm creation).
    /// </summary>
    public string? BaseUrl { get; set; }
}
