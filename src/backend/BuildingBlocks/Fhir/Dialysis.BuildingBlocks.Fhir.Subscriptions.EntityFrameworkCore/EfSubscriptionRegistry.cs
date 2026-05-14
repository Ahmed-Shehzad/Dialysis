using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;

/// <summary>
/// Persists <see cref="FhirSubscriptionRegistration"/> entries on the host module's
/// <typeparamref name="TDbContext"/> under the <c>fhir_subscriptions</c> schema. Implements both
/// <see cref="ISubscriptionRegistry"/> and <see cref="ISubscriptionMatcher"/> so a single durable
/// component replaces the in-memory default.
/// </summary>
public sealed class EfSubscriptionRegistry<TDbContext>(TDbContext db) : ISubscriptionRegistry, ISubscriptionMatcher
    where TDbContext : DbContext
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async ValueTask<FhirSubscriptionRegistration> RegisterAsync(FhirSubscriptionRegistration registration, CancellationToken cancellationToken)
    {
        var existing = await db.Set<SubscriptionRecord>()
            .FirstOrDefaultAsync(r => r.Id == registration.Id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            db.Set<SubscriptionRecord>().Add(ToRecord(registration));
        }
        else
        {
            ApplyTo(existing, registration);
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return registration;
    }

    public async ValueTask<FhirSubscriptionRegistration?> GetAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        var record = await db.Set<SubscriptionRecord>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == subscriptionId, cancellationToken).ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async ValueTask<IReadOnlyList<FhirSubscriptionRegistration>> ListActiveForTopicAsync(string topicUrl, CancellationToken cancellationToken)
    {
        var records = await db.Set<SubscriptionRecord>().AsNoTracking()
            .Where(r => r.TopicUrl == topicUrl && r.Status == SubscriptionStatus.Active)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return records.ConvertAll(ToDomain);
    }

    public async ValueTask DeleteAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        var record = await db.Set<SubscriptionRecord>()
            .FirstOrDefaultAsync(r => r.Id == subscriptionId, cancellationToken).ConfigureAwait(false);
        if (record is null) return;
        db.Set<SubscriptionRecord>().Remove(record);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask UpdateStatusAsync(string subscriptionId, SubscriptionStatus status, int consecutiveFailures, CancellationToken cancellationToken)
    {
        var record = await db.Set<SubscriptionRecord>()
            .FirstOrDefaultAsync(r => r.Id == subscriptionId, cancellationToken).ConfigureAwait(false);
        if (record is null) return;
        record.Status = status;
        record.ConsecutiveFailures = consecutiveFailures;
        if (status == SubscriptionStatus.Active && consecutiveFailures == 0)
        {
            record.LastNotificationAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<FhirSubscriptionRegistration>> MatchAsync(
        string topicUrl,
        IReadOnlyDictionary<string, string> payloadAttributes,
        CancellationToken cancellationToken)
    {
        var candidates = await ListActiveForTopicAsync(topicUrl, cancellationToken).ConfigureAwait(false);
        var matched = new List<FhirSubscriptionRegistration>(candidates.Count);
        foreach (var sub in candidates)
        {
            if (FiltersMatch(sub.FilterParameters, payloadAttributes))
                matched.Add(sub);
        }
        return matched;
    }

    private static bool FiltersMatch(IReadOnlyDictionary<string, string> filters, IReadOnlyDictionary<string, string> attributes)
    {
        foreach (var (key, expected) in filters)
        {
            if (!attributes.TryGetValue(key, out var actual) || !string.Equals(expected, actual, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static SubscriptionRecord ToRecord(FhirSubscriptionRegistration registration) => new()
    {
        Id = registration.Id,
        TopicUrl = registration.TopicUrl,
        ChannelType = registration.ChannelType,
        ChannelEndpoint = registration.ChannelEndpoint,
        ChannelHeader = registration.ChannelHeader,
        FilterParametersJson = JsonSerializer.Serialize(registration.FilterParameters, _json),
        Status = registration.Status,
        ConsecutiveFailures = registration.ConsecutiveFailures,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static void ApplyTo(SubscriptionRecord record, FhirSubscriptionRegistration registration)
    {
        record.TopicUrl = registration.TopicUrl;
        record.ChannelType = registration.ChannelType;
        record.ChannelEndpoint = registration.ChannelEndpoint;
        record.ChannelHeader = registration.ChannelHeader;
        record.FilterParametersJson = JsonSerializer.Serialize(registration.FilterParameters, _json);
        record.Status = registration.Status;
        record.ConsecutiveFailures = registration.ConsecutiveFailures;
    }

    private static FhirSubscriptionRegistration ToDomain(SubscriptionRecord record)
    {
        var filters = string.IsNullOrEmpty(record.FilterParametersJson)
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(record.FilterParametersJson, _json)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        return new FhirSubscriptionRegistration(
            Id: record.Id,
            TopicUrl: record.TopicUrl,
            ChannelType: record.ChannelType,
            ChannelEndpoint: record.ChannelEndpoint,
            ChannelHeader: record.ChannelHeader,
            FilterParameters: filters,
            Status: record.Status,
            ConsecutiveFailures: record.ConsecutiveFailures);
    }
}
