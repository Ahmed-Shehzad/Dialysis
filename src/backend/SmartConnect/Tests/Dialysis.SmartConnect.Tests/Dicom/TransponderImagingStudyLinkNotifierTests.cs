using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.SmartConnect.Dicom;
using Dialysis.SmartConnect.Dicom.Integration;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Dicom;

/// <summary>
/// Coverage for the DICOM → EHR producer bridge: a STOW'd instance with an accession number
/// publishes ImagingStudyLinkedIntegrationEvent; one without is ignored.
/// </summary>
public sealed class TransponderImagingStudyLinkNotifierTests
{
    private static readonly Guid _patientId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static DicomInstanceMetadata Metadata(string? accession, string? patientId = null) =>
        new(
            StudyInstanceUid: "1.2.840.55",
            SeriesInstanceUid: "1.2.840.55.1",
            SopInstanceUid: "1.2.840.55.1.1",
            SopClassUid: "1.2.840.10008.5.1.4.1.1.6.1",
            PatientId: patientId,
            PatientName: "DOE^JANE",
            Modality: "US",
            ReceivedUtc: DateTimeOffset.UtcNow,
            SizeBytes: 1024,
            BlobId: Guid.NewGuid())
        {
            AccessionNumber = accession,
        };

    [Fact]
    public async Task Publishes_Linked_Event_With_Accession_And_Study_Async()
    {
        var bus = new RecordingBus();
        var notifier = new TransponderImagingStudyLinkNotifier(bus);

        await notifier.NotifyInstanceIngestedAsync(
            Metadata("IMG-ABC123", _patientId.ToString()), CancellationToken.None);

        var ev = Assert.IsType<ImagingStudyLinkedIntegrationEvent>(Assert.Single(bus.Published));
        Assert.Equal("IMG-ABC123", ev.AccessionNumber);
        Assert.Equal("1.2.840.55", ev.StudyInstanceUid);
        Assert.Equal(_patientId, ev.PatientId);
    }

    [Fact]
    public async Task Ignores_Instance_Without_Accession_Async()
    {
        var bus = new RecordingBus();
        var notifier = new TransponderImagingStudyLinkNotifier(bus);

        await notifier.NotifyInstanceIngestedAsync(Metadata(accession: null), CancellationToken.None);

        Assert.Empty(bus.Published);
    }

    private sealed class RecordingBus : ITransponderBus
    {
        public List<object> Published { get; } = [];
        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class
        {
            Published.Add(message);
            return Task.CompletedTask;
        }
        public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where TMessage : class
        {
            Published.Add(message);
            return Task.CompletedTask;
        }
        public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    }
}
