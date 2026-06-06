using System.Text;
using Dialysis.BuildingBlocks.Direct;
using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Outbound.Partners.Direct;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class DirectPartnerEndpointTests
{
    [Fact]
    public async Task Ccd_Document_Reference_Is_Sent_As_The_Direct_Attachment_Async()
    {
        var messenger = new CapturingMessenger();
        var endpoint = new DirectPartnerEndpoint(
            "partner-x", "us@hisp.example", "them@hisp.partner", messenger,
            NullLogger<DirectPartnerEndpoint>.Instance);

        var ccdBytes = Encoding.UTF8.GetBytes("<ClinicalDocument/>");
        var docRef = new DocumentReference
        {
            Id = "doc-1",
            Status = DocumentReferenceStatus.Current,
            Content = [new DocumentReference.ContentComponent
            {
                Attachment = new Attachment { ContentType = "application/cda+xml", Data = ccdBytes },
            }],
        };

        var patientId = Guid.NewGuid();
        var result = await endpoint.DeliverAsync(docRef, new PartnerDeliveryContext(patientId, "Treatment"), CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        messenger.Sent.ShouldNotBeNull();
        messenger.Sent!.FromAddress.ShouldBe("us@hisp.example");
        messenger.Sent.ToAddress.ShouldBe("them@hisp.partner");
        messenger.Sent.Attachment.ShouldNotBeNull();
        messenger.Sent.Attachment!.FileName.ShouldBe("ccd.xml");
        messenger.Sent.Attachment.ContentType.ShouldBe("application/cda+xml");
        messenger.Sent.Attachment.Payload.ShouldBe(ccdBytes);
    }

    [Fact]
    public async Task Non_Document_Resource_Is_Sent_As_Fhir_Json_Async()
    {
        var messenger = new CapturingMessenger();
        var endpoint = new DirectPartnerEndpoint(
            "partner-x", "us@hisp.example", "them@hisp.partner", messenger,
            NullLogger<DirectPartnerEndpoint>.Instance);

        var observation = new Observation { Id = "obs-1", Status = ObservationStatus.Final };
        await endpoint.DeliverAsync(observation, new PartnerDeliveryContext(Guid.NewGuid(), "Treatment"), CancellationToken.None);

        messenger.Sent!.Attachment!.FileName.ShouldBe("Observation.json");
        messenger.Sent.Attachment.ContentType.ShouldBe("application/fhir+json");
        Encoding.UTF8.GetString(messenger.Sent.Attachment.Payload).ShouldContain("Observation");
    }

    [Fact]
    public async Task Pickup_Relay_Writes_An_Eml_With_The_Envelope_Async()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dialysis-direct-" + Guid.NewGuid().ToString("N"));
        try
        {
            var relay = new PickupDirectoryDirectSmtpRelay(
                Options.Create(new DirectMessagingOptions { PickupDirectory = dir }));
            var envelope = Encoding.UTF8.GetBytes("smime-envelope-bytes");

            await relay.SendAsync("us@hisp.example", "them@hisp.partner", envelope, CancellationToken.None);

            var file = Directory.EnumerateFiles(dir, "*.eml").ShouldHaveSingleItem();
            var contents = await File.ReadAllTextAsync(file);
            contents.ShouldContain("From: us@hisp.example");
            contents.ShouldContain("To: them@hisp.partner");
            contents.ShouldContain("application/pkcs7-mime");
            contents.ShouldContain(Convert.ToBase64String(envelope));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class CapturingMessenger : IDirectMessenger
    {
        public DirectMessage? Sent { get; private set; }

        public ValueTask SendAsync(DirectMessage message, CancellationToken cancellationToken)
        {
            Sent = message;
            return ValueTask.CompletedTask;
        }
    }
}
