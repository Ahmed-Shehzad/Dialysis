namespace Dialysis.BuildingBlocks.Tefca.Ti.Smcb;

/// <summary>
/// Composition fallback when no real SMC-B is wired (dev / CI). Reports IsPresent=false and
/// throws on every call — so any path that tries to talk to gematik without a card fails
/// loudly instead of silently masquerading as a connected practice. Production deployments
/// must register <c>Pcsc.PcscSmcBCardReader</c> (lives in a dedicated platform package that
/// pulls in `PCSC.Iso7816` — out-of-scope for this PR's NuGet footprint).
/// </summary>
public sealed class StubSmcBCardReader : ISmcBCardReader
{
    public bool IsPresent => false;

    public Task<SmcBCertificateChain> ReadCertificateChainAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No SMC-B card reader registered. Production deployments must register a real " +
            "ISmcBCardReader implementation (e.g. PcscSmcBCardReader) via the composition root " +
            "before any TI / ePA call.");

    public Task<byte[]> SignAsync(
        ReadOnlyMemory<byte> payload, SmcBKeyKind keyKind, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            "No SMC-B card reader registered. See StubSmcBCardReader.ReadCertificateChainAsync " +
            "for the registration guidance.");
}
