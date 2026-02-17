using NutrientPDF.Abstractions;
using NutrientPDF.Abstractions.Options;
using NutrientPDF.Decorators;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NutrientPDF.Tests;

public sealed class ValidatingNutrientPdfServiceTests
{
    private readonly INutrientPdfService _inner;
    private readonly INutrientPdfService _sut;

    public ValidatingNutrientPdfServiceTests()
    {
        _inner = Substitute.For<INutrientPdfService>();
        _sut = new ValidatingNutrientPdfService(_inner);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPdfPageCountAsync_throws_on_null_or_blank_sourcePath(string? path)
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.GetPdfPageCountAsync(path!));
        await _inner.DidNotReceive().GetPdfPageCountAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPdfPageCountAsync_delegates_to_inner_when_valid()
    {
        _inner.GetPdfPageCountAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(5);
        var result = await _sut.GetPdfPageCountAsync("valid.pdf");
        result.ShouldBe(5);
    }

    [Fact]
    public async Task FillPdfFormFieldsAsync_throws_on_null_fieldValues()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.FillPdfFormFieldsAsync("a.pdf", "b.pdf", null!));
        await _inner.DidNotReceive().FillPdfFormFieldsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlattenPdfFormFieldsAsync_throws_on_page_number_less_than_one()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            _sut.FlattenPdfFormFieldsAsync("a.pdf", "b.pdf", 0));
        await _inner.DidNotReceive().FlattenPdfFormFieldsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPdfMetadataAsync_throws_on_null_metadata()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.SetPdfMetadataAsync("a.pdf", "b.pdf", null!));
    }

    [Fact]
    public async Task ExportPdfFormToXfdfAsync_stream_throws_on_null_streams()
    {
        using var ms = new MemoryStream();
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.ExportPdfFormToXfdfAsync(null!, ms, false));
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.ExportPdfFormToXfdfAsync(ms, null!, false));
    }

    [Fact]
    public async Task GetPdfPageSizeAsync_throws_on_page_number_less_than_one()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            _sut.GetPdfPageSizeAsync("a.pdf", 0));
    }

    [Fact]
    public async Task ExtractTextFromPageAsync_throws_on_page_number_less_than_one()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            _sut.ExtractTextFromPageAsync("a.pdf", 0));
    }

    [Fact]
    public async Task MergeToPdfAsync_stream_throws_on_empty_sources()
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            _sut.MergeToPdfAsync(Array.Empty<PdfMergeSource>(), new MemoryStream()));
    }

    [Fact]
    public async Task ImportPdfFormFromXfdfAsync_stream_throws_on_null_streams()
    {
        using var ms = new MemoryStream();
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.ImportPdfFormFromXfdfAsync(null!, ms, ms, true, false));
    }

    [Fact]
    public async Task RedactPdfRegionsAsync_throws_on_empty_regions()
    {
        await Should.ThrowAsync<ArgumentException>(() =>
            _sut.RedactPdfRegionsAsync("a.pdf", "b.pdf", Array.Empty<PdfRedactionRegion>()));
    }

    [Fact]
    public async Task RedactPdfRegionsAsync_stream_throws_on_null_regions()
    {
        using var ms = new MemoryStream();
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.RedactPdfRegionsAsync(ms, ms, null!));
    }

    [Fact]
    public async Task ExtractPdfEmbeddedFileAsync_throws_on_negative_fileIndex()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            _sut.ExtractPdfEmbeddedFileAsync("a.pdf", -1, "out.dat"));
    }

    [Fact]
    public async Task ConvertToSearchablePdfAsync_OcrOptions_throws_on_null_options()
    {
        using var ms = new MemoryStream();
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.ConvertToSearchablePdfAsync(ms, ms, (OcrOptions)null!));
    }

    [Fact]
    public async Task GetPdfMetadataStructuredAsync_delegates_to_inner()
    {
        var expected = new PdfMetadata("Title", "Author");
        _inner.GetPdfMetadataStructuredAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expected);
        var result = await _sut.GetPdfMetadataStructuredAsync("doc.pdf");
        result.ShouldBe(expected);
    }

    [Fact]
    public async Task AppendPdfAsync_delegates_to_inner()
    {
        _inner.AppendPdfAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<int>?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        await _sut.AppendPdfAsync("main.pdf", "out.pdf", "append.pdf");
        await _inner.Received(1).AppendPdfAsync("main.pdf", "out.pdf", "append.pdf", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AppendPdfAsync_stream_overload_delegates_to_inner()
    {
        using var main = new MemoryStream();
        using var append = new MemoryStream();
        using var output = new MemoryStream();
        _inner.AppendPdfAsync(Arg.Any<Stream>(), Arg.Any<Stream>(), Arg.Any<Stream>(), Arg.Any<IEnumerable<int>?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        await _sut.AppendPdfAsync(main, append, output);
        await _inner.Received(1).AppendPdfAsync(main, append, output, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddPdfBatesNumberingAsync_stream_overload_delegates_to_inner()
    {
        using var src = new MemoryStream();
        using var outStream = new MemoryStream();
        _inner.AddPdfBatesNumberingAsync(Arg.Any<Stream>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        await _sut.AddPdfBatesNumberingAsync(src, outStream, "DOC-", 1, 4, "");
        await _inner.Received(1).AddPdfBatesNumberingAsync(src, outStream, "DOC-", 1, 4, "", Arg.Any<CancellationToken>());
    }
}
