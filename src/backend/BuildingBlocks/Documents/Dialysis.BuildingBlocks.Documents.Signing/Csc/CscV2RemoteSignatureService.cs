using System.Security.Cryptography.X509Certificates;

namespace Dialysis.BuildingBlocks.Documents.Signing.Csc;

/// <summary>
/// <see cref="IRemoteSignatureService"/> implementation backed by the CSC v2 client.
/// Thin adapter — the wire concerns (SAD acquisition, hash submission, response decoding)
/// live in <see cref="CscV2Client"/>; this type owns the worst-case-size policy and the
/// <see cref="IRemoteSignatureService"/> contract.
/// </summary>
public sealed class CscV2RemoteSignatureService : IRemoteSignatureService
{
    // CSC TSPs don't expose the worst-case PKCS#7 container size through a standard field,
    // so we reserve a conservative 16 KiB slot — enough for a SHA-256 detached CMS with an
    // RFC 3161 timestamp token attribute and a 3-link cert chain.
    private const int DefaultContainerSize = 16 * 1024;

    private readonly CscV2Client _client;

    public CscV2RemoteSignatureService(CscV2Client client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    public string TspId => _client.TspId;

    public Task<int> EstimateSignatureContainerSizeAsync(string credentialId, CancellationToken cancellationToken) =>
        Task.FromResult(DefaultContainerSize);

    public Task<byte[]> SignHashAsync(string credentialId, X509Certificate2 certificate, ReadOnlyMemory<byte> documentHash, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
        ArgumentNullException.ThrowIfNull(certificate);
        return _client.SignHashAsync(credentialId, documentHash, cancellationToken);
    }
}
