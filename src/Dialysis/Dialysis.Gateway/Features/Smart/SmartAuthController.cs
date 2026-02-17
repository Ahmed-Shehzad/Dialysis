using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Smart;

/// <summary>
/// SMART on FHIR OAuth2 authorize and token endpoints. C5: AllowAnonymous (unauthenticated token issuance).
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class SmartAuthController : ControllerBase
{
    private readonly ISender _sender;

    public SmartAuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// OAuth2 authorize. Supports authorization code flow. Auto-approves when configured.
    /// </summary>
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string client_id,
        [FromQuery] string response_type,
        [FromQuery] string redirect_uri,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? launch,
        [FromQuery] string? tenant,
        CancellationToken ct)
    {
        var result = await _sender.SendAsync(new SmartAuthorizeQuery(client_id, response_type, redirect_uri, scope, state, launch, tenant), ct);

        if (result.Error is not null)
            return BadRequest(new { error = result.Error, error_description = result.ErrorDescription });

        return Redirect(result.RedirectUrl!);
    }

    /// <summary>
    /// OAuth2 token endpoint. Exchanges authorization code for access token.
    /// </summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request, CancellationToken ct)
    {
        var command = new SmartTokenCommand(
            request.grant_type,
            request.code,
            request.redirect_uri,
            request.client_id,
            request.client_secret,
            request.code_verifier);

        var result = await _sender.SendAsync(command, ct);

        if (!result.Success)
            return StatusCode(result.StatusCode, new { error = result.Error, error_description = result.ErrorDescription });

        return Ok(result.TokenResponse);
    }
}

public record TokenRequest(
    string grant_type,
    string? code,
    string? redirect_uri,
    string? client_id,
    string? client_secret,
    string? code_verifier);
