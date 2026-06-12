using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.Contracts.Messaging;
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
    private readonly ITransponderOutbox _outbox;
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
        ITransponderOutbox outbox,
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
        _outbox = outbox;
        _clock = clock;
        _logger = logger;
    }
    public async Task HandleAsync(ConsumeContext<DialysisSessionCompletedIntegrationEvent> context)
    {
        var ct = context.CancellationToken;
        var sessionId = context.Message.SessionId;

        // Idempotency on (SessionId, Kind): a re-delivered completion event re-renders only the
        // kinds that have not already succeeded. A previously-failed kind is retried; a fully
        // processed session is a no-op (no re-render, no duplicate charge).
        var existing = await _reports.ListBySessionAsync(sessionId, ct).ConfigureAwait(false);
        bool AlreadyGenerated(ReportKind kind) =>
            existing.Any(r => r.Kind == kind && r.Status == ReportStatus.Generated);

        var needDischarge = !AlreadyGenerated(ReportKind.DischargeLetter);
        var needBilling = !AlreadyGenerated(ReportKind.BillingDocument);
        if (!needDischarge && !needBilling)
        {
            _logger.LogDebug(
                "Reports for session {SessionId} already generated; skipping (idempotent).", sessionId);
            return;
        }

        var ctx = await _contextBuilder.BuildAsync(sessionId, ct).ConfigureAwait(false);
        if (ctx is null)
        {
            _logger.LogWarning("Reporting consumer skipped: no context for completed session {SessionId}.", sessionId);
            return;
        }

        if (needDischarge)
        {
            await TryGenerateAsync(ctx, ReportKind.DischargeLetter, async () =>
            {
                // Resolve the discharge-letter template in the patient's preferred language, falling
                // back to the operator-flagged language-neutral default (ReportTemplateResolver).
                var template = await _templates
                    .FindActiveAsync(ReportKind.DischargeLetter, ctx.PreferredLanguageCode, ct)
                    .ConfigureAwait(false);
                return await _dischargeLetter.GenerateAsync(ctx, template, ct).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }

        if (needBilling)
        {
            await TryGenerateAsync(ctx, ReportKind.BillingDocument, async () =>
            {
                var (pdf, _) = await _billing
                    .GenerateAsync(ctx, evaluationCount: 1, context.Message.AchievedUfVolumeLiters, ct)
                    .ConfigureAwait(false);
                return pdf;
            }, ct).ConfigureAwait(false);

            // Billing trigger — published independently of the human-readable summary PDF so a
            // render failure above never blocks the charge + invoice pipeline. Gated on the
            // billing-document kind so it fires once per session; EHR.Billing is additionally
            // idempotent on (SessionId, CptCode) to absorb any retry-driven re-publish.
            var chargeReady = new DialysisSessionChargeReadyIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: _clock.GetUtcNow().UtcDateTime,
                SchemaVersion: 1,
                SessionId: ctx.SessionId,
                PatientId: ctx.PatientId,
                Modality: ctx.Modality,
                // Use the pause-aware machine usage time computed by the aggregate on completion,
                // so the charge / invoice match the live chairside estimate and the session summary.
                DurationMinutes: context.Message.ActualDurationMinutes,
                CompletedAtUtc: ctx.CompletedAtUtc,
                CptCode: BillingDocumentGenerator.ResolveCptCode(ctx.Modality, evaluationCount: 1),
                AchievedUfVolumeLiters: context.Message.AchievedUfVolumeLiters);
            // Rides the transactional outbox: the billing-document report row and the charge
            // trigger commit together on the SaveChanges below.
            await _outbox.EnqueueAsync(PdmsTransponderOutboxEnvelope.From(chargeReady), ct).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task TryGenerateAsync(
        SessionReportContext ctx,
        ReportKind kind,
        Func<Task<byte[]>> render,
        CancellationToken cancellationToken)
    {
        var report = new SessionReport(Guid.CreateVersion7(), ctx.SessionId, ctx.PatientId, kind);
        var storageRef = string.Empty;
        var hash = string.Empty;
        try
        {
            var bytes = await render().ConfigureAwait(false);
            hash = Convert.ToHexString(SHA256.HashData(bytes));
            storageRef = await _blobs.SaveAsync(report.Id, "application/pdf", bytes, cancellationToken)
                .ConfigureAwait(false);
            report.RecordGenerated(storageRef, hash, _clock.GetUtcNow().UtcDateTime, ctx.PreferredLanguageCode);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reporting generator for {Kind} on session {SessionId} failed.", kind, ctx.SessionId);
            report.RecordFailure(ex.GetType().Name);
        }
        await _reports.AddAsync(report, cancellationToken).ConfigureAwait(false);

    }
}
