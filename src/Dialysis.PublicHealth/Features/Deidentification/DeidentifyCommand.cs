using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.Deidentification;

public sealed record DeidentifyCommand(
    Stream Input,
    Stream Output,
    DeidentificationOptions Options) : ICommand;
