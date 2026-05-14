using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Security.Features.RegisterLocalUser;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/security")]
public sealed class SecurityController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("local-users")]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterLocalUserResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterLocalUser(
        [FromBody] RegisterLocalUserCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<RegisterLocalUserCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/security/local-users/{id}",
            new RegisterLocalUserResponse(id));
    }

    public sealed record RegisterLocalUserResponse(Guid Id);
}
