using Dialysis.Domain.Aggregates;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Sessions;

public sealed record StartSessionCommand(string PatientId, string? AccessSite, string? EncounterId) : ICommand<StartSessionResult>;

public sealed record StartSessionResult(Session Session);
