using Dialysis.Registry.Adapters;
using Intercessor.Abstractions;

namespace Dialysis.Registry.Features.BatchExport;

public sealed class BatchExportHandler : IQueryHandler<BatchExportQuery, RegistryExportResult>
{
    private readonly IEnumerable<IRegistryAdapter> _adapters;

    public BatchExportHandler(IEnumerable<IRegistryAdapter> adapters)
    {
        _adapters = adapters;
    }

    public async Task<RegistryExportResult> HandleAsync(BatchExportQuery request, CancellationToken cancellationToken = default)
    {
        var adapter = _adapters.FirstOrDefault(a =>
            string.Equals(a.Name, request.Adapter, StringComparison.OrdinalIgnoreCase));
        if (adapter == null)
            return new RegistryExportResult(false, request.Adapter, null, null, $"Unknown adapter: {request.Adapter}");

        var exportRequest = new RegistryExportRequest(request.From, request.To, request.PatientIds, null, request.Format);
        return await adapter.ExportAsync(exportRequest, cancellationToken);
    }
}
