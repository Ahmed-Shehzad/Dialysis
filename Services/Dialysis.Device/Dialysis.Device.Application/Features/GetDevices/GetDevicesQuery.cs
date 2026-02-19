using Intercessor.Abstractions;

namespace Dialysis.Device.Application.Features.GetDevices;

/// <summary>
/// FHIR search params: _id, identifier (device Id or EUI-64).
/// </summary>
public sealed record GetDevicesQuery(string? Id = null, string? Identifier = null) : IQuery<GetDevicesResponse>;
