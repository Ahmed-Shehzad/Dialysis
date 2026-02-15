using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Export;

public sealed record ExportCommand(
    string ResourceType,
    DateOnly From,
    DateOnly To,
    ExportFormat Format,
    Stream Output) : ICommand;
