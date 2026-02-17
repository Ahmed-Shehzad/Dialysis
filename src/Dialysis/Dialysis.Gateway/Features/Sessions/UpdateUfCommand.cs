using Dialysis.Domain.Aggregates;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Sessions;

public sealed record UpdateUfCommand(string SessionId, decimal UfRemovedKg) : ICommand<UpdateUfResult>;

public sealed record UpdateUfResult(Session? Session);
