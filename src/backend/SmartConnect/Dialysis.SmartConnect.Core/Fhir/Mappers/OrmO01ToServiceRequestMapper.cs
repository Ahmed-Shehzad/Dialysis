using System.Globalization;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 ORM^O01 (order message) to a FHIR R4 <c>ServiceRequest</c>.
/// ORC-2 carries the order identifier; OBR-4 the requested service (LOINC); ORC-9 the authored
/// timestamp; ORC-1 the order control code; PID-3 the patient identifier.
/// </summary>
public sealed class OrmO01ToServiceRequestMapper : IFhirV2MessageMapper<ServiceRequest>
{
    public string TriggerEvent => "ORM^O01";

    public ServiceRequest Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var request = new ServiceRequest
        {
            Status = MapStatus(message.GetValue("ORC.1")),
            Intent = RequestIntent.Order,
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding(
                        system: "http://loinc.org",
                        code: message.GetValue("OBR.4.1") ?? "unknown",
                        display: message.GetValue("OBR.4.2")),
                ],
            },
        };

        var orderId = message.GetValue("ORC.2.1");
        if (!string.IsNullOrEmpty(orderId))
        {
            request.Identifier.Add(new Identifier
            {
                System = "urn:ietf:rfc:3986",
                Value = orderId,
            });
        }

        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            request.Subject = new ResourceReference($"Patient/{mrn}");
        }

        var authored = message.GetValue("ORC.9");
        if (!string.IsNullOrEmpty(authored) && authored.Length >= 8)
        {
            request.AuthoredOn = $"{authored[..4]}-{authored.Substring(4, 2)}-{authored.Substring(6, 2)}";
        }

        return request;
    }

    /// <summary>
    /// Maps HL7 v2 ORC-1 order-control codes to the FHIR R4 <see cref="RequestStatus"/> enum.
    /// Defaults to <see cref="RequestStatus.Active"/> when the code is unrecognised; SmartConnect
    /// flows can override via downstream transforms.
    /// </summary>
    private static RequestStatus MapStatus(string? orc1) =>
        (orc1 ?? string.Empty).ToUpper(CultureInfo.InvariantCulture) switch
        {
            "NW" or "RE" => RequestStatus.Active,
            "CA" => RequestStatus.Revoked,
            "DC" => RequestStatus.Revoked,
            "HD" => RequestStatus.OnHold,
            "CM" => RequestStatus.Completed,
            _ => RequestStatus.Active,
        };
}
