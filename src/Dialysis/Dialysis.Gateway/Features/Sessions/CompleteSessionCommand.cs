using Dialysis.Domain.Aggregates;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Sessions;

public sealed record CompleteSessionCommand(string SessionId, decimal? UfRemovedKg) : ICommand<CompleteSessionResult>;

public sealed record CompleteSessionResult(Session? Session);
