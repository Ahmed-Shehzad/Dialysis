namespace Dialysis.HIS.Security.Authorization;

public interface ICurrentUser
{
    string? UserId { get; }

    IReadOnlyCollection<string> Permissions { get; }
}
