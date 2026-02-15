# FHIR Query Examples for Analytics

Documented `$search` examples for common descriptive metrics. Use the FHIR Gateway base URL (e.g. `https://gateway-host/fhir`). Include `X-Tenant-Id` when multi-tenant.

---

## LOINC Codes (Vitals)

| LOINC | Description |
|-------|-------------|
| 8480-6 | Systolic blood pressure |
| 85354-9 | Blood pressure panel (includes systolic) |
| 8867-4 | Heart rate |
| 59408-5 | SpO2 |

---

## 1. Session Count (Encounter)

**Metric:** Count of dialysis encounters in a date range.

**FHIR search:**
```
GET /Encounter?date=ge2025-01-01&date=le2025-01-31&class=AMB&_count=1000&_total=accurate
```

- `date=ge...` / `date=le...` – encounter date range
- `class=AMB` – ambulatory (adjust to your `Encounter.class` coding)
- `_count=1000` – page size
- `_total=accurate` – request total in `Bundle.total`

**Alternative (summary count):**
```
GET /Encounter?date=ge2025-01-01&date=le2025-01-31&_summary=count
```

---

## 2. Hypotension Rate – Encounters with Systolic < 100 mmHg

**Metric:** % of sessions with at least one systolic BP < 100.

**Step 1 – Observations with low systolic:**
```
GET /Observation?code=8480-6,85354-9&value-number=lt100&date=ge2025-01-01&date=le2025-01-31&_elements=subject,encounter,effective&_count=500
```

- `code=8480-6,85354-9` – systolic BP LOINC
- `value-number=lt100` – value less than 100
- `date=ge...` / `date=le...` – effective date range

**Step 2 – All encounters in same period:**
```
GET /Encounter?date=ge2025-01-01&date=le2025-01-31&_count=1000
```

**Computation:** Unique `Encounter` IDs from Step 1 ÷ unique `Encounter` IDs from Step 2.

**Note:** FHIR `value-number` search may vary by server. If unsupported, fetch Observations and filter in application code.

---

## 3. Vitals by Encounter (Trends)

**Metric:** Min/max/avg systolic per session.

**Get observations for a patient/encounter:**
```
GET /Observation?encounter=Encounter/{encounterId}&code=8480-6,85354-9&_sort=date&_count=100
```

- `encounter=Encounter/{id}` – filter by encounter
- `_sort=date` – chronological order

**Get all encounters for a patient:**
```
GET /Encounter?subject=Patient/{patientId}&date=ge2025-01-01&date=le2025-01-31&_sort=date
```

---

## 4. Patients with Observations in Date Range

**Metric:** Patient list for cohort building.

```
GET /Observation?code=8480-6&date=ge2025-01-01&date=le2025-01-31&_elements=subject&_count=500
```

Use distinct `Observation.subject` (Patient references).

---

## 5. Pagination

FHIR uses `next` link for paging:

```
GET /Encounter?date=ge2025-01-01&_count=100
# Response: Bundle.entry[] + Bundle.link where relation=next
GET {Bundle.link[relation=next].url}
```

---

## 6. Minimal Elements (Performance)

Request only needed elements to reduce payload:

```
GET /Encounter?date=ge2025-01-01&_elements=id,status,subject,period,class&_count=200
GET /Observation?code=8480-6&_elements=id,subject,encounter,valueQuantity,effective&_count=200
```

---

## 7. Alerting API (Non-FHIR)

Alert metrics come from Dialysis.Alerting, not FHIR:

| Metric | Endpoint |
|--------|----------|
| Alert count | `GET /api/v1/alerts` (count `result.Length`) |
| Time to acknowledgement | `GET /api/v1/alerts` → compute `AcknowledgedAt - RaisedAt` for acknowledged alerts |

**Example:**
```
GET /api/v1/alerts
# Returns: [{ "id", "patientId", "encounterId", "raisedAt", "acknowledgedAt", ... }]
```

---

## cURL Examples

```bash
# Session count (replace BASE and TENANT)
curl -H "Authorization: Bearer $TOKEN" -H "X-Tenant-Id: default" \
  "$FHIR_BASE/Encounter?date=ge2025-01-01&date=le2025-01-31&_summary=count"

# Observations with low systolic
curl -H "Authorization: Bearer $TOKEN" -H "X-Tenant-Id: default" \
  "$FHIR_BASE/Observation?code=8480-6&value-number=lt100&date=ge2025-01-01&_count=100"
```

---

**See:** [ANALYTICS-DECISION-SUPPORT.md](../ANALYTICS-DECISION-SUPPORT.md) for full design.
