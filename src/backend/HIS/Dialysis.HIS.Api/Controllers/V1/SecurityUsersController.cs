using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Security.Features.AssignRole;
using Dialysis.HIS.Security.Features.RegisterUser;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Security</em> — identity and authorization surface (Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/security")]
public sealed class SecurityUsersController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        await gateway.SendCommandAsync<RegisterUserCommand, Unit>(command, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("users/{userName}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignRole(string userName, [FromBody] AssignRoleBody body, CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<AssignRoleCommand, Unit>(new AssignRoleCommand(userName, body.RoleCode), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    public sealed record AssignRoleBody(string RoleCode);
}
