using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Dicom.Ai;

/// <summary>
/// Governed orchestrator for AI-assisted imaging reads. Enforces the policy around the raw
/// <see cref="IImagingInferenceProvider"/>:
/// <list type="bullet">
///   <item>runs only when <see cref="ImagingAiOptions.Enabled"/> (feature flag, default off);</item>
///   <item>drops findings below <see cref="ImagingAiOptions.MinConfidence"/>;</item>
///   <item>marks every surfaced finding <c>RequiresHumanReview</c> — AI output is advisory, never
///         auto-final/diagnostic (human-in-the-loop);</item>
///   <item>audits every attempt (produced or not) for governance / bias review.</item>
/// </list>
/// Returns <see langword="null"/> when AI is disabled or no qualifying finding was produced.
/// </summary>
public sealed class ImagingAiAnalyzer
{
    private readonly IImagingInferenceProvider _provider;
    private readonly IImagingAiAuditSink _audit;
    private readonly IImagingFindingCodeValidator _codeValidator;
    private readonly ImagingAiOptions _options;
    private readonly TimeProvider _clock;

    public ImagingAiAnalyzer(
        IImagingInferenceProvider provider,
        IImagingAiAuditSink audit,
        IImagingFindingCodeValidator codeValidator,
        IOptions<ImagingAiOptions> options,
        TimeProvider clock)
    {
        _provider = provider;
        _audit = audit;
        _codeValidator = codeValidator;
        _options = options.Value;
        _clock = clock;
    }

    /// <summary>Whether AI imaging is currently enabled by configuration.</summary>
    public bool IsEnabled => _options.Enabled;

    public async Task<ImagingAiAssessment?> AnalyzeAsync(ImagingInferenceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_options.Enabled)
        {
            return null;
        }

        var finding = await _provider.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);
        var now = _clock.GetUtcNow();

        if (finding is null || finding.Confidence < _options.MinConfidence)
        {
            await _audit.RecordAsync(
                new ImagingAiAuditEntry(_provider.ModelId, request.StudyInstanceUid, request.AccessionNumber,
                    FindingProduced: false, Code: finding?.Code, Confidence: finding?.Confidence, AtUtc: now),
                cancellationToken).ConfigureAwait(false);
            return null;
        }

        // Terminology governance: a finding may only surface a code from the platform's governed
        // imaging value set. An ungoverned code is dropped (never reaches the chart) but the attempt is
        // audited with the offending code so model drift / vocabulary mismatch is visible for review.
        if (!await _codeValidator.IsGovernedAsync(finding.System, finding.Code, cancellationToken).ConfigureAwait(false))
        {
            await _audit.RecordAsync(
                new ImagingAiAuditEntry(_provider.ModelId, request.StudyInstanceUid, request.AccessionNumber,
                    FindingProduced: false, Code: finding.Code, Confidence: finding.Confidence, AtUtc: now),
                cancellationToken).ConfigureAwait(false);
            return null;
        }

        await _audit.RecordAsync(
            new ImagingAiAuditEntry(_provider.ModelId, request.StudyInstanceUid, request.AccessionNumber,
                FindingProduced: true, Code: finding.Code, Confidence: finding.Confidence, AtUtc: now),
            cancellationToken).ConfigureAwait(false);

        // AI output is advisory — always flagged for human review.
        return new ImagingAiAssessment(_provider.ModelId, finding, RequiresHumanReview: true, ProducedAtUtc: now);
    }
}
