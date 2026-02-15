using Dialysis.Registry.Adapters;
using Intercessor.Abstractions;

namespace Dialysis.Registry.Features.BatchExport;

public sealed record BatchExportQuery(
    string Adapter,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<string>? PatientIds = null,
    string? Format = null) : IQuery<RegistryExportResult>;
