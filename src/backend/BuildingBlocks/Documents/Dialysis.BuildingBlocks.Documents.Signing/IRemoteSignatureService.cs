using System.Security.Cryptography.X509Certificates;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>
/// Signs a single document hash with a remote TSP-held key. Used by the eIDAS-QES path
/// where the private key never leaves the trust service provider. The implementation owns
/// the SAD (Signature Activation Data) exchange and the wire protocol — typically CSC v2's
/// <c>/signatures/signHash</c> endpoint.
/// </summary>
public interface IRemoteSignatureService
{
    /// <summary>Identifies the TSP this implementation talks to (e.g. <c>swisscom</c>, <c>infocert</c>).</summary>
    string TspId { get; }

    /// <summary>
    /// Returns the worst-case signature container size in bytes for a credential. Used by
    /// the PDF signer to reserve the placeholder before computing the document byte range.
    /// </summary>
    Task<int> EstimateSignatureContainerSizeAsync(string credentialId, CancellationToken cancellationToken);

    /// <summary>
    /// Submits the document hash to the TSP and returns the PKCS#7 / CMS detached signature
    /// the signer embeds into the PDF.
    /// </summary>
    Task<byte[]> SignHashAsync(
        string credentialId,
        X509Certificate2 certificate,
        ReadOnlyMemory<byte> documentHash,
        CancellationToken cancellationToken);
}
