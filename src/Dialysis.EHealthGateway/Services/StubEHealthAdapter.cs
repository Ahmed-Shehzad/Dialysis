namespace Dialysis.EHealthGateway.Services;

/// <summary>Stub adapter – eHealth platforms require certification; use for development/documentation.</summary>
public sealed class StubEHealthAdapter : IEHealthPlatformAdapter
{
    private readonly string _platformId;

    public StubEHealthAdapter(string platformId)
    {
        _platformId = platformId;
    }

    public string PlatformId => _platformId;

    public Task<EHealthUploadResult> UploadAsync(
        byte[] documentContent,
        string patientIdentifier,
        string? documentType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EHealthUploadResult(
            Success: false,
            DocumentId: null,
            Error: $"eHealth platform '{_platformId}' requires certification; stub adapter does not perform real uploads."));
    }

    public Task<IReadOnlyList<EHealthDocumentInfo>> ListDocumentsAsync(
        string patientIdentifier,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<EHealthDocumentInfo>>([]);
    }
}
