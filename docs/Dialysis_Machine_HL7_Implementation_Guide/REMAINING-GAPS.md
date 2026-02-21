# HL7 Implementation Guide – Remaining Gaps

**Source**: Dialysis Machine HL7 Implementation Guide Rev 4.0 (March 2023)  
**Status**: Core transactions implemented; lower-priority items documented for future work.

---

## 1. Implemented (No Gaps)

| Transaction | Status |
|-------------|--------|
| Patient Demographics (PDQ) | QBP^Q22 / RSP^K22 |
| Prescription Transfer | QBP^D01 / RSP^K22 |
| Treatment Reporting (PCD-01) | ORU^R01 / ACK^R01 |
| Alarm Reporting (PCD-04) | ORU^R40 / ORA^R41 |
| HL7 Batch Protocol | FHS/BHS/BTS/FTS |
| OBX sub-ID (IEEE 11073 containment) | MdcToObxSubIdCatalog |
| Rx Use column (M/C/O) | PrescriptionRxUseCatalog |
| Prescription conflict handling | Reject, Replace, Ignore, Callback, Partial |

---

## 2. Lower Priority / Future Work

| Gap | Priority | Notes |
|-----|----------|-------|
| **Private manufacturer terms** | P5 | Guide §8: 11073 partition 2, term codes 0xF000–0xFFFF. PDMS uses MDC catalog; manufacturer-specific codes would require extensible catalog. |
| **OBX-17 provenance mapping** | P4 | RSET/MSET/ASET parsed; full Provenance resource mapping optional. |
| **Additional alarm types** | P4 | Guide Table 3: arterial/venous air, self-test failure, etc. Implement as needed per facility. |
| **MLLP transport** | P4 | Guide §2: Default MLLP over TCP/IP. PDMS uses REST/JSON for HL7 ingest; MLLP would require separate listener. |
| **HL7 v2.7** | P5 | Guide assumes v2.6; v2.7 compatibility if required. |
| **Peritoneal dialysis** | N/A | Explicitly out of scope per Guide. |

---

## 3. Alignment Reports

- [HL7-IMPLEMENTATION-GUIDE-ALIGNMENT-REPORT.md](../HL7-IMPLEMENTATION-GUIDE-ALIGNMENT-REPORT.md)
- [IMMEDIATE-HIGH-PRIORITY-PLAN.md](../IMMEDIATE-HIGH-PRIORITY-PLAN.md) §4
