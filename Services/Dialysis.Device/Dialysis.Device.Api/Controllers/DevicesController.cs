using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Device.Api.Contracts;
using Dialysis.Device.Application.Features.GetDevice;
using Dialysis.Device.Application.Features.GetDevices;
using Dialysis.Device.Application.Features.RegisterDevice;
using Dialysis.Hl7ToFhir;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Device.Api.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;

    public DevicesController(ISender sender, IAuditRecorder audit, ITenantContext tenant)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
    }

    [HttpGet]
    [Authorize(Policy = "DeviceRead")]
    [ProducesResponseType(typeof(GetDevicesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevicesAsync(CancellationToken cancellationToken)
    {
        GetDevicesResponse response = await _sender.SendAsync(new GetDevicesQuery(), cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Device", null, User.Identity?.Name,
            AuditOutcome.Success, $"List devices ({response.Devices.Count})", _tenant.TenantId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{deviceId}")]
    [Authorize(Policy = "DeviceRead")]
    [ProducesResponseType(typeof(GetDeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceAsync(string deviceId, CancellationToken cancellationToken)
    {
        GetDeviceResponse? response = await _sender.SendAsync(new GetDeviceQuery(deviceId), cancellationToken);
        if (response is null)
            return NotFound();

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Device", deviceId, User.Identity?.Name,
            AuditOutcome.Success, "Get device", _tenant.TenantId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{deviceId}/fhir")]
    [Authorize(Policy = "DeviceRead")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceFhirAsync(string deviceId, CancellationToken cancellationToken)
    {
        GetDeviceResponse? response = await _sender.SendAsync(new GetDeviceQuery(deviceId), cancellationToken);
        if (response is null)
            return NotFound();

        Hl7.Fhir.Model.Device fhirDevice = DeviceMapper.ToFhirDevice(response.DeviceEui64, response.Manufacturer, response.Model);
        fhirDevice.Id = response.Id;
        if (!string.IsNullOrEmpty(response.Serial))
            fhirDevice.SerialNumber = response.Serial;
        if (!string.IsNullOrEmpty(response.Udi))
            fhirDevice.UdiCarrier = [new Hl7.Fhir.Model.Device.UdiCarrierComponent { DeviceIdentifier = response.Udi }];

        string json = FhirJsonHelper.ToJson(fhirDevice);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Device", deviceId, User.Identity?.Name,
            AuditOutcome.Success, "Get device FHIR", _tenant.TenantId), cancellationToken);
        return Content(json, "application/fhir+json");
    }

    [HttpPost]
    [Authorize(Policy = "DeviceWrite")]
    [ProducesResponseType(typeof(RegisterDeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterDeviceAsync(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterDeviceCommand(
            request.DeviceEui64,
            request.Manufacturer,
            request.Model,
            request.Serial,
            request.Udi);
        RegisterDeviceResponse response = await _sender.SendAsync(command, cancellationToken);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create, "Device", response.DeviceId, User.Identity?.Name,
            AuditOutcome.Success, response.Created ? "Register device" : "Update device", _tenant.TenantId), cancellationToken);

        return response.Created ? CreatedAtAction(nameof(GetDeviceAsync), new { deviceId = response.DeviceId }, response) : Ok(response);
    }
}
