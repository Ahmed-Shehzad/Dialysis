using Dialysis.HIS.Security.Audit;
using Dialysis.HIS.Security.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Stores;

public sealed class EfAuditTrail(HisDbContext db, ICurrentUser currentUser) : IAuditTrail
{
    public async Task WriteAsync(string actionCode, string? subjectId, string? details, CancellationToken cancellationToken = default)
    {
        db.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = Guid.CreateVersion7(),
            ActionCode = actionCode,
            SubjectId = subjectId,
            Details = details,
            OccurredAtUtc = DateTime.UtcNow,
            ActorUserId = currentUser.UserId,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
