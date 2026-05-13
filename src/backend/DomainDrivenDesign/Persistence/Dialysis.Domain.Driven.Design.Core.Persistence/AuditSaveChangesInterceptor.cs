using Dialysis.DomainDrivenDesign.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dialysis.DomainDrivenDesign.Persistence;

/// <summary>
/// Stamps <see cref="Audit"/> shadow fields on every aggregate flagged for the audit primitive,
/// so handlers no longer need to call <c>RecordCreation</c>/<c>RecordUpdate</c>/<c>RecordSoftDelete</c> manually.
/// Soft-delete is detected by an entity entering the modified state with <see cref="Audit.IsDeleted"/> flipped to true.
/// </summary>
public sealed class AuditSaveChangesInterceptor(
    TimeProvider timeProvider,
    IAuditActorAccessor actorAccessor)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var actorId = actorAccessor.ActorId;

        foreach (var entry in context.ChangeTracker.Entries<Audit>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    Set(entry, nameof(Audit.CreatedAt), now);
                    Set(entry, nameof(Audit.CreatedBy), actorId);
                    break;

                case EntityState.Modified:
                    if (IsBeingSoftDeleted(entry))
                    {
                        Set(entry, nameof(Audit.DeletedAt), now);
                        Set(entry, nameof(Audit.DeletedBy), actorId);
                    }
                    Set(entry, nameof(Audit.UpdatedAt), now);
                    Set(entry, nameof(Audit.UpdatedBy), actorId);
                    break;
            }
        }
    }

    private static bool IsBeingSoftDeleted(EntityEntry<Audit> entry)
    {
        var property = entry.Property(nameof(Audit.IsDeleted));
        return property.IsModified
            && property.CurrentValue is true
            && property.OriginalValue is false;
    }

    private static void Set(EntityEntry entry, string propertyName, object? value)
    {
        var property = entry.Property(propertyName);
        property.CurrentValue = value;
        property.IsModified = true;
    }
}
