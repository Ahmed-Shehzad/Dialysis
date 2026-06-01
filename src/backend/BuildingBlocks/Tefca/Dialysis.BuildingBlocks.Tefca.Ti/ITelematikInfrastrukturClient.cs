using Dialysis.BuildingBlocks.Tefca.Ti.Endpoints;

namespace Dialysis.BuildingBlocks.Tefca.Ti;

/// <summary>
/// Coarse-grained client for talking to the German TI gateway. Handles:
/// <list type="bullet">
///   <item>OIDC handshake against the gematik IDP (token exchange uses the SMC-B card).</item>
///   <item>Resource calls to ePA (upload / download / discovery).</item>
///   <item>Health-check / handshake probing for the operator dashboard.</item>
/// </list>
/// Concrete implementation lives in this assembly's internal <c>GematikTelematikInfrastrukturClient</c>;
/// tests substitute a fake via this interface.
/// </summary>
public interface ITelematikInfrastrukturClient
{
    /// <summary>The active environment (RU / TU / PU).</summary>
    GematikEnvironment Environment { get; }

    /// <summary>Probes the configured gematik IDP. Returns true on a successful discovery
    /// document fetch; the operator dashboard uses this for the green/yellow/red badge.</summary>
    Task<TiHandshakeResult> HandshakeAsync(CancellationToken cancellationToken);
}

public sealed record TiHandshakeResult(
    bool Succeeded,
    DateTimeOffset CheckedAtUtc,
    string? FailureReason,
    string? IdpIssuer,
    string? SmcBChainFingerprintSha256);
