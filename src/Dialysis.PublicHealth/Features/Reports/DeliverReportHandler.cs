using Dialysis.PublicHealth.Configuration;
using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;
using Microsoft.Extensions.Options;

namespace Dialysis.PublicHealth.Features.Reports;

public sealed class DeliverReportHandler : ICommandHandler<DeliverReportCommand, DeliverReportResult>
{
    private readonly IEnumerable<IReportGenerator> _generators;
    private readonly IReportDeliveryService _delivery;
    private readonly IMeasureReportToHl7V2Converter? _hl7Converter;
    private readonly PublicHealthOptions _options;

    public DeliverReportHandler(
        IEnumerable<IReportGenerator> generators,
        IReportDeliveryService delivery,
        IOptions<PublicHealthOptions> options,
        IMeasureReportToHl7V2Converter? hl7Converter = null)
    {
        _generators = generators;
        _delivery = delivery;
        _options = options.Value;
        _hl7Converter = hl7Converter;
    }

    public async Task<DeliverReportResult> HandleAsync(DeliverReportCommand request, CancellationToken cancellationToken = default)
    {
        var generator = _generators.FirstOrDefault(g =>
            string.Equals(g.Format, request.Format, StringComparison.OrdinalIgnoreCase));
        if (generator == null)
            return new DeliverReportResult(false, false, $"Unknown format: {request.Format}");

        var reportRequest = new ReportRequest(request.From, request.To, request.ConditionCode, request.PatientIds);
        var result = await generator.GenerateAsync(reportRequest, cancellationToken);

        if (!result.Success)
            return new DeliverReportResult(false, false, result.Error);

        if (result.Content == null)
            return new DeliverReportResult(false, false, "Report generation failed");

        result.Content.Position = 0;
        Stream deliverStream = result.Content;
        var contentType = result.Format.Contains("json", StringComparison.OrdinalIgnoreCase)
            ? "application/json"
            : "application/octet-stream";
        var filename = result.Filename;

        if (string.Equals(_options.ReportDeliveryFormat, "hl7v2", StringComparison.OrdinalIgnoreCase) &&
            _hl7Converter != null &&
            result.Format.Contains("measure-report", StringComparison.OrdinalIgnoreCase))
        {
            deliverStream = await _hl7Converter.ConvertAsync(result.Content, cancellationToken);
            contentType = "application/hl7-v2";
            filename = (result.Filename ?? "report").Replace(".json", ".hl7");
        }

        deliverStream.Position = 0;
        var delivery = await _delivery.DeliverAsync(deliverStream, contentType, filename, cancellationToken);

        return new DeliverReportResult(true, delivery.Success, delivery.Error);
    }
}
