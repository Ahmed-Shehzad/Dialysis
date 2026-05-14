# Integration Event Versioning Policy

This policy applies to every `IIntegrationEvent` record across all modules
(`Dialysis.HIS.Contracts`, `Dialysis.EHR.Contracts`, `Dialysis.PDMS.Contracts`,
`Dialysis.SmartConnect.Contracts`, `Dialysis.Identity.Contracts`).

Per Eric Evans, *Domain-Driven Design* (2003), pp. 263–264:

> **Open Host Service**: define a protocol that gives access to your subsystem as a set of services. … Use this protocol to enable other subsystems to use your services.
>
> **Published Language**: use a well-documented, shared language … as a medium of communication. … Use it to translate as necessary into and out of that language with other models.

To honor those patterns the schema of every event is part of a **versioned, published contract**.

## Rules

1. **`SchemaVersion` is required.** Every event record declares `int SchemaVersion` as a positional
   constructor parameter immediately after `Guid EventId, DateTime OccurredOn`. There is no implicit
   default; producers must supply a value at each publish site so the choice is intentional.

2. **First version is `1`.** A newly created event is published with `SchemaVersion: 1`.

3. **Breaking changes increment `SchemaVersion` AND rename the type.** A breaking change is any of:
   - renaming a payload field
   - changing the C# type of a payload field
   - changing the semantic meaning of a value (e.g. units, enum re-purposing)
   - making a previously-required field optional or vice-versa
   - removing a field

   On a breaking change, create a new record type with suffix `V<n>` (e.g.
   `PatientRegisteredIntegrationEventV2`) AND bump its `SchemaVersion` to `n`. Keep the previous
   record alive only as long as at least one consumer still subscribes to it; remove once everyone
   is on the new version. There is no in-place mutation of an existing event type — that would
   silently corrupt downstream consumers.

4. **Additive changes do NOT bump `SchemaVersion`.** Adding a new nullable field is backwards-compatible
   on the wire (the JSON serializer tolerates extra/missing properties), so it stays at the same
   version. Document the additive change in the type's XML doc-comment instead.

5. **Consumers conform to the published language.** A consumer that needs an internal model
   different from the contract translates at its boundary via an Anticorruption Layer
   (Evans pp. 258–260), not by mutating the event record.

6. **Wire identity = `(AssemblyQualifiedTypeName, SchemaVersion)`.** The Transponder outbox uses the
   assembly-qualified type name for routing; `SchemaVersion` is recorded in the payload for
   diagnostics and consumer compatibility checks.

## Architecture-test enforcement

`tests/Dialysis.ArchitectureTests/IntegrationEventVersioningTests.cs` reflects over every
`IIntegrationEvent` implementation and asserts a non-zero `SchemaVersion`. A new event without a
version, or with `SchemaVersion = 0`, fails the build.
