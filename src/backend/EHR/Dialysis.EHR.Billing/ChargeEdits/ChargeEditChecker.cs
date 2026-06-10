using Dialysis.EHR.Billing.Coding;
using Dialysis.EHR.Billing.Ports;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.Billing.ChargeEdits;

/// <summary>
/// Deterministic charge-review edit checker. Evaluates a charge against configured CPT frequency limits
/// and required-diagnosis coverage rules, escalates a firing edit to an ABN-required block when the payer
/// is Medicare, and flags an under-coding opportunity when a captured E/M charge is below the level the
/// documentation supports. Empty options → no-op.
/// </summary>
public sealed class ChargeEditChecker : IChargeEditChecker
{
    private readonly IChargeRepository _charges;
    private readonly TimeProvider _timeProvider;
    private readonly ChargeEditOptions _options;
    private readonly IEvaluationManagementCoder _coder;

    public ChargeEditChecker(IChargeRepository charges, TimeProvider timeProvider, IOptions<ChargeEditOptions> options, IEvaluationManagementCoder coder)
    {
        _charges = charges;
        _timeProvider = timeProvider;
        _options = options.Value;
        _coder = coder;
    }

    public async Task<ChargeAdvisoryResult> CheckChargeAsync(
        Guid patientId,
        string cptCode,
        IReadOnlyList<string> diagnosisPointerIcd10Codes,
        string? payerCode,
        CancellationToken cancellationToken = default)
    {
        var cpt = cptCode?.Trim() ?? string.Empty;
        if (cpt.Length == 0)
            return ChargeAdvisoryResult.None;

        var advisories = new List<ChargeAdvisory>();

        var freqRule = _options.FrequencyLimits.Find(r => r.CptCode.Equals(cpt, StringComparison.OrdinalIgnoreCase));
        if (freqRule is not null && freqRule.MaxOccurrences > 0)
        {
            var window = freqRule.WindowDays is > 0 ? freqRule.WindowDays.Value : _options.FrequencyWindowDays;
            var since = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-Math.Abs(window));
            var recent = await _charges.ListRecentForPatientAsync(patientId, since, cancellationToken).ConfigureAwait(false);
            var count = recent.Count(c => c.CptCode.Equals(cpt, StringComparison.OrdinalIgnoreCase));
            if (count >= freqRule.MaxOccurrences)
            {
                advisories.Add(new ChargeAdvisory(
                    ChargeAdvisoryCategory.CptFrequencyLimitExceeded,
                    freqRule.Blocking ? ChargeAdvisorySeverity.Blocking : ChargeAdvisorySeverity.Warning,
                    cpt,
                    $"{count} in {window}d",
                    $"Max {freqRule.MaxOccurrences} per {window} days already billed."));
            }
        }

        var covRule = _options.CoverageRules.Find(r => r.CptCode.Equals(cpt, StringComparison.OrdinalIgnoreCase));
        if (covRule is not null && covRule.RequiredAnyIcd10.Count > 0)
        {
            var pointers = (diagnosisPointerIcd10Codes ?? [])
                .Select(d => d?.Trim() ?? string.Empty)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!covRule.RequiredAnyIcd10.Exists(req => pointers.Contains(req.Trim())))
            {
                advisories.Add(new ChargeAdvisory(
                    ChargeAdvisoryCategory.MissingRequiredDiagnosis,
                    covRule.Blocking ? ChargeAdvisorySeverity.Blocking : ChargeAdvisorySeverity.Warning,
                    cpt,
                    string.Join(" / ", covRule.RequiredAnyIcd10),
                    "Covered only with one of the listed diagnoses."));
            }
        }

        // ABN escalation: a non-covered/over-frequency service for a Medicare payer needs an Advance
        // Beneficiary Notice before billing. Surfaced as a distinct blocking advisory. (Evaluated on the
        // coverage/frequency edits only — before the advisory under-coding opportunity is added below.)
        if (advisories.Count > 0 && !string.IsNullOrWhiteSpace(payerCode)
            && _options.MedicarePayerCodes.Exists(p => p.Trim().Equals(payerCode!.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            advisories.Add(new ChargeAdvisory(
                ChargeAdvisoryCategory.AbnRequired,
                ChargeAdvisorySeverity.Blocking,
                cpt,
                payerCode,
                "Medicare may not cover this — obtain an Advance Beneficiary Notice (ABN) before billing."));
        }

        // E/M under-coding: when the captured charge is an E/M code below the level the documented
        // diagnoses support, surface a non-blocking opportunity (the "~50% of visits under-coded" gap).
        if (_coder.IsEmCode(cpt))
        {
            var pointers = (diagnosisPointerIcd10Codes ?? []).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
            var suggestion = _coder.Suggest(pointers, [], pointers.Count);
            var capturedLevel = _coder.LevelOf(cpt);
            if (suggestion is not null && capturedLevel is { } captured && suggestion.Level > captured)
            {
                advisories.Add(new ChargeAdvisory(
                    ChargeAdvisoryCategory.UnderCodingOpportunity,
                    ChargeAdvisorySeverity.Warning,
                    cpt,
                    suggestion.SuggestedCptCode,
                    $"Documentation supports {suggestion.SuggestedCptCode} — {suggestion.Rationale}"));
            }
        }

        return new ChargeAdvisoryResult(advisories);
    }
}
