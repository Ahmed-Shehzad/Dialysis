namespace NutrientPDF.Abstractions;

/// <summary>
/// PDF metadata, embedded files, bookmarks, encryption. Segregated interface (ISP).
/// </summary>
public interface IPdfMetadataService
{
    Task<string> GetPdfMetadataAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<string> GetPdfMetadataAsync(Stream sourceStream, CancellationToken cancellationToken = default);
    Task SetPdfMetadataAsync(string sourcePath, string outputPath, PdfMetadata metadata, CancellationToken cancellationToken = default);
    Task SetPdfMetadataAsync(Stream sourceStream, Stream outputStream, PdfMetadata metadata, CancellationToken cancellationToken = default);
    Task AttachFileToPdfAsync(string sourcePath, string outputPath, string fileToAttach, int pageNumber, string? description = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfEmbeddedFileInfo>> GetPdfEmbeddedFilesAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task ExtractPdfEmbeddedFileAsync(string sourcePath, int fileIndex, string outputPath, CancellationToken cancellationToken = default);
    Task ExtractPdfEmbeddedFileAsync(string sourcePath, int fileIndex, Stream outputStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfBookmark>> GetPdfBookmarksAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfBookmark>> GetPdfBookmarksAsync(Stream sourceStream, CancellationToken cancellationToken = default);
    Task<int> AddPdfBookmarkAsync(string sourcePath, string outputPath, string title, int pageNumber, int? parentId = null, CancellationToken cancellationToken = default);
    Task RemovePdfBookmarkAsync(string sourcePath, string outputPath, int bookmarkId, CancellationToken cancellationToken = default);
    Task UpdatePdfBookmarkAsync(string sourcePath, string outputPath, int bookmarkId, string? newTitle = null, int? newPageNumber = null, CancellationToken cancellationToken = default);
    Task EncryptPdfAsync(string sourcePath, string outputPath, string? userPassword, string? ownerPassword = null, PdfEncryptionLevel encryptionLevel = PdfEncryptionLevel.Aes256, CancellationToken cancellationToken = default);
    Task EncryptPdfAsync(Stream sourceStream, Stream outputStream, string? userPassword, string? ownerPassword = null, PdfEncryptionLevel encryptionLevel = PdfEncryptionLevel.Aes256, CancellationToken cancellationToken = default);
    Task DecryptPdfAsync(string sourcePath, string outputPath, string ownerPassword, CancellationToken cancellationToken = default);
    Task DecryptPdfAsync(Stream sourceStream, Stream outputStream, string ownerPassword, CancellationToken cancellationToken = default);

    Task<PdfMetadata> GetPdfMetadataStructuredAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<PdfMetadata> GetPdfMetadataStructuredAsync(Stream sourceStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfEmbeddedFileInfo>> GetPdfEmbeddedFilesAsync(Stream sourceStream, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfExtractedImageInfo>> ExtractPdfImagesAsync(string sourcePath, int? pageNumber = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfAnnotationInfo>> GetPdfAnnotationsAsync(string sourcePath, int? pageNumber = null, CancellationToken cancellationToken = default);
}
