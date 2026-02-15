using Dialysis.IdentityAdmission.Services;
using Intercessor.Abstractions;

namespace Dialysis.IdentityAdmission.Features.SessionScheduling;

public sealed class CreateSessionHandler : ICommandHandler<CreateSessionCommand, CreateSessionResult>
{
    private readonly IFhirIdentityWriter _writer;

    public CreateSessionHandler(IFhirIdentityWriter writer)
    {
        _writer = writer;
    }

    public async Task<CreateSessionResult> HandleAsync(CreateSessionCommand request, CancellationToken cancellationToken = default)
    {
        var encounterId = await _writer.CreateSessionAsync(request, cancellationToken);
        return new CreateSessionResult { EncounterId = encounterId ?? "" };
    }
}

public sealed record CreateSessionResult
{
    public required string EncounterId { get; init; }
}
