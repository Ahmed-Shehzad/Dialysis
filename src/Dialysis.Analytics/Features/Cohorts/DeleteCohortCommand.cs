using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed record DeleteCohortCommand(string Id) : ICommand<bool>;
