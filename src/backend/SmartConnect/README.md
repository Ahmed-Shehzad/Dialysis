# Dialysis.SmartConnect

The **SmartConnect** bounded context — the platform's legacy-protocol integration engine. It speaks the languages older equipment and hospital systems still use (HL7 v2 over MLLP/TCP, files, SFTP, database polling, vendor EHR adapters, DICOM) and translates them into the platform's modern vocabulary (FHIR R4 + integration events). It is a stateless message router with bounded retention and owns no patient master record.

The runtime is a Mirth-Connect-style **flow engine** (`FlowRuntimeEngine`): a channel is an `IntegrationFlow`; a message runs through *source connector → filters → transforms → outbound routes*, each stage written to an append-only ledger.

📐 **Full design — domain model (ERD), pipeline, integration events, and sequence diagrams — is in [ARCHITECTURE.md](ARCHITECTURE.md).**

For the system-wide picture see the [root README](../../../README.md); for build/run/test conventions see [CLAUDE.md](../../../CLAUDE.md).

> This file is also packed as the NuGet package readme for the SmartConnect projects (`PackageReadmeFile` in `Directory.Build.props`); keep it present.
