using System.Text;
using Dialysis.BuildingBlocks.Direct;
using Dialysis.HIE.Core.Abstraction.Partners;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Outbound.Partners.Direct;

/// <summary>
/// Delivers a resource to a partner over <b>Direct</b> secure messaging (S/MIME) instead of FHIR
/// REST. A CCD <c>DocumentReference</c> is sent with its CCD attachment verbatim (the common
/// Directed-Exchange payload); any other resource is attached as FHIR JSON. The actual S/MIME
/// packaging + relay is the building block's <see cref="IDirectMessenger"/>.
/// </summary>
public sealed class DirectPartnerEndpoint : IPartnerEndpoint
{
    private static string SerializeFhirJson(Resource resource) => resource.ToJson();

    private readonly IDirectMessenger _messenger;
    private readonly string _fromAddress;
    private readonly string _toAddress;
    private readonly ILogger<DirectPartnerEndpoint> _logger;

    public DirectPartnerEndpoint(
        string partnerId,
        string fromAddress,
        string toAddress,
        IDirectMessenger messenger,
        ILogger<DirectPartnerEndpoint> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        ArgumentNullException.ThrowIfNull(messenger);
        PartnerId = partnerId;
        _fromAddress = fromAddress;
        _toAddress = toAddress;
        _messenger = messenger;
        _logger = logger;
    }

    public string PartnerId { get; }

    public async Task<PartnerDeliveryResult> DeliverAsync(Resource resource, PartnerDeliveryContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var attachment = BuildAttachment(resource);
        var message = new DirectMessage(
            FromAddress: _fromAddress,
            ToAddress: _toAddress,
            Subject: $"Clinical document for patient {context.PatientId}",
            TextBody: $"Directed Exchange ({context.PurposeOfUse}) — {resource.TypeName} for patient {context.PatientId}.",
            Attachment: attachment);

        try
        {
            await _messenger.SendAsync(message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Direct message delivered to partner {PartnerId} ({To}) for patient {PatientId}",
                PartnerId, _toAddress, context.PatientId);
            return new PartnerDeliveryResult(true, 200, null);
        }
        catch (Exception ex)
        {
            return new PartnerDeliveryResult(false, 0, ex.Message);
        }
    }

    private static DirectAttachment BuildAttachment(Resource resource)
    {
        // A CCD DocumentReference carries the C-CDA bytes in its attachment — send those verbatim.
        if (resource is DocumentReference docRef
            && docRef.Content.FirstOrDefault()?.Attachment is { Data: { } ccdBytes } att
            && ccdBytes.Length > 0)
        {
            var contentType = string.IsNullOrWhiteSpace(att.ContentType) ? "application/cda+xml" : att.ContentType;
            return new DirectAttachment("ccd.xml", contentType, ccdBytes);
        }

        var json = SerializeFhirJson(resource);
        var fileName = $"{resource.TypeName}.json";
        return new DirectAttachment(fileName, "application/fhir+json", Encoding.UTF8.GetBytes(json));
    }
}
