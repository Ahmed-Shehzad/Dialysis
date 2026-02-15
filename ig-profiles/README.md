# IG Profiles Directory

Place FHIR R4 StructureDefinition JSON files here. They will be loaded at startup for profile validation.

## Included

- `dialysis-vital-signs-observation.json` – example profile constraining Observation for vital signs (BP, HR, etc.)

## Adding Profiles

Add JSON files with `resourceType: "StructureDefinition"`. Examples: US Core Observation, custom dialysis profiles. The Gateway loads all `*.json` files under this directory.
