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
public sealed class OnDialysisSessionCompleted(
    ISessionReportContextBuilder contextBuilder,
    IReportTemplateRepository templates,
    DischargeLetterGenerator dischargeLetter,
    BillingDocumentGenerator billing,
    IReportBlobStore blobs,
    ISessionReportRepository reports,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<OnDialysisSessionCompleted> logger)
    : IConsumer<DialysisSessionCompletedIntegrationEvent>
{
    public async Task HandleAsync(ConsumeContext<DialysisSessionCompletedIntegrationEvent> context)
    {
        var ct = context.CancellationToken;
        var sessionId = context.Message.SessionId;
        var ctx = await contextBuilder.BuildAsync(sessionId, ct).ConfigureAwait(false);
        if (ctx is null)
        {
            logger.LogWarning("Reporting consumer skipped: no context for completed session {SessionId}.", sessionId);
            return;
        }

        await TryGenerateAsync(ctx, ReportKind.DischargeLetter, async () =>
        {
            var template = await templates.FindActiveAsync(ReportKind.DischargeLetter, ct).ConfigureAwait(false);
            return await dischargeLetter.GenerateAsync(ctx, template, ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        await TryGenerateAsync(ctx, ReportKind.BillingDocument, async () =>
        {
            var (pdf, _) = await billing.GenerateAsync(ctx, evaluationCount: 1, ct).ConfigureAwait(false);
            return pdf;
        }, ct).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task TryGenerateAsync(
        SessionReportContext ctx,
        ReportKind kind,
        Func<Task<byte[]>> render,
        CancellationToken cancellationToken)
    {
        var report = new SessionReport(Guid.CreateVersion7(), ctx.SessionId, ctx.PatientId, kind);
        try
        {
            var bytes = await render().ConfigureAwait(false);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var storageRef = await blobs.SaveAsync(report.Id, "application/pdf", bytes, cancellationToken)
                .ConfigureAwait(false);
            report.RecordGenerated(storageRef, hash, clock.GetUtcNow().UtcDateTime);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reporting generator for {Kind} on session {SessionId} failed.", kind, ctx.SessionId);
            report.RecordFailure(ex.GetType().Name);
        }
        reports.Add(report);
    }
}
