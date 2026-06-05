using Dialysis.SmartConnect.Dicom.Ai;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Dicom;

/// <summary>
/// Coverage for the governed AI imaging analyzer (feature-flag gate, confidence floor,
/// human-in-the-loop flag, audit) and the shipped sample provider.
/// </summary>
public sealed class ImagingAiAnalyzerTests
{
    private static readonly ImagingInferenceRequest _avfUltrasound =
        new("1.2.840.1", "US", "VascularAccess", "IMG-1");

    private static ImagingAiAnalyzer Analyzer(
        ImagingAiOptions options,
        out RecordingAudit audit,
        IImagingInferenceProvider? provider = null,
        IImagingFindingCodeValidator? codeValidator = null)
    {
        audit = new RecordingAudit();
        return new ImagingAiAnalyzer(
            provider ?? new SampleHeuristicImagingInferenceProvider(),
            audit,
            codeValidator ?? new PermissiveImagingFindingCodeValidator(),
            Options.Create(options),
            TimeProvider.System);
    }

    [Fact]
    public async Task Returns_Null_When_Disabled_Async()
    {
        var analyzer = Analyzer(new ImagingAiOptions { Enabled = false }, out var audit);

        var result = await analyzer.AnalyzeAsync(_avfUltrasound, CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(audit.Entries); // disabled → provider never runs, nothing audited
    }

    [Fact]
    public async Task Produces_Human_Review_Assessment_When_Enabled_Async()
    {
        var analyzer = Analyzer(new ImagingAiOptions { Enabled = true, MinConfidence = 0.5 }, out var audit);

        var result = await analyzer.AnalyzeAsync(_avfUltrasound, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.RequiresHumanReview);
        Assert.Equal("sample-heuristic-v0", result.ModelId);
        Assert.Equal("http://radlex.org", result.Finding.System);
        Assert.Contains("SAMPLE MODEL", result.Finding.Summary);
        var entry = Assert.Single(audit.Entries);
        Assert.True(entry.FindingProduced);
    }

    [Fact]
    public async Task Drops_Finding_Below_Confidence_Floor_And_Audits_Async()
    {
        var analyzer = Analyzer(new ImagingAiOptions { Enabled = true, MinConfidence = 0.99 }, out var audit);

        var result = await analyzer.AnalyzeAsync(_avfUltrasound, CancellationToken.None);

        Assert.Null(result); // sample confidence ~0.62 < 0.99
        var entry = Assert.Single(audit.Entries);
        Assert.False(entry.FindingProduced);
    }

    [Fact]
    public async Task Drops_Finding_With_Ungoverned_Code_And_Audits_Async()
    {
        var analyzer = Analyzer(
            new ImagingAiOptions { Enabled = true, MinConfidence = 0.5 },
            out var audit,
            codeValidator: new RejectingCodeValidator());

        var result = await analyzer.AnalyzeAsync(_avfUltrasound, CancellationToken.None);

        Assert.Null(result); // code not in the governed value set → not surfaced
        var entry = Assert.Single(audit.Entries);
        Assert.False(entry.FindingProduced); // but the attempt (with the offending code) is audited
        Assert.Equal("RID39055", entry.Code);
    }

    [Fact]
    public async Task Sample_Provider_Declines_Unknown_Modality_Async()
    {
        var finding = await new SampleHeuristicImagingInferenceProvider()
            .AnalyzeAsync(new ImagingInferenceRequest("1.2.3", "ZZ", "Nowhere", null), CancellationToken.None);

        Assert.Null(finding);
    }

    private sealed class RejectingCodeValidator : IImagingFindingCodeValidator
    {
        public ValueTask<bool> IsGovernedAsync(string system, string code, CancellationToken cancellationToken) => new(false);
    }

    private sealed class RecordingAudit : IImagingAiAuditSink
    {
        public List<ImagingAiAuditEntry> Entries { get; } = [];
        public Task RecordAsync(ImagingAiAuditEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
