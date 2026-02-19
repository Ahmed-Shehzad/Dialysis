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
- Per-service databases: `dialysis_patient`, `dialysis_prescription`, `dialysis_treatment`, `dialysis_alarm`, `dialysis_device`
- Connection strings via configuration; secrets from Key Vault or environment (no hardcoded credentials per C5)

---

## 3. Network & Security

- All external API traffic over HTTPS.
- JWT authentication for business endpoints; health and OpenAPI may remain anonymous in development.
- Multi-tenancy via `X-Tenant-Id` header.

---

## 4. Docker Compose (Local Development)

See [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) §16. Ensure the host running `docker compose` has NTP-enabled system time. Containers use the host clock.

---

## 5. References

- [TIME-SYNCHRONIZATION-PLAN.md](TIME-SYNCHRONIZATION-PLAN.md) – Implementation plan for time sync alignment
- [HL7-IMPLEMENTATION-GUIDE-ALIGNMENT-REPORT.md](HL7-IMPLEMENTATION-GUIDE-ALIGNMENT-REPORT.md) – Guide compliance
- Dialysis Machine HL7 Implementation Guide Rev 4.0, §2 Time Synchronization
