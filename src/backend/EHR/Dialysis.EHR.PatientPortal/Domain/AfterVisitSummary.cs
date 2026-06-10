using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.PatientPortal.Domain;

public enum AfterVisitSummaryStatus
{
    Draft = 1,
    Published = 2,
}

/// <summary>A self-care instruction line on an <see cref="AfterVisitSummary"/>.</summary>
public sealed class AfterVisitInstruction : Entity<Guid>
{
    private AfterVisitInstruction()
    {
    }

    internal AfterVisitInstruction(Guid id, Guid summaryId, string text) : base(id)
    {
        SummaryId = summaryId;
        Text = text;
    }

    public Guid SummaryId { get; private set; }
    public string Text { get; private set; } = string.Empty;
}

/// <summary>A follow-up action line on an <see cref="AfterVisitSummary"/>.</summary>
public sealed class AfterVisitFollowUp : Entity<Guid>
{
    private AfterVisitFollowUp()
    {
    }

    internal AfterVisitFollowUp(Guid id, Guid summaryId, string text) : base(id)
    {
        SummaryId = summaryId;
        Text = text;
    }

    public Guid SummaryId { get; private set; }
    public string Text { get; private set; } = string.Empty;
}

/// <summary>An education / web-resource link on an <see cref="AfterVisitSummary"/>.</summary>
public sealed class AfterVisitResourceLink : Entity<Guid>
{
    private AfterVisitResourceLink()
    {
    }

    internal AfterVisitResourceLink(Guid id, Guid summaryId, string label, string url) : base(id)
    {
        SummaryId = summaryId;
        Label = label;
        Url = url;
    }

    public Guid SummaryId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
}

/// <summary>
/// A patient-friendly summary of a visit — what happened, self-care instructions, follow-up actions,
/// and education resource links — authored by the clinician (Draft) and published to the patient
/// portal. The portal-facing companion to the clinician's SOAP note; the central "follow-up info after
/// a visit" surface.
/// </summary>
public sealed class AfterVisitSummary : AggregateRoot<Guid>
{
    private readonly List<AfterVisitInstruction> _instructions = new();
    private readonly List<AfterVisitFollowUp> _followUps = new();
    private readonly List<AfterVisitResourceLink> _resourceLinks = new();

    private AfterVisitSummary()
    {
    }

    public AfterVisitSummary(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }
    public Guid EncounterRef { get; private set; }
    public DateTime VisitDateUtc { get; private set; }
    public Guid AuthoringProviderId { get; private set; }
    public string Narrative { get; private set; } = string.Empty;
    public AfterVisitSummaryStatus Status { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public IReadOnlyCollection<AfterVisitInstruction> Instructions => _instructions;
    public IReadOnlyCollection<AfterVisitFollowUp> FollowUps => _followUps;
    public IReadOnlyCollection<AfterVisitResourceLink> ResourceLinks => _resourceLinks;

    public static AfterVisitSummary CreateDraft(
        Guid id,
        Guid patientId,
        Guid encounterRef,
        DateTime visitDateUtc,
        Guid authoringProviderId,
        string narrative)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(narrative);

        return new AfterVisitSummary(id)
        {
            PatientId = patientId,
            EncounterRef = encounterRef,
            VisitDateUtc = visitDateUtc,
            AuthoringProviderId = authoringProviderId,
            Narrative = narrative.Trim(),
            Status = AfterVisitSummaryStatus.Draft,
        };
    }

    public Guid AddInstruction(Guid id, string text)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _instructions.Add(new AfterVisitInstruction(id, Id, text.Trim()));
        return id;
    }

    public Guid AddFollowUp(Guid id, string text)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _followUps.Add(new AfterVisitFollowUp(id, Id, text.Trim()));
        return id;
    }

    public Guid AddResourceLink(Guid id, string label, string url)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        _resourceLinks.Add(new AfterVisitResourceLink(id, Id, label.Trim(), url.Trim()));
        return id;
    }

    public void Publish(DateTime publishedAtUtc)
    {
        if (Status == AfterVisitSummaryStatus.Published)
            return;
        Status = AfterVisitSummaryStatus.Published;
        PublishedAtUtc = publishedAtUtc;
        RaiseIntegrationEvent(new AfterVisitSummaryPublishedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            SummaryId: Id,
            PatientId: PatientId,
            VisitDateUtc: VisitDateUtc));
    }

    private void EnsureDraft()
    {
        if (Status != AfterVisitSummaryStatus.Draft)
            throw new InvalidOperationException("Cannot edit a published after-visit summary.");
    }
}
