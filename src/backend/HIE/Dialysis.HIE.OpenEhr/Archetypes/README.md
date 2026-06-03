# Archetype mapping catalog

Declarative FHIR → openEHR archetype projections. Adding a new archetype is a
**one-file change**: drop a JSON definition into `Definitions/` and the catalog loader
picks it up at startup.

## Wire shape

Each definition file is JSON, deserialised into `ArchetypeMappingDefinition`:

```jsonc
{
  // openEHR CKM archetype id; matched against `IArchetypeProjection<T>.ArchetypeId`.
  "archetypeId": "openEHR-EHR-OBSERVATION.lab_test_result.v1",

  // FHIR R4 resource type the projection accepts. Must be a known
  // Hl7.Fhir.Model.<Type> name (e.g. `Patient`, `Observation`, `Procedure`).
  "fhirResourceType": "Observation",

  // Free-text description — surfaced in catalogs / tooling.
  "description": "Lab test result observation",

  // One entry per output field.
  "fields": [
    { "key": "code.code",    "path": "Code.Coding[0].Code" },
    { "key": "value.magnitude", "path": "Value as Quantity.Value" },
    { "key": "notes",        "path": "Note[?].Text" }
  ]
}
```

The projection produces:

```jsonc
{
  "archetype_node_id": "openEHR-EHR-OBSERVATION.lab_test_result.v1",
  "fields": {
    "code.code": "29463-7",
    "value.magnitude": 75.5,
    "notes": ["uneventful session"]
  }
}
```

Keys whose path evaluates to `null` are skipped — the output stays compact and
auditable.

## Path grammar

The `path` expression in each field uses dotted PascalCase FHIR property names
augmented with three operators (see `ResourcePath.cs` for the implementation).

| Operator | Meaning | Example |
|---|---|---|
| `Name.Sub` | property chain | `Code.Coding[0].Code` |
| `Name[N]` | list element (zero-indexed) | `Identifier[0].Value` |
| `Name[?]` | wildcard — emit a list, flatten across chained `[?]` | `Note[?].Text` |
| `Name as TypeName.Sub` | subtype cast; returns `null` on type mismatch | `Value as Quantity.Value` |

Enums are normalised to their string name (`Status` → `"Final"`); empty wildcard
expansions return `null` so the JSON key is omitted entirely.

The grammar is deliberately a strict subset of FHIRPath — enough to express the
projections a clinical archetype actually needs without depending on a full
FHIRPath engine.

## Shipped catalog

| File | Archetype | Resource |
|---|---|---|
| `Definitions/patient-demographics.json` | `openEHR-DEMOGRAPHIC-PERSON.person.v1` | `Patient` |
| `Definitions/haemodialysis-session.json` | `openEHR-EHR-COMPOSITION.haemodialysis_session.v1` | `Procedure` |
| `Definitions/lab-test-result.json` | `openEHR-EHR-OBSERVATION.lab_test_result.v1` | `Observation` |

## Adding a new archetype

1. Drop the new file under `Definitions/`. The csproj wildcard
   `Definitions/*.json` already embeds every file in the project; no project edit
   needed.
2. Add a constant in `ArchetypeIds.cs` so other code can reference the id.
3. Add a test in `Dialysis.HIE.Tests/OpenEhr/DeclarativeArchetypeProjectionTests.cs`
   asserting a representative FHIR input maps to the expected output keys.

A partner clinic can extend the catalog from a downstream module by either:

- shipping additional JSON files in their own embedded-resource folder
  (the catalog scanner picks up every `Archetypes.Definitions.*.json` resource
  on the assembly that calls `LoadEmbeddedDefinitions`); or
- calling `services.AddArchetypeMappingDefinition(definition)` at composition
  time with a hand-built definition (useful for tests).

## Migration from the hard-coded projections

The earlier hand-rolled `PatientArchetypeProjection` / `ProcedureArchetypeProjection`
/ `ObservationArchetypeProjection` types were deleted as part of this change —
their behaviour is now expressed entirely by the three shipped definitions. The
DI registration in `HealthInformationExchangeExtensions` is one line
(`services.AddArchetypeMappingCatalog()`) instead of three hand-typed
`AddScoped<IArchetypeProjection<T>, …>` entries.
