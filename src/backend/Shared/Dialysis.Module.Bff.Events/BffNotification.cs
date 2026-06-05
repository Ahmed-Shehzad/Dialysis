namespace Dialysis.Module.Bff.Events;

/// <summary>
/// A "go look" signal pushed to a context SPA over the <see cref="NotificationsHub"/>. It carries
/// no clinical record — only enough metadata to surface a toast/badge and let the SPA refetch the
/// authoritative data through the BFF's synchronous, permission-checked API. Keeping the payload
/// PHI-light is deliberate: the SignalR group scoping is a routing convenience, not the access
/// control boundary (the synchronous read path enforces that).
/// </summary>
public sealed record BffNotification
{
    /// <summary>Stable kind discriminator the SPA switches on, e.g. <c>lab-result</c>, <c>imaging-finding</c>.</summary>
    public required string Type { get; init; }

    /// <summary>Short headline for the toast/badge, e.g. "New lab result".</summary>
    public required string Title { get; init; }

    /// <summary>Optional one-line, non-clinical context, e.g. "3 observations for order LAB-1234".</summary>
    public string? Summary { get; init; }

    /// <summary>Patient the signal concerns, when patient-scoped (matches the watched group).</summary>
    public string? PatientId { get; init; }

    /// <summary>Optional in-app route the SPA can deep-link to, e.g. <c>/ehr/patients/{id}/labs</c>.</summary>
    public string? Link { get; init; }

    /// <summary>When the underlying event occurred (UTC). Defaults to now when not supplied.</summary>
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
