using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Dialysis.DomainDrivenDesign.Persistence;

/// <summary>
/// Model-finalizing convention that marks every <see cref="System.Guid"/> primary-key property as
/// client-generated (<see cref="ValueGenerated.Never"/>).
/// <para>
/// Aggregates in this codebase assign their own identifiers in the domain (<c>Guid.CreateVersion7()</c>)
/// before persistence. EF Core's default key convention for a <see cref="System.Guid"/> key is
/// <see cref="ValueGenerated.OnAdd"/>, which means a <i>child</i> entity discovered on an
/// <b>already-loaded, tracked</b> aggregate (e.g. a reading added to a loaded session, a signature
/// appended to a loaded document) is seen by <c>DetectChanges</c> as having a <i>set</i> key and is
/// marked <see cref="EntityState.Modified"/>. EF then emits <c>UPDATE ... WHERE Id = ...</c> which
/// affects <b>0</b> rows (the row does not exist yet) → <see cref="DbUpdateConcurrencyException"/>
/// → HTTP 500. Declaring such keys <see cref="ValueGenerated.Never"/> makes EF track the new child
/// as <see cref="EntityState.Added"/> (INSERT). Creating a <i>new root</i> via <c>DbSet.Add</c> never
/// hit this because <c>Add</c> forces <see cref="EntityState.Added"/> regardless.
/// </para>
/// <para>
/// This runs at model finalization with convention precedence, so any property a module configured
/// explicitly (e.g. an <c>int</c> identity column kept as <see cref="ValueGenerated.OnAdd"/>) is left
/// untouched — the builder call cannot override an explicit configuration. The convention only targets
/// <see cref="System.Guid"/> keys; nothing in this codebase relies on the database or EF to generate a
/// <see cref="System.Guid"/> key (no <c>HasDefaultValueSql</c>, <c>gen_random_uuid</c>, or
/// <c>HasValueGenerator</c> on Guid keys), and the shared infrastructure tables stacked on every module
/// context (Transponder outbox/inbox/saga, the FHIR audit store, the durable-command ledger) all assign
/// their keys in code, so no value generation is lost. Because Npgsql maps an OnAdd Guid to a client-side
/// generator rather than a column default, switching to Never produces no schema change and needs no migration.
/// </para>
/// </summary>
public sealed class GuidKeyClientGeneratedConvention : IModelFinalizingConvention
{
    /// <inheritdoc />
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey is null)
            {
                continue;
            }

            foreach (var property in primaryKey.Properties)
            {
                if (property.ClrType == typeof(Guid))
                {
                    // Convention-source call: respects (never overrides) explicit module configuration.
                    property.Builder.ValueGenerated(ValueGenerated.Never);
                }
            }
        }
    }
}
