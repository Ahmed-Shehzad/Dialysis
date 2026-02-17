using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Outbound.PushToEhr;

public sealed record PushToEhrCommand(string BaseUrl, string PatientId) : ICommand<PushToEhrResult>;

public sealed record PushToEhrResult(bool Success, int? StatusCode, string? ErrorMessage, string PatientId, int ResourceCount);
