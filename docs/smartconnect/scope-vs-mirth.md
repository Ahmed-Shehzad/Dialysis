# SmartConnect scope vs. Mirth Connect user guide

The [traceability matrix](guide-traceability.md) maps every PDF outline entry to SmartConnect. This document summarizes **product scope**: what we intentionally do not replicate from the Mirth Connect Administrator / commercial stack.

## In scope

- **Integration runtime:** channel-style flows (source → filters → transforms → destinations), ledger, retries, response transforms, in-process channel chaining (channel writer).
- **Hosting:** ASP.NET Core API, JSON management routes, static operator shell, EF Core persistence (flows, ledger, maps, audit events).
- **Connectors (core):** HTTP inbound, MLLP/TCP listeners, file reader (local filesystem), database reader (polling), outbound HTTP/file/SMTP/TCP/MLLP/database/channel-writer.

## Out of scope or delegated (typically N/A in the matrix)

- **Mirth installers, Server Manager, Java Administrator UI:** deployment and operations use .NET/Kubernetes and REST APIs instead.
- **JVM channel runtime:** no embedded Java connectors; JMS and MC JavaScript reader/writer patterns are not provided as built-ins.
- **Enterprise file transports:** FTP/SFTP/SMB/S3 options on File Reader are not in core; use local paths after a sidecar sync or extend with a dedicated connector later.
- **SOAP Web Service listener/sender, JMS listener/sender:** use inbound HTTP and HTTP outbound (or native broker clients) unless a future connector is added.
- **Visual rule builder, full data-type trees, attachment viewers:** partial APIs and transforms only; parity is API/script-oriented.
- **Mirth commercial / certification bundles** (ONC §170 appendices, EHI export, MFA, etc.): SmartConnect does not claim Mirth certification; compliance is the responsibility of the host product (IdP, audit strategy, accessibility). Selected matrix rows are marked **N/A** with rationale.

## Compliance / certification appendix

Chapters at the end of the Mirth guide (ONC §170, EHI export, MFA, QMS, accessibility) are **not** SmartConnect deliverables. Selected outline rows are marked **N/A** in [traceability-overrides.json](traceability-overrides.json) with rationale (e.g. delegated to host IdP or platform). See matrix rows such as `ad5275427813`, `94c7b0366be0`, `865840ed47b0`, `5ad5b9956070`, `249db375a76d`, `5711b375af90`.

**Commercial / extension connectors** (FHIR, Email Reader, DICOM, LDAP, serial, SSL Manager, JMS template chapters, etc.) are also **N/A** in core SmartConnect unless explicitly added; the matrix records the decision per PDF outline id.

## How to extend scope

When a feature is implemented, update [traceability-overrides.json](traceability-overrides.json) with the PDF outline `id` from [guide-toc.json](../book/guide-toc.json), run `tools/smartconnect/generate_traceability_md.py`, and keep CI PDF verification green.

For bulk **N/A** alignment (Administrator-only prose, certification appendix pages, commercial connector rows), you can run `tools/smartconnect/merge_traceability_batch.py` after editing its rules, then re-run the generator and `validate_traceability.py`.
