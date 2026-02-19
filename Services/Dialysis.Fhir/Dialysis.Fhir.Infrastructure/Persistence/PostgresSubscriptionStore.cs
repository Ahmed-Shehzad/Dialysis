using BuildingBlocks.Tenancy;

using Dialysis.Fhir.Abstractions;
using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Fhir.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL-backed store for FHIR Subscription resources.
/// </summary>
public sealed class PostgresSubscriptionStore : ISubscriptionStore
{
    private readonly FhirDbContext _db;
    private readonly ITenantContext _tenant;

    public PostgresSubscriptionStore(FhirDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public void Add(string id, Subscription subscription)
    {
        string json = FhirJsonHelper.ToJson(subscription);
        var entity = new SubscriptionEntity
        {
            Id = id,
            TenantId = _tenant.TenantId ?? string.Empty,
            Status = subscription.Status?.ToString() ?? "active",
            ChannelType = subscription.Channel?.Type?.ToString(),
            Endpoint = subscription.Channel?.Endpoint,
            Criteria = subscription.Criteria ?? string.Empty,
            ResourceJson = json
        };
        _ = _db.Subscriptions.Add(entity);
        _ = _db.SaveChanges();
    }

    public bool TryGet(string id, out Subscription? subscription)
    {
        SubscriptionEntity? entity = _db.Subscriptions
            .AsNoTracking()
            .FirstOrDefault(e => e.Id == id && (string.IsNullOrEmpty(_tenant.TenantId) || e.TenantId == _tenant.TenantId));
        if (entity is null)
        {
            subscription = null;
            return false;
        }
        subscription = FhirJsonHelper.FromJson<Subscription>(entity.ResourceJson);
        if (subscription is not null)
            subscription.Id = entity.Id;
        return subscription is not null;
    }

    public bool Remove(string id)
    {
        SubscriptionEntity? entity = _db.Subscriptions.FirstOrDefault(e =>
            e.Id == id && (string.IsNullOrEmpty(_tenant.TenantId) || e.TenantId == _tenant.TenantId));
        if (entity is null)
            return false;
        _ = _db.Subscriptions.Remove(entity);
        _ = _db.SaveChanges();
        return true;
    }

    public IReadOnlyList<Subscription> GetActiveRestHookSubscriptions()
    {
        // FHIR R4 channel type is "rest-hook"; Firely enum ToString() yields "RestHook"
        const string restHook1 = "rest-hook";
        const string restHook2 = "RestHook";
        // FHIR R4 status is lowercase "active"; Firely ToString() may yield "Active"
        var entities = _db.Subscriptions
            .AsNoTracking()
            .Where(e => (e.Status == "active" || e.Status == "Active")
                && (e.ChannelType == restHook1 || e.ChannelType == restHook2)
                && e.Endpoint != null && e.Endpoint != ""
                && (string.IsNullOrEmpty(_tenant.TenantId) || e.TenantId == _tenant.TenantId))
            .ToList();

        var result = new List<Subscription>();
        foreach (SubscriptionEntity entity in entities)
        {
            Subscription? sub = FhirJsonHelper.FromJson<Subscription>(entity.ResourceJson);
            if (sub is not null)
            {
                sub.Id = entity.Id;
                result.Add(sub);
            }
        }
        return result;
    }
}
