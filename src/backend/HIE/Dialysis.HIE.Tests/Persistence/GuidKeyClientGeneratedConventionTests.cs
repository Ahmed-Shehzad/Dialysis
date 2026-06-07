using Dialysis.HIE.Persistence;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Persistence;

/// <summary>
/// Guards the model-wide <c>GuidKeyClientGeneratedConvention</c> applied by <c>ModuleDbContextBase</c>.
/// Every <see cref="System.Guid"/> primary key in a module context must be client-generated
/// (<see cref="ValueGenerated.Never"/>) because aggregates assign their own ids via
/// <c>Guid.CreateVersion7()</c>. If a Guid key were left at the EF default
/// (<see cref="ValueGenerated.OnAdd"/>), appending a child to an already-loaded aggregate would be
/// tracked as Modified → <c>UPDATE</c> affecting 0 rows → <c>DbUpdateConcurrencyException</c> → HTTP 500
/// (the original PDMS reading and HIE document-signature failures). HieDbContext is exercised here as a
/// representative module context that stacks domain aggregates, the Transponder outbox/inbox/saga tables,
/// the FHIR audit store, and the durable-command ledger — all of which assign keys in code.
/// </summary>
public sealed class GuidKeyClientGeneratedConventionTests
{
    [Fact]
    public void Every_Guid_Primary_Key_Is_Client_Generated()
    {
        using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();

        var offenders = new List<string>();
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey is null)
            {
                continue;
            }

            foreach (var property in primaryKey.Properties)
            {
                if (property.ClrType == typeof(Guid)
                    && property.ValueGenerated != ValueGenerated.Never)
                {
                    offenders.Add($"{entityType.DisplayName()}.{property.Name} = {property.ValueGenerated}");
                }
            }
        }

        offenders.ShouldBeEmpty(
            "Guid primary keys must be ValueGenerated.Never (set globally by GuidKeyClientGeneratedConvention). " +
            "Offenders: " + string.Join(", ", offenders));
    }
}
