using Dialysis.DeviceIngestion.Features.Hl7.Stream;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Hl7;

public sealed record RetryFailedHl7Command(string Id) : ICommand<RetryFailedHl7Result>;

public sealed record RetryFailedHl7Result(Hl7StreamResponse? Response, bool NotFound);
