using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Security.Features.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(static c => c.UserName, nameof(RegisterUserCommand.UserName)).NotEmpty();
        RuleFor(static c => c.Password, nameof(RegisterUserCommand.Password)).NotEmpty();
        RuleFor(static c => c.RoleCodes, nameof(RegisterUserCommand.RoleCodes)).NotEmpty();
    }
}
