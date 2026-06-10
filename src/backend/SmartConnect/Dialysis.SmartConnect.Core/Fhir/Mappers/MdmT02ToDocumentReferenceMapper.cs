using System.Globalization;
using System.Text;
using Dialysis.SmartConnect.DataTypes;
using Hl7.Fhir.Model;

namespace Dialysis.SmartConnect.Fhir.Mappers;

/// <summary>
/// Maps an HL7 v2 MDM^T02 (medical document, original notification) to a FHIR R4
/// <c>DocumentReference</c>. TXA-12 carries the document control id; TXA-2 the document type code
/// (LOINC); TXA-6 the activity datetime; OBX-5 the inline document content; PID-3 the patient id.
/// </summary>
public sealed class MdmT02ToDocumentReferenceMapper : IFhirV2MessageMapper<DocumentReference>
{
    public string TriggerEvent => "MDM^T02";

    public DocumentReference Map(Hl7V2Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var doc = new DocumentReference
        {
            Status = DocumentReferenceStatus.Current,
            Type = new CodeableConcept
            {
                Coding =
                [
                    new Coding(
                        system: "http://loinc.org",
                        code: message.GetValue("TXA.2.1") ?? "unknown",
                        display: message.GetValue("TXA.2.2")),
                ],
            },
        };

        var docId = message.GetValue("TXA.12.1");
        if (!string.IsNullOrEmpty(docId))
        {
            doc.Identifier.Add(new Identifier
            {
                System = "urn:ietf:rfc:3986",
                Value = docId,
            });
        }

        var mrn = message.GetValue("PID.3.1");
        if (!string.IsNullOrEmpty(mrn))
        {
            doc.Subject = new ResourceReference($"Patient/{mrn}");
        }

        var date = message.GetValue("TXA.6");
        if (!string.IsNullOrEmpty(date) && date.Length >= 8)
        {
            doc.DateElement = new Instant(new DateTimeOffset(
                int.Parse(date[..4], CultureInfo.InvariantCulture),
                int.Parse(date.Substring(4, 2), CultureInfo.InvariantCulture),
                int.Parse(date.Substring(6, 2), CultureInfo.InvariantCulture),
                0, 0, 0, TimeSpan.Zero));
        }

        var inlineBody = message.GetValue("OBX.5");
        if (!string.IsNullOrEmpty(inlineBody))
        {
            doc.Content.Add(new DocumentReference.ContentComponent
            {
                Attachment = new Attachment
                {
                    ContentType = "text/plain",
                    Data = Encoding.UTF8.GetBytes(inlineBody),
                },
            });
        }

        return doc;
    }
}
