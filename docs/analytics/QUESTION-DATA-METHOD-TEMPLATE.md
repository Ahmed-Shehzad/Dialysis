# Analytics Planning Template: Question → Data → Method

Use this template when planning a new analysis for Dialysis PDMS.

---

## 1. Question

**Clinical or operational question (specific):**

_Example: What proportion of dialysis sessions in the last 30 days had at least one systolic BP reading below 100 mmHg?_

| Field | Your Answer |
|-------|-------------|
| Question | |
| Stakeholder / Use case | |
| Decision this informs | |

---

## 2. Data

**Required data elements:**

| Data Element | Source | FHIR/API Reference | Notes |
|--------------|--------|--------------------|-------|
| | | | |
| | | | |

**Common sources:**
- **Patient**: FHIR `Patient` (demographics)
- **Encounter**: FHIR `Encounter` (sessions; filter by `class` = ambulatory)
- **Observation**: FHIR `Observation` (vitals; LOINC 8480-6 systolic, 8867-4 HR, 59408-5 SpO2)
- **Alert**: Dialysis.Alerting API (hypotension alerts)

**Date range / filters:**
- From: ________  
- To: ________  
- Tenant(s): ________  

---

## 3. Method

**Analytic approach:**

| Aspect | Choice |
|--------|--------|
| Type | Descriptive / Cohort / Inferential |
| Unit of analysis | Patient / Encounter / Observation |
| Aggregation | Count / Mean / Median / Rate / Distribution |
| Grouping | By tenant / date / facility / other |

**Output:**
- Chart / Table / Export format: ________
- Audience: ________

---

## 4. Validation

- [ ] Data available in FHIR/Alerting for the date range?
- [ ] Tenant isolation respected?
- [ ] Audit trail required for this analysis?

---

## 5. Implementation Notes

_FHIR search example, API calls, or code references:_

```
# Example: Encounters in date range
GET /fhir/Encounter?date=ge2025-01-01&date=le2025-01-31&_count=100

# Example: Observations for systolic BP
GET /fhir/Observation?code=8480-6,85354-9&subject=Patient/xxx&_sort=date
```

---

**See:** [ANALYTICS-DECISION-SUPPORT.md](../ANALYTICS-DECISION-SUPPORT.md) for full design and roadmap.
