---
name: Solution and Entity Naming
overview: "Ensure all 40 projects are in the solution (verified: all present) and rename all persistence entity classes that use \"Entity\" as a suffix/prefix to comply with the naming convention."
todos: []
isProject: false
---

# Solution and Entity Naming Plan

## 1. Solution Projects (Verification)

**Status: All projects are already included.** The solution has 40 projects. Cross-check confirms every `.csproj` from the workspace is listed in [Dialysis.slnx](Dialysis.slnx):

- Core: BuildingBlocks, Verifier, Intercessor
- Transponder: 33 projects (main lib, transports, persistence, tests)
- Services: 7 projects (Patient, Prescription, Treatment)

No changes needed for the solution file.

---

## 2. Entity Class Renames (Remove "Entity" Suffix)

Persistence entity classes must not use "Entity" as suffix or prefix. The following need renaming:


| Current Name             | New Name           | Location                                                                                                                                 | Implements        |
| ------------------------ | ------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- | ----------------- |
| `PatientEntity`          | `PatientRecord`    | [Services/Dialysis.Patient/.../PatientEntity.cs](Services/Dialysis.Patient/Dialysis.Patient.Infrastructure/Persistence/PatientEntity.cs) | -                 |
| `OutboxMessageEntity`    | `OutboxMessage`    | Transponder.Persistence.EntityFramework                                                                                                  | IOutboxMessage    |
| `InboxStateEntity`       | `InboxState`       | Transponder.Persistence.EntityFramework                                                                                                  | IInboxState       |
| `ScheduledMessageEntity` | `ScheduledMessage` | Transponder.Persistence.EntityFramework                                                                                                  | IScheduledMessage |
| `SagaStateEntity`        | `SagaState`        | Transponder.Persistence.EntityFramework                                                                                                  | -                 |


**Note:** `BaseEntity` in [BuildingBlocks/BaseEntity.cs](BuildingBlocks/BaseEntity.cs) is a domain base class (DDD terminology), not a persistence mapping class. If you want it renamed (e.g., to `BaseRecord`), that can be added; it is not in scope by default for this plan.

---

## 3. Implementation Order

### 3.1 Dialysis.Patient Service (single service)

1. Rename `PatientEntity.cs` to `PatientRecord.cs` and change class name to `PatientRecord`.
2. Update references in [PatientDbContext.cs](Services/Dialysis.Patient/Dialysis.Patient.Infrastructure/Persistence/PatientDbContext.cs): `DbSet<PatientRecord>`, `modelBuilder.Entity<PatientRecord>`.
3. Update references in [PatientRepository.cs](Services/Dialysis.Patient/Dialysis.Patient.Infrastructure/Persistence/PatientRepository.cs): variable types and `MapToPatient(PatientRecord e)`.

### 3.2 Transponder.Persistence.EntityFramework (shared library)

1. Rename entity files and classes:
  - `OutboxMessageEntity.cs` -> `OutboxMessage.cs` (class: `OutboxMessage`)
  - `InboxStateEntity.cs` -> `InboxState.cs` (class: `InboxState`)
  - `ScheduledMessageEntity.cs` -> `ScheduledMessage.cs` (class: `ScheduledMessage`)
  - `SagaStateEntity.cs` -> `SagaState.cs` (class: `SagaState`)
2. Update all references in:
  - [TransponderDbContext.cs](Transponder.Persistence.EntityFramework/TransponderDbContext.cs)
  - [EntityFrameworkInboxStore.cs](Transponder.Persistence.EntityFramework/EntityFrameworkInboxStore.cs)
  - [EntityFrameworkOutboxStore.cs](Transponder.Persistence.EntityFramework/EntityFrameworkOutboxStore.cs)
  - [EntityFrameworkSagaRepository.cs](Transponder.Persistence.EntityFramework/EntityFrameworkSagaRepository.cs)
  - [EntityFrameworkScheduledMessageStore.cs](Transponder.Persistence.EntityFramework/EntityFrameworkScheduledMessageStore.cs)
3. Update EF migration string references in:
  - [PostgreSqlTransponderDbContext.cs](Transponder.Persistence.EntityFramework.PostgreSql/PostgreSqlTransponderDbContext.cs)
  - [SqlServerTransponderDbContext.cs](Transponder.Persistence.EntityFramework.SqlServer/SqlServerTransponderDbContext.cs)
  - Migration Designer files (`*_Designer.cs`) and `PostgreSqlTransponderDbContextModelSnapshot.cs` (string literals like `"Transponder.Persistence.EntityFramework.OutboxMessageEntity"` -> `"Transponder.Persistence.EntityFramework.OutboxMessage"`)
4. Update test files:
  - [OutboxMessageEntityTests.cs](Transponder.Persistence.EntityFramework.Tests/OutboxMessageEntityTests.cs) -> rename to `OutboxMessageTests.cs`, update class and references
  - [ScheduledMessageEntityTests.cs](Transponder.Persistence.EntityFramework.Tests/ScheduledMessageEntityTests.cs) -> rename to `ScheduledMessageTests.cs`, update class and references

---

## 4. Migration and Snapshot Strings

EF Core migrations/snapshots reference entity types via fully qualified names. After renaming, update string literals in:

- `Transponder.Persistence.EntityFramework.PostgreSql/Migrations/PostgreSqlTransponderDbContextModelSnapshot.cs`
- `Transponder.Persistence.EntityFramework.PostgreSql/Migrations/*_Designer.cs`
- `Transponder.Persistence.EntityFramework.SqlServer/Migrations/*_Designer.cs` (if present)

Replace `InboxStateEntity` -> `InboxState`, `OutboxMessageEntity` -> `OutboxMessage`, `SagaStateEntity` -> `SagaState`, `ScheduledMessageEntity` -> `ScheduledMessage` in those strings.

---

## 5. Summary

- **Solution:** No changes; all 40 projects are already included.
- **Entity renames:** 5 classes across 2 areas (Dialysis.Patient and Transponder.Persistence.EntityFramework), plus migration/snapshot string updates and test class renames.

