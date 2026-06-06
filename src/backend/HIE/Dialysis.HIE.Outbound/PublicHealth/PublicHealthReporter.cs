using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.PublicHealth;

/// <summary>
/// Electronic case reporting: when a reportable finding is detected, queue a report to the configured
/// public-health authority under the <c>PublicHealth</c> purpose.
///
/// <para><b>Mandated-reporting consent bypass.</b> HIPAA permits disclosure to an authorized
/// public-health authority without individual consent. This is the one outbound path that
/// intentionally does <i>not</i> call the consent gate. It is narrowly gated — only a configured
/// reportable code, only to the configured authority partner, only under the PublicHealth purpose —
/// and every disclosure is audited (structured log below; the FHIR <c>AuditEvent.purposeOfEvent</c>
/// profile applies). All other outbound paths remain fully consent-gated.</para>
/// </summary>
public sealed class PublicHealthReporter
{
    private static string SerializeFhirJson(Resource resource) => resource.ToJson();

    private readonly IOutboundBundleStore _store;
    private readonly IReportabilityClassifier _classifier;
    private readonly PublicHealthReportingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PublicHealthReporter> _logger;

    public PublicHealthReporter(
        IOutboundBundleStore store,
        IReportabilityClassifier classifier,
        IOptions<PublicHealthReportingOptions> options,
        TimeProvider timeProvider,
        ILogger<PublicHealthReporter> logger)
    {
        _store = store;
        _classifier = classifier;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Reports <paramref name="resource"/> for <paramref name="patientId"/> when <paramref name="reportableCode"/>
    /// is reportable and reporting is configured. Returns true when a report was queued.
    /// </summary>
    public async Task<bool> ReportAsync(Guid patientId, Resource resource, string? reportableCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (!_options.Enabled || !_classifier.IsReportable(reportableCode))
            return false;

        // Consent gate intentionally bypassed — see the type doc (mandated reporting).
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var bundle = new OutboundBundle(
            patientId,
            resource.TypeName,
            resource.Id ?? Guid.NewGuid().ToString(),
            _options.AuthorityPartnerId!,
            SerializeFhirJson(resource),
            now,
            TefcaPermittedPurposes.PublicHealth);
        await _store.AddAsync(bundle, cancellationToken).ConfigureAwait(false);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Public-health case report queued (consent bypassed — mandated reporting): patient {PatientId} code {Code} → authority {Authority} purpose {Purpose} bundle {BundleId}",
            patientId, reportableCode, _options.AuthorityPartnerId, TefcaPermittedPurposes.PublicHealth, bundle.Id);
        return true;
    }
}
