using Dialysis.CQRS.Commands;

namespace Dialysis.HIS.Security.Features.RegisterUser;

public sealed record RegisterUserCommand(string UserName, string Password, IReadOnlyList<string> RoleCodes)
    : ICommand;
