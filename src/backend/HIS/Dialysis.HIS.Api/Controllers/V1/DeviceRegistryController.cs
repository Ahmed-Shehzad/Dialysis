using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Integration.DeviceRegistry;
using Dialysis.HIS.Integration.Features.DeviceRegistry;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>
/// RA: <em>Patient monitoring</em> — the RPM device registry (Tummers et al., 2021). Register
/// devices, list/read them, and inspect the configured device-type catalog. Device readings come
/// in through <see cref="DeviceIntegrationController"/>; this controller owns device identity.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration/devices")]
public sealed class DeviceRegistryController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    /// <summary>RA: <em>Patient monitoring</em> — RPM device registry.</summary>
    public DeviceRegistryController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>Registers a device against a validated device type with a unique external id.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterDeviceResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterDeviceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var id = await _gateway
                .SendCommandAsync<RegisterDeviceCommand, Guid>(command, cancellationToken)
                .ConfigureAwait(false);
            return CreatedResource($"{Request.Path}/{id}", new RegisterDeviceResponse(id), LinkCapabilitiesIndex());
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Binds a registered device to a patient (and optional session) so its readings are attributed.</summary>
    [HttpPost("{id:guid}/bind")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BindAsync(
        Guid id, [FromBody] BindDeviceRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            await _gateway
                .SendCommandAsync<BindDeviceToPatientCommand, Unit>(
                    new BindDeviceToPatientCommand(id, body.PatientId, body.SessionId), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Applies a lifecycle transition (suspend / activate / retire) to a registered device.</summary>
    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeStatusAsync(
        Guid id, [FromBody] ChangeDeviceStatusRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            await _gateway
                .SendCommandAsync<ChangeDeviceStatusCommand, Unit>(
                    new ChangeDeviceStatusCommand(id, body.Action), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Lists registered devices, most-recently-registered first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<DeviceSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var devices = await _gateway
            .SendQueryAsync<ListDevicesQuery, IReadOnlyList<DeviceSummaryDto>>(new ListDevicesQuery(take), cancellationToken)
            .ConfigureAwait(false);
        return OkResource(devices, LinkCapabilitiesIndex());
    }

    /// <summary>Reads one registered device by registry id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResourceEnvelope<DeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await _gateway
            .SendQueryAsync<GetDeviceByIdQuery, DeviceDto?>(new GetDeviceByIdQuery(id), cancellationToken)
            .ConfigureAwait(false);
        return device is null ? NotFound() : OkResource(device, LinkCapabilitiesIndex());
    }

    /// <summary>Lists the configured device-type catalog (so clients can populate a registration form).</summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyCollection<DeviceType>>), StatusCodes.Status200OK)]
    public IActionResult ListTypes([FromServices] IDeviceTypeCatalog catalog) =>
        OkResource(catalog.All, LinkCapabilitiesIndex());

    /// <summary>201 response body for a registered device.</summary>
    public sealed record RegisterDeviceResponse(Guid Id);

    /// <summary>Request body to bind a device to a patient and optional session.</summary>
    public sealed record BindDeviceRequest(Guid PatientId, Guid? SessionId);

    /// <summary>Request body to change a device's lifecycle status.</summary>
    public sealed record ChangeDeviceStatusRequest(DeviceStatusAction Action);
}
