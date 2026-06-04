using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.Reporting.Domain;
using Dialysis.PDMS.Reporting.Generators;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Reporting.Consumers;

/// <summary>
/// Reacts to <see cref="DialysisSessionCompletedIntegrationEvent"/> by running the three
/// generators in best-effort order: discharge letter → shift roll-up → billing summary.
/// Each generator runs in isolation so a transient failure in one (e.g. an unpublished
/// template) does not stop the others.
///
/// The consumer is idempotent on <c>(SessionId, ReportKind)</c>: re-delivery of the same
/// event finds the existing aggregate row and exits without re-rendering.
/// </summary>
public sealed class OnDialysisSessionCompleted : IConsumer<DialysisSessionCompletedIntegrationEvent>
{
    private readonly ISessionReportContextBuilder _contextBuilder;
    private readonly IReportTemplateRepository _templates;
    private readonly DischargeLetterGenerator _dischargeLetter;
    private readonly BillingDocumentGenerator _billing;
    private readonly IReportBlobStore _blobs;
    private readonly ISessionReportRepository _reports;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransponderBus _bus;
    private readonly TimeProvider _clock;
    private readonly ILogger<OnDialysisSessionCompleted> _logger;
    /// <summary>
    /// Reacts to <see cref="DialysisSessionCompletedIntegrationEvent"/> by running the three
    /// generators in best-effort order: discharge letter → shift roll-up → billing summary.
    /// Each generator runs in isolation so a transient failure in one (e.g. an unpublished
    /// template) does not stop the others.
    ///
    /// The consumer is idempotent on <c>(SessionId, ReportKind)</c>: re-delivery of the same
    /// event finds the existing aggregate row and exits without re-rendering.
    /// </summary>
    public OnDialysisSessionCompleted(ISessionReportContextBuilder contextBuilder,
        IReportTemplateRepository templates,
        DischargeLetterGenerator dischargeLetter,
        BillingDocumentGenerator billing,
        IReportBlobStore blobs,
        ISessionReportRepository reports,
        IUnitOfWork unitOfWork,
        ITransponderBus bus,
        TimeProvider clock,
        ILogger<OnDialysisSessionCompleted> logger)
    {
        _contextBuilder = contextBuilder;
        _templates = templates;
        _dischargeLetter = dischargeLetter;
        _billing = billing;
        _blobs = blobs;
        _reports = reports;
        _unitOfWork = unitOfWork;
        _bus = bus;
        _clock = clock;
        _logger = logger;
    }
    public async Task HandleAsync(ConsumeContext<DialysisSessionCompletedIntegrationEvent> context)
    {
        var ct = context.CancellationToken;
        var sessionId = context.Message.SessionId;
        var ctx = await _contextBuilder.BuildAsync(sessionId, ct).ConfigureAwait(false);
        if (ctx is null)
        {
            _logger.LogWarning("Reporting consumer skipped: no context for completed session {SessionId}.", sessionId);
            return;
        }

        await TryGenerateAsync(ctx, ReportKind.DischargeLetter, async () =>
        {
            // Resolve the discharge-letter template in the patient's preferred language, falling
            // back to the operator-flagged language-neutral default (ReportTemplateResolver).
            var template = await _templates
                .FindActiveAsync(ReportKind.DischargeLetter, ctx.PreferredLanguageCode, ct)
                .ConfigureAwait(false);
            return await _dischargeLetter.GenerateAsync(ctx, template, ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        await TryGenerateAsync(ctx, ReportKind.BillingDocument, async () =>
        {
            var (pdf, _) = await _billing.GenerateAsync(ctx, evaluationCount: 1, ct).ConfigureAwait(false);
            return pdf;
        }, ct).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task TryGenerateAsync(
        SessionReportContext ctx,
        ReportKind kind,
        Func<Task<byte[]>> render,
        CancellationToken cancellationToken)
    {
        var report = new SessionReport(Guid.CreateVersion7(), ctx.SessionId, ctx.PatientId, kind);
        var generated = false;
        var storageRef = string.Empty;
        var hash = string.Empty;
        try
        {
            var bytes = await render().ConfigureAwait(false);
            hash = Convert.ToHexString(SHA256.HashData(bytes));
            storageRef = await _blobs.SaveAsync(report.Id, "application/pdf", bytes, cancellationToken)
                .ConfigureAwait(false);
            report.RecordGenerated(storageRef, hash, _clock.GetUtcNow().UtcDateTime);
            generated = true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reporting generator for {Kind} on session {SessionId} failed.", kind, ctx.SessionId);
            report.RecordFailure(ex.GetType().Name);
        }
        _reports.Add(report);

        if (generated)
        {
            // HIE Documents consumes this and indexes the report as a FHIR DocumentReference
            // pointing at the same shared blob store, so the admin Documents view, ePA upload,
            // and partner exchange resolve through one storage ref.
            await _bus.PublishAsync(
                new ClinicalDocumentProducedIntegrationEvent(
                    EventId: Guid.CreateVersion7(),
                    OccurredOn: _clock.GetUtcNow().UtcDateTime,
                    SchemaVersion: 1,
                    ReportId: report.Id,
                    PatientId: ctx.PatientId,
                    Kind: kind.ToString(),
                    MimeType: "application/pdf",
                    Title: $"{kind} — session {ctx.SessionId:N}",
                    StorageRef: storageRef,
                    ContentHash: hash,
                    LanguageCode: ctx.PreferredLanguageCode),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
