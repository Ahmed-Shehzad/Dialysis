using Microsoft.Extensions.Options;

using NutrientPDF;
using NutrientPDF.Abstractions;

using Shouldly;

using Xunit;

namespace NutrientPDF.Tests;

/// <summary>
/// Integration tests that use the real NutrientPdfService and GdPicture API.
/// Skipped when NUTRIENT_PDF_LICENSE is not set (e.g. CI without license).
/// Run with: NUTRIENT_PDF_LICENSE=your_key dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public sealed class NutrientPdfIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly INutrientPdfService _service;

    public NutrientPdfIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NutrientPdfIntegration_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var licenseKey = Environment.GetEnvironmentVariable("NUTRIENT_PDF_LICENSE") ?? "";
        _service = new NutrientPdfService(Options.Create(new NutrientPdfOptions { LicenseKey = licenseKey }));
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private static bool ShouldSkip()
    {
        var key = Environment.GetEnvironmentVariable("NUTRIENT_PDF_LICENSE");
        return string.IsNullOrWhiteSpace(key);
    }

    private async Task<(string MainPath, string AppendPath)> CreateTestPdfsAsync()
    {
        var mainTxt = Path.Combine(_tempDir, "main.txt");
        var appendTxt = Path.Combine(_tempDir, "append.txt");
        var mainPdf = Path.Combine(_tempDir, "main.pdf");
        var appendPdf = Path.Combine(_tempDir, "append.pdf");

        await File.WriteAllTextAsync(mainTxt, "Main document page 1");
        await File.WriteAllTextAsync(appendTxt, "Appended document page 1");

        await _service.ConvertTextToPdfAsync(mainTxt, mainPdf);
        await _service.ConvertTextToPdfAsync(appendTxt, appendPdf);

        return (mainPdf, appendPdf);
    }

    [Fact]
    public async Task AppendPdfAsync_stream_overload_appends_pages()
    {
        if (ShouldSkip())
        {
            return; // Skip when no license
        }

        var (mainPath, appendPath) = await CreateTestPdfsAsync();
        var resultPath = Path.Combine(_tempDir, "appended.pdf");

        using var mainStream = File.OpenRead(mainPath);
        using var appendStream = File.OpenRead(appendPath);
        using var outputStream = File.Create(resultPath);

        await _service.AppendPdfAsync(mainStream, appendStream, outputStream);

        mainStream.Position = 0;
        appendStream.Position = 0;
        outputStream.Close();

        var pageCount = await _service.GetPdfPageCountAsync(resultPath);
        pageCount.ShouldBe(2);
    }

    [Fact]
    public async Task AddPdfBatesNumberingAsync_stream_overload_adds_labels()
    {
        if (ShouldSkip())
        {
            return;
        }

        var (mainPath, _) = await CreateTestPdfsAsync();
        var resultPath = Path.Combine(_tempDir, "bates.pdf");

        using var sourceStream = File.OpenRead(mainPath);
        using var outputStream = File.Create(resultPath);

        await _service.AddPdfBatesNumberingAsync(sourceStream, outputStream, "DOC-", 1, 4, "");

        outputStream.Close();

        var isValid = await _service.IsValidPdfAsync(resultPath);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task AppendPdfAsync_path_overload_appends_pages()
    {
        if (ShouldSkip())
        {
            return;
        }

        var (mainPath, appendPath) = await CreateTestPdfsAsync();
        var resultPath = Path.Combine(_tempDir, "appended_path.pdf");

        await _service.AppendPdfAsync(mainPath, resultPath, appendPath);

        var pageCount = await _service.GetPdfPageCountAsync(resultPath);
        pageCount.ShouldBe(2);
    }

    [Fact]
    public async Task AddPdfBatesNumberingAsync_path_overload_adds_labels()
    {
        if (ShouldSkip())
        {
            return;
        }

        var (mainPath, _) = await CreateTestPdfsAsync();
        var resultPath = Path.Combine(_tempDir, "bates_path.pdf");

        await _service.AddPdfBatesNumberingAsync(mainPath, resultPath, "DOC-", 1);

        var isValid = await _service.IsValidPdfAsync(resultPath);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPdfPageCountAsync_returns_count_for_valid_pdf()
    {
        if (ShouldSkip())
        {
            return;
        }

        var (mainPath, _) = await CreateTestPdfsAsync();
        var count = await _service.GetPdfPageCountAsync(mainPath);
        count.ShouldBe(1);
    }
}
