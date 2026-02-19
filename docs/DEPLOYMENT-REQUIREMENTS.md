# Dialysis PDMS – Deployment Requirements

Deployment and operational requirements for the Dialysis PDMS. Aligns with the Dialysis Machine HL7 Implementation Guide Rev 4.0 and C5 compliance.

---

## 1. Time Synchronization

Per the HL7 Implementation Guide §2 and IHE Consistent Time (CT) Protocol, time must be synchronized across systems so that treatment data from dialysis machines is reconcilable with EMR data.

### 1.1 PDMS Servers

PDMS APIs and the Gateway must run on hosts with NTP-synchronized system clocks.

| Environment | Action |
|-------------|--------|
| **Linux** | Ensure `systemd-timesyncd` or `ntpd`/`chronyd` is enabled. Verify with `timedatectl status` (NTP synchronized: yes). |
| **Windows** | Configure NTP via `w32tm` or Group Policy. |
| **Docker / Kubernetes** | Containers inherit the host clock. Ensure the host is NTP-synced. Do not run NTP inside containers. |

### 1.2 Dialysis Machines

Dialysis machines sending HL7 messages (ORU^R01, ORU^R40, QBP^Q22, QBP^D01) must use IHE Consistent Time Protocol — NTP (RFC 1305 / RFC 5905) — to synchronize their clocks. This is a device manufacturer and facility IT responsibility.

### 1.3 Facility NTP Infrastructure

- Provide one or more NTP servers (e.g. `pool.ntp.org`, or on-prem NTP) for machines and PDMS hosts.
- Target sync error: &lt; 1 second per IHE CT Profile.

### 1.4 Verification

- PDMS health endpoint includes server UTC time for verification (see [GATEWAY.md](GATEWAY.md)).
- If incoming HL7 message timestamps (MSH-7) drift significantly from server time, PDMS logs a warning (configurable via `TimeSync:MaxAllowedDriftSeconds`).

---

## 2. Database

- **PostgreSQL** 16+ (or 14+)
- Per-service databases: `dialysis_patient`, `dialysis_prescription`, `dialysis_treatment`, `dialysis_alarm`, `dialysis_device`, `dialysis_fhir`
- Connection strings via configuration; secrets from Key Vault or environment (no hardcoded credentials per C5)
- **dialysis_fhir**: Used when `ConnectionStrings:FhirDb` is set. Stores FHIR Subscriptions for rest-hook notifications. When omitted, subscriptions use in-memory store (lost on restart).

---

## 3. Network & Security

- All external API traffic over HTTPS.
- JWT authentication for business endpoints; health and OpenAPI may remain anonymous in development.
- Multi-tenancy via `X-Tenant-Id` header.

---

## 4. Docker Compose (Local Development)

See [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) §16. Ensure the host running `docker compose` has NTP-enabled system time. Containers use the host clock.

---

## 5. Health Checks

- Gateway `/health` aggregates health from downstream services (patient-api, prescription-api, treatment-api, alarm-api, device-api, fhir-api).
- When `ConnectionStrings:FhirDb` is configured, the FHIR service reports healthy when it can reach the database. If FhirDb is not set, the FHIR service uses an in-memory subscription store and does not depend on a database.

---

## 6. Production Deployment (Azure)

### 6.1 Azure Services

| Component | Azure Service | Notes |
|-----------|---------------|-------|
| Database | Azure Database for PostgreSQL (Flexible Server) | Create per-service databases; enable SSL |
| APIs | Azure App Service (Linux, .NET) | One app per API or container apps |
| Gateway | Azure App Service | Single entry; YARP reverse proxy |
| Secrets | Azure Key Vault | Store connection strings, JWT authority |
| Identity | Azure AD | OAuth2 / OpenID Connect for JWT |

### 6.2 Secrets Management (C5)

- **Never** hardcode credentials in code or config files.
- Store connection strings in Key Vault; reference via `@Microsoft.KeyVault(SecretUri=...)` in App Service configuration.
- JWT authority and audience: externalize to configuration; use Key Vault for client secrets if applicable.

### 6.3 Kubernetes (AKS)

- Use Kubernetes Secrets or External Secrets Operator for Key Vault integration.
- Deploy each API as a Deployment; Gateway as ingress or separate service.
- Health checks: use `/health` for liveness and readiness probes.
- Consider Horizontal Pod Autoscaling for high-traffic APIs.

### 6.4 Data Residency

- For C5 and regulatory requirements, ensure PostgreSQL and App Services are deployed in the target region (e.g. EU for GDPR).
- Document data flows and jurisdictions in operational runbooks.

---

## 7. References

- [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md) – Step-by-step deploy, rollback, troubleshooting
- [HEALTH-CHECK.md](HEALTH-CHECK.md) – Health endpoints and monitoring
- [TIME-SYNCHRONIZATION-PLAN.md](TIME-SYNCHRONIZATION-PLAN.md) – Implementation plan for time sync alignment
- [HL7-IMPLEMENTATION-GUIDE-ALIGNMENT-REPORT.md](HL7-IMPLEMENTATION-GUIDE-ALIGNMENT-REPORT.md) – Guide compliance
- Dialysis Machine HL7 Implementation Guide Rev 4.0, §2 Time Synchronization
