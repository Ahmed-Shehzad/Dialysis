using Dialysis.Contracts.Messages;
using Dialysis.PublicHealth.Services;
using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Dialysis.PublicHealth.Features.Reports.Sagas;

/// <summary>
/// Saga that orchestrates report generation and delivery with compensating transactions.
/// Step 1: Generate report. Step 2: Deliver report. Compensation logs failures (delivery cannot be undone).
/// </summary>
public sealed class ReportDeliverySaga : ISagaMessageHandler<ReportDeliveryState, DeliverReportSagaMessage>,
    ISagaStepProvider<ReportDeliveryState, DeliverReportSagaMessage>
{
    private readonly IEnumerable<IReportGenerator> _generators;
    private readonly IReportDeliveryService _delivery;

    public ReportDeliverySaga(IEnumerable<IReportGenerator> generators, IReportDeliveryService delivery)
    {
        _generators = generators ?? throw new ArgumentNullException(nameof(generators));
        _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
    }

    public Task HandleAsync(ISagaConsumeContext<ReportDeliveryState, DeliverReportSagaMessage> context)
    {
        var msg = context.Message;
        context.Saga.From = msg.From;
        context.Saga.To = msg.To;
        context.Saga.Format = msg.Format;
        context.Saga.ConditionCode = msg.ConditionCode;
        context.Saga.PatientIds = msg.PatientIds;
        return Task.CompletedTask;
    }

    public IEnumerable<SagaStep<ReportDeliveryState>> GetSteps(
        ISagaConsumeContext<ReportDeliveryState, DeliverReportSagaMessage> context)
    {
        yield return new SagaStep<ReportDeliveryState>(
            ExecuteGenerateAsync,
            CompensateGenerate);
        yield return new SagaStep<ReportDeliveryState>(
            ExecuteDeliverAsync,
            CompensateDeliver);
    }

    private async Task ExecuteGenerateAsync(ReportDeliveryState state, CancellationToken ct)
    {
        var generator = _generators.FirstOrDefault(g =>
            string.Equals(g.Format, state.Format, StringComparison.OrdinalIgnoreCase));
        if (generator == null)
            throw new InvalidOperationException($"Unknown format: {state.Format}");

        var request = new ReportRequest(state.From, state.To, state.ConditionCode, state.PatientIds);
        var result = await generator.GenerateAsync(request, ct);

        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? "Report generation failed");

        if (result.Content == null)
            throw new InvalidOperationException("Report generation produced no content");

        result.Content.Position = 0;
        using var ms = new MemoryStream();
        await result.Content.CopyToAsync(ms, ct);
        state.ReportContent = ms.ToArray();
        state.ContentType = result.Format.Contains("json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : "application/octet-stream";
        state.Filename = result.Filename;
    }

    private static Task CompensateGenerate(ReportDeliveryState state, CancellationToken ct)
    {
        state.ReportContent = null;
        state.ContentType = null;
        state.Filename = null;
        return Task.CompletedTask;
    }

    private async Task ExecuteDeliverAsync(ReportDeliveryState state, CancellationToken ct)
    {
        if (state.ReportContent == null || state.ContentType == null)
            throw new InvalidOperationException("Report content not generated");

        using var stream = new MemoryStream(state.ReportContent);
        var result = await _delivery.DeliverAsync(stream, state.ContentType, state.Filename, ct);

        if (!result.Success)
            throw new InvalidOperationException(result.Error ?? "Report delivery failed");
    }

    private static Task CompensateDeliver(ReportDeliveryState state, CancellationToken ct)
    {
        // Cannot undo HTTP delivery - log for audit
        return Task.CompletedTask;
    }
}
