using Microsoft.Extensions.Options;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>
/// Population outreach configuration, bound from <c>Ehr:Population:Outreach</c>. Real dispatch is
/// off by default; until a patient contact-channel store exists, the resolver hands back a single
/// configured fallback channel/address (e.g. an ops webhook).
/// </summary>
public sealed class OutreachOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:Population:Outreach";

    /// <summary>When false (default), the command builds an audited target list but does not dispatch.</summary>
    public bool Enabled { get; set; }

    /// <summary>Fallback channel for the stub resolver (e.g. "webhook", "sms").</summary>
    public string FallbackChannel { get; set; } = "webhook";

    /// <summary>Fallback address for the stub resolver (no per-patient contact store yet).</summary>
    public string? FallbackAddress { get; set; }
}

/// <summary>A resolved outreach channel/address for a patient (null when none is available).</summary>
public sealed record OutreachContact(string Channel, string Address);

/// <summary>Resolves how to reach a patient for outreach. The default impl is a configured fallback stub.</summary>
public interface IOutreachContactResolver
{
    OutreachContact? Resolve(Guid patientId);
}

/// <summary>
/// Stub resolver: returns the single configured fallback channel/address (or null when unset). The
/// honest stand-in for the missing per-patient contact store — every patient resolves to the same
/// ops/webhook target so the path is exercisable without leaking that contacts aren't modelled yet.
/// </summary>
public sealed class ConfiguredFallbackOutreachContactResolver : IOutreachContactResolver
{
    private readonly OutreachOptions _options;
    public ConfiguredFallbackOutreachContactResolver(IOptions<OutreachOptions> options) => _options = options.Value;

    public OutreachContact? Resolve(Guid patientId) =>
        string.IsNullOrWhiteSpace(_options.FallbackAddress)
            ? null
            : new OutreachContact(_options.FallbackChannel, _options.FallbackAddress);
}
