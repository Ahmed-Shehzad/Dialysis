# eHealth Platform Certification Checklist

This document prepares the codebase for eHealth platform integration. Actual certification requires external processes, credentials, and conformance testing with national authorities.

---

## Overview

| Jurisdiction | Platform | Patient ID | Certification Body |
|--------------|----------|------------|---------------------|
| **DE (Germany)** | gematik ePA | KVNR (Krankenversichertennummer) | gematik |
| **FR (France)** | DMP (Dossier Médical Partagé) | INS (Identifiant National de Santé) | ANS |
| **UK** | NHS/Spine | NHS Number | NHS Digital |

---

## #1 Germany (gematik ePA)

### Prerequisites

- [ ] gematik Konnektor (hardware/software) or certified TI (Telematikinfrastruktur) access
- [ ] FdV (Fachdienst Verzeichnis) endpoint credentials
- [ ] CDA CH:EMED document format compliance
- [ ] HBA (Health Professional Card) or SMC-B (Institution Card) for authentication

### Configuration Placeholders

```json
{
  "EHealth": {
    "Platform": "epa",
    "Jurisdiction": "DE",
    "De": {
      "KonnektorUrl": "https://konnektor.example.de",
      "FdVBaseUrl": "https://fdv.example.de",
      "MandantId": "<from-certification>",
      "ClientSystemId": "<from-certification>",
      "WorkplaceId": "<from-certification>"
    }
  }
}
```

### Secrets (Key Vault / K8s Secret)

- `EHealth__De__ClientCertificate` – TLS client cert for Konnektor
- `EHealth__De__ClientCertificatePassword` – Cert passphrase

### Adapter Implementation Notes

When implementing a certified ePA adapter:

1. Implement `IEHealthPlatformAdapter` with `PlatformId = "epa"`
2. Use Konnektor SOAP/HTTP APIs for document upload (VSDM, OPS)
3. Map Patient.identifier (KVNR) from FHIR to ePA context
4. Support CDA CH:EMED or PDF as document format per gematik specs
5. Register via `builder.Services.AddSingleton<IEHealthPlatformAdapter, GematikEpaAdapter>()` when configured

### References

- [gematik Fachportal](https://fachportal.gematik.de/)
- [TI-Anwendungsfall ePA](https://fachportal.gematik.de/anwendungsfaelle/elektronische-patientenakte)

---

## #2 France (DMP)

### Prerequisites

- [ ] DMP API access (INS – Identifiant National de Santé)
- [ ] Pro Sante Connect or equivalent national API credentials
- [ ] Document format: CDA, PDF per ANS specs

### Configuration Placeholders

```json
{
  "EHealth": {
    "Platform": "dmp",
    "Jurisdiction": "FR",
    "Fr": {
      "DmpApiBaseUrl": "https://api.dmp.fr",
      "ProSanteConnectIssuer": "https://auth.psc.fr",
      "ClientId": "<from-certification>"
    }
  }
}
```

### Secrets

- `EHealth__Fr__ClientSecret` – OAuth client secret
- `EHealth__Fr__ApiKey` – DMP API key (if required)

### Adapter Implementation Notes

1. Implement `IEHealthPlatformAdapter` with `PlatformId = "dmp"`
2. Resolve INS from Patient.identifier before upload
3. Use OAuth2/OIDC for API authentication
4. Map document types to DMP document codes

### References

- [ANS DMP](https://www.esante.gouv.fr/)
- [Pro Sante Connect](https://www.pro.santeconnect.fr/)

---

## #3 UK (NHS/Spine)

### Prerequisites

- [ ] NHS Spine integration (Care Identity Service)
- [ ] NHS Number as patient identifier
- [ ] NHS Digital API credentials

### Configuration Placeholders

```json
{
  "EHealth": {
    "Platform": "spine",
    "Jurisdiction": "UK",
    "Uk": {
      "SpineBaseUrl": "https://spine.nhs.uk",
      "CareIdentityServiceUrl": "https://auth.spine.nhs.uk",
      "ClientId": "<from-certification>"
    }
  }
}
```

### Secrets

- `EHealth__Uk__ClientSecret`
- `EHealth__Uk__ClientCertificate` (if mTLS required)

---

## Production Deployment Readiness

### Swap Stub for Certified Adapter

When a certified adapter is available:

1. Create adapter class implementing `IEHealthPlatformAdapter`
2. Register in `Program.cs`:

   ```csharp
   if (!string.IsNullOrWhiteSpace(options.De?.KonnektorUrl)) // or jurisdiction-specific check
       builder.Services.AddSingleton<IEHealthPlatformAdapter, GematikEpaAdapter>();
   else
       builder.Services.AddSingleton<IEHealthPlatformAdapter, StubEHealthAdapter>();
   ```

3. Configure jurisdiction-specific options and secrets
4. Run conformance tests against platform test environment

### Kubernetes Secret

```yaml
# deploy/kubernetes/secret-ehealth-example.yaml (template – do not commit real values)
apiVersion: v1
kind: Secret
metadata:
  name: dialysis-secrets
  namespace: dialysis-pdms
type: Opaque
stringData:
  EHealth__De__ClientCertificate: "<base64-or-path>"
  EHealth__De__ClientCertificatePassword: "<secret>"
  # Add per-jurisdiction secrets as needed
```

---

## Summary

| Item | Status |
|------|--------|
| Stub adapter (development) | ✅ Implemented |
| Configuration structure | ✅ Placeholders in this doc |
| Jurisdiction options (DE/FR/UK) | See EHealthOptions |
| Certified adapter integration | ⏳ Requires certification |
| Production credentials | ⏳ External process |
