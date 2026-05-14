using Dialysis.BuildingBlocks.Verifier;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Security.Domain.ValueObjects;

namespace Dialysis.HIS.Security.Features.RegisterLocalUser;

public sealed class RegisterLocalUserCommandValidator : AbstractValidator<RegisterLocalUserCommand>
{
    public RegisterLocalUserCommandValidator()
    {
        RuleFor(static c => c.LoginName, nameof(RegisterLocalUserCommand.LoginName))
            .Must(static (_, v) => TryParse(v))
            .WithMessage("LoginName must be 3-64 chars: lowercase alphanumerics, dot, underscore or hyphen.");

        RuleFor(static c => c.DisplayName, nameof(RegisterLocalUserCommand.DisplayName))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 256)
            .WithMessage("DisplayName must be 1-256 chars.");
    }

    private static bool TryParse(string? value)
    {
        try { _ = new LoginName(value!); return true; }
        catch (DomainException) { return false; }
    }
}
