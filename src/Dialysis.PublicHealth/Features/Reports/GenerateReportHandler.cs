using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.Reports;

public sealed class GenerateReportHandler : IQueryHandler<GenerateReportQuery, ReportResult>
{
    private readonly IEnumerable<IReportGenerator> _generators;

    public GenerateReportHandler(IEnumerable<IReportGenerator> generators)
    {
        _generators = generators;
    }

    public async Task<ReportResult> HandleAsync(GenerateReportQuery request, CancellationToken cancellationToken = default)
    {
        var generator = _generators.FirstOrDefault(g =>
            string.Equals(g.Format, request.Format, StringComparison.OrdinalIgnoreCase));
        if (generator == null)
            return new ReportResult(false, request.Format, null, null, $"Unknown format: {request.Format}");

        var reportRequest = new ReportRequest(request.From, request.To, request.ConditionCode, request.PatientIds);
        return await generator.GenerateAsync(reportRequest, cancellationToken);
    }
}
