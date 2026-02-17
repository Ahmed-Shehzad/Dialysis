using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Hl7.Stream;

public sealed record ProcessHl7StreamCommand(string RawMessage) : ICommand<Hl7StreamResponse>;
