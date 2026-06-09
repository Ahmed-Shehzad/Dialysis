<!--
  Dialysis platform - MVP demo scenario.
  Generated demo collateral (CMIO / nephrology / product / ops / UX personas).
  Pairs with the recorded e2e-demo video walkthrough (e2e-demo/, video in e2e-artifacts/mvp-demo/).
  Grounded in the real module/route inventory; roadmap items are flagged in-text.
-->

# When the Chair Sat Empty: How One Missed Dialysis Session Almost Sent Marcus to the Hospital

# Executive Summary

On a Wednesday, a dialysis chair sat empty. Marcus Bell, a 58-year-old man with end-stage kidney disease, missed the treatment his body cannot live without — not because he chose to, but because his wife's work shift collided with the only ride he had. In dialysis, a missed session is not a rescheduling inconvenience. It is the opening move in a cascade that, left undetected, ends in an emergency department, a hospital bed, and sometimes far worse. By the time Marcus returned that Friday after a five-day gap, he was carrying 3.8 kilograms of excess fluid, a serum potassium of 6.6 mmol/L that can stop a heart, and a failing heart muscle already running at 40% of normal. This is the story of how a coordinated, modular health platform turned that near-catastrophe into a managed outpatient afternoon — and sent Marcus home, stabilized, that same evening.

This scenario matters because the missed dialysis session is one of the most common and most lethal continuity-of-care failures in nephrology. Clinically, it is the crucible where fluid overload, hyperkalemia, and intradialytic hypotension converge — and where a patient with heart failure with reduced ejection fraction (HFrEF) has the least margin for error. The clinical drama here is real and unforgiving: pull fluid too fast on an overloaded HFrEF patient and his blood pressure collapses to 78/44; pull too cautiously and the potassium and volume that are poisoning him stay in his body. Rosa Martinez, RN, and her team had to thread that needle in real time, at the chair, with a patient cramping and near-syncope. The platform's job was not to make the clinical decision — it was to make sure the right people had the right data at the right second, and that nothing fell through the cracks between the eight systems a single dialysis patient touches.

Operationally and commercially, the scenario shows what a modular monolith built as one coordinating care team can do. Across the afternoon, seven browser applications behind a single Gateway — HIS, EHR, PDMS chairside, SmartConnect, HIE, the Admin/Identity console, and the Patient Portal, plus the headless Lab module — coordinate *only* through integration events over a durable Transponder outbox. The empty chair on the PDMS chair board becomes a missed-treatment signal; that signal lights up the EHR care-coordination worklist, pushes a portal alert to Marcus, and re-slots Tara Nguyen's Friday schedule with a transport plan attached. At the chair, a TimescaleDB-backed vitals stream and PDMS treatment alarms drive an on-call page to Dr. Priya Anand. An outside ED record arrives over the HIE behind a TEFCA-gated trust boundary; a metabolic panel flows in as an HL7 ORU through SmartConnect; the outpatient hemodialysis claim moves from a HIS billing-export job into an EHR EDI 837 against Medicare. The business value is the avoided admission — a single inpatient dialysis-related hospitalization routinely costs more than a month of outpatient care — multiplied across every at-risk patient a facility serves, with revenue-cycle, compliance, and quality-reporting machinery running in the same coordinated motion. This is continuity of care, made operational.

# Main Characters

### Patient — Marcus Bell

- **Name:** Marcus Bell
- **Age:** 58
- **Role:** Patient — ESKD (CKD stage 5) on in-center hemodialysis, three times weekly (Mon/Wed/Fri, 4-hour sessions)
- **Background:** A 58-year-old man whose kidneys failed as a complication of long-standing type 2 diabetes (diabetic nephropathy). He dialyzes through a left upper-arm AV fistula and has held a dry weight of 78.0 kg. His care is complicated by a heart already weakened to an ejection fraction of 40%, which leaves him little tolerance for either fluid overload or aggressive fluid removal. He depends on his wife Denise for transport to the center.
- **Goals:** Stay out of the hospital, keep dialyzing on schedule, manage his diabetes and heart failure, and preserve as much independence and normal life as a thrice-weekly treatment allows.
- **Concerns:** Getting reliable rides to every session; the breathlessness and swelling that creep in when he misses one; the fear, after this Friday, of feeling his blood pressure bottom out in the chair again; and the cost burden even with Medicare and Medicaid coverage.

### Caregiver — Denise Bell

- **Name:** Denise Bell
- **Age:** 54
- **Role:** Caregiver — spouse and primary transport for Marcus
- **Background:** Marcus's wife of many years and his most reliable lifeline to the dialysis center. She works shift hours, and it was a Wednesday shift conflict with no backup ride that caused Marcus to miss his session — the single event that set this whole arc in motion. Today she helps Marcus navigate his care through his own Patient Portal account; a dedicated delegated-access caregiver portal is on the platform roadmap.
- **Goals:** Keep Marcus safe and on schedule without having to choose between her paycheck and his treatment; have a transport plan she can actually depend on.
- **Concerns:** The guilt of the missed Wednesday; the impossible scheduling math between work and three dialysis trips a week; not always knowing when Marcus's numbers are heading the wrong way until he is already symptomatic.

### Nephrologist — Dr. Priya Anand, MD

- **Name:** Dr. Priya Anand, MD
- **Age:** 49
- **Role:** Nephrologist — attending physician, on call the Friday of this scenario
- **Background:** Marcus's attending nephrologist, responsible for his dialysis prescription and overall renal care. She runs the brief telehealth-style check-in after the missed session (today via Patient Portal secure messaging and a scheduled check-in), is paged through ClinicianNotification during the intradialytic emergency, reconciles the outside ED record in the EHR, and signs both the clinical note and the After-Visit Summary.
- **Goals:** Keep Marcus dialyzing safely as an outpatient and out of the ED; correct his hyperkalemia and fluid overload without crashing his pressure; close the loop on the missed session with a durable adherence plan.
- **Concerns:** The narrow safety window an EF-40% heart leaves for ultrafiltration; whether the outside records and home medication list are accurate and reconciled; making sure the chairside team can reach her instantly when an alarm fires.

### Dialysis Nurse — Rosa Martinez, RN

- **Name:** Rosa Martinez, RN
- **Age:** 41
- **Role:** Dialysis Nurse — charge nurse, chairside
- **Background:** The charge nurse who owns the chairside response when Marcus's blood pressure nadirs to 78/44. Her interventions are the clinical heart of the save: reducing the ultrafiltration rate (UF paused, blood-flow rate brought down to 250 mL/min), placing Marcus in Trendelenburg, giving a 200 mL saline bolus, lowering the dialysate temperature, and adjusting the potassium bath in response to his falling serum potassium and cramping. She charts every step in the medication administration record.
- **Goals:** Stabilize Marcus fast, get him to dry weight as safely as the session allows, and document the response cleanly and in real time.
- **Concerns:** Catching the hypotension at its first sign rather than after near-syncope; balancing the need to remove 3.8 L of fluid against a heart that cannot tolerate aggressive pulls; reaching the on-call nephrologist without delay.

### Technician — Kevin Osei

- **Name:** Kevin Osei
- **Age:** 34
- **Role:** Dialysis Technician (Patient Care Technician)
- **Background:** The PCT who cannulates Marcus's left-arm AV fistula, sets up and primes the machine, and watches the live telemetry streaming into the PDMS chairside session. When the alarm fires, he assists Rosa's emergency response and captures the rapid-cadence vitals that document Marcus's decline and recovery.
- **Goals:** A clean cannulation and a well-prepared machine; eyes on the vitals trend so the team sees trouble coming; smooth hands-on support during the emergency.
- **Concerns:** Reliable, low-latency vitals on the screen during a fast-moving event; fistula integrity and patient comfort; knowing his readings are landing in the record accurately.

### Scheduler — Tara Nguyen

- **Name:** Tara Nguyen
- **Age:** 38
- **Role:** Scheduler — front-office and care-coordination intake
- **Background:** The scheduler who receives the missed-treatment signal when Marcus's Wednesday chair goes unfilled. She works the EHR appointment-request queue and care-coordination worklist to re-confirm Marcus's Friday slot, arrange the transport that was the original point of failure, and make sure the plan is documented so it does not happen again.
- **Goals:** Get every patient who misses a session promptly re-slotted with the logistics actually solved; keep the chair board full and the schedule realistic.
- **Concerns:** That a no-show becomes invisible until the next missed session; coordinating transport across patients with limited resources; closing the loop with the clinical team and the patient.

### Administrator — James Whitfield

- **Name:** James Whitfield
- **Age:** 52
- **Role:** Administrator — facility and revenue-cycle operations
- **Background:** The administrator who owns the HIS operations dashboard, the billing-export queue, the RPM device-registry governance, and HIPAA/identity oversight for the facility. After Marcus's session, the outpatient HD claim moves through his billing-export queue to EHR for filing, and the RPM home BP cuff is registered and bound to Marcus in the device registry.
- **Goals:** Keep chairs utilized and staff deployed efficiently; get clean claims out the door against Medicare and Medicaid; maintain airtight HIPAA safeguards and a governed device fleet.
- **Concerns:** Empty chairs as both a clinical risk and a revenue leak; claim denials and charge-edit blocks; the compliance and audit exposure of every PHI access and every connected home device.

# Patient Clinical Profile

**Patient:** Marcus Bell

### Demographics

- 58-year-old male
- End-stage kidney disease (ESKD); CKD stage 5
- Lives with and is supported by spouse/caregiver Denise Bell

### Insurance

- **Medicare** — primary (ESRD entitlement)
- **Medicaid** — secondary

### Allergies

- **Penicillin** — rash
- **Iodinated contrast** — mild reaction

### Medical History

- End-stage kidney disease secondary to **diabetic nephropathy**
- Long-standing type 2 diabetes mellitus with progression to ESKD
- Dialysis-dependent via a **left upper-arm AV fistula**

### CKD Stage

- **CKD stage 5 (ESKD)** — dialysis-dependent

### Dialysis Modality

- **In-center hemodialysis**, 3×/week (Monday / Wednesday / Friday)
- **4-hour** sessions
- Vascular access: **left upper-arm AV fistula**
- **Target / dry weight: 78.0 kg**

### Comorbidities

- Type 2 diabetes mellitus
- Hypertension
- **Heart failure with reduced ejection fraction (HFrEF), EF 40%** — narrows the margin for both fluid overload and rapid ultrafiltration
- Secondary hyperparathyroidism
- Anemia of CKD

### Medications

| Drug | Class / purpose |
|---|---|
| Insulin glargine | Basal insulin (T2DM) |
| Lisinopril | ACE inhibitor (HTN / cardiorenal) |
| Carvedilol | Beta-blocker (HFrEF / HTN) |
| Sevelamer carbonate | Phosphate binder |
| Calcitriol | Active vitamin D (secondary hyperparathyroidism) |
| Epoetin alfa | Erythropoiesis-stimulating agent (anemia of CKD) |
| Atorvastatin | Statin |
| Aspirin 81 mg | Antiplatelet |

### Lab Results

| Lab | Friday pre-HD (T0) | Session end (T0+240) |
|---|---|---|
| Potassium (K⁺) | **6.6 mmol/L** (critical hyperkalemia) | **4.9 mmol/L** |
| Creatinine | **9.8 mg/dL** | — |
| Hemoglobin (Hgb) | **9.6 g/dL** | 9.6 g/dL (unchanged session-over) |

*Pre-HD potassium of 6.6 mmol/L on Friday reflects the five-day interdialytic gap after the missed Wednesday session; the post-dialysis 4.9 mmol/L confirms effective correction.*

### Vital Signs

| Timepoint | Weight (kg) | BP (mmHg) | Pulse (bpm) | Temp (°C) |
|---|---|---|---|---|
| **T0 — Friday arrival, pre-HD** | **81.8** (+3.8 kg over dry weight) | **168/92** | **96** | **36.8** |
| **T0+90 — intradialytic hypotension** | ~80.6 | **78/44** | **58** | 36.6 |
| **T0+120 — post-intervention** | ~80.0 | **104/64** | **74** | 36.5 |
| **T0+240 — session end** | **78.3** | **132/80** | **80** | 36.7 |

**Dialysis prescription (Friday, T0):** UF goal **3.8 L**, blood-flow rate (Qb) **400 mL/min**, dialysate potassium bath **2.0**.

**Fluid math (canonical):** 81.8 kg → 78.3 kg = **3.5 L actually removed** against a **3.8 L goal**. The intradialytic hypotension emergency cost roughly 0.3 L of the goal, leaving Marcus **+0.3 kg over dry weight (78.3 vs 78.0 kg)** at session end — a deliberately conservative, safe stopping point given his EF-40% heart, not a failure to dialyze.

# The Story Begins

Marcus Bell has measured the last three years of his life in four-hour increments, three days a week. Monday, Wednesday, Friday. Chair, cannula, four hours, dry weight. At fifty-eight, he knows the rhythm of his fistula the way another man knows the sound of his own car engine — the thrill of it under his fingertips when he wakes, a small daily proof that the machine and the vein are still talking to each other. The fistula sits in his left upper arm, a quiet, ropey companion. The dialysis center staff know him. Kevin Osei, the technician, knows exactly how Marcus likes the cannulation done — slow, with a warning before the stick. Rosa Martinez, the charge nurse, knows that Marcus jokes when he's nervous and goes silent when he's actually scared. Dr. Priya Anand, his nephrologist, has walked him through every number on every panel for three years: the creatinine that won't come down, the hemoglobin that hovers at 9.6, the potassium they watch like a hawk because diabetic kidneys and a tired heart leave no margin for error.

That last part is the thing Marcus tries not to think about. His kidneys failed from diabetes — the same diabetes he's carried for two decades, the same disease that thinned the walls of his heart until his ejection fraction settled at forty percent. His cardiologist calls it HFrEF; Marcus calls it "my heart only does part of the job now." Between a heart that can't pump hard and kidneys that can't drain at all, fluid is the enemy that never sleeps. Every glass of water, every cup of coffee, every interdialytic hour is fluid his body can no longer let go of on its own. The center pulls it off three times a week. Miss a session and the water has nowhere to go — it backs up into his ankles, his belly, and eventually his lungs.

Marcus understands this in his bones. What he doesn't always control is his life around it. Denise, his wife of thirty-one years, drives him to most of his sessions. She works shift hours, and her schedule and his schedule are two gears that don't always mesh. Most weeks they make it work. Most weeks.

So this is the case as it stands at the start: a 58-year-old man with end-stage kidney disease on in-center hemodialysis, target dry weight 78.0 kilograms, Medicare primary and Medicaid secondary, eight home medications, two allergies he never forgets to mention — penicillin and contrast dye. A man who is medically fragile in exactly the ways that punish a missed treatment, and socially fragile in exactly the way that makes one likely. He is, in the language his care team would never say to his face, a patient sitting on a knife's edge between a manageable chronic illness and a hospital bed. The only thing that keeps him on the right side of that edge is showing up. Three days a week. Chair, cannula, four hours, dry weight.

This is the week he doesn't show up.

# Trigger Event

It happens on a Wednesday, and it happens the way these things almost always happen — not through one dramatic failure but through the quiet collision of two ordinary problems.

Denise's shift gets moved. A coworker calls out sick, the manager asks her to cover, and the covering swallows the window in which she normally drives Marcus to his afternoon session. They scramble. There is a phone call about a neighbor who might be able to help, a text that goes unanswered, a bus route that doesn't connect, a copay for a rideshare that feels like one expense too many at the end of the month. None of it is catastrophic on its own. Stacked together, on a day with no slack in it, they add up to the same outcome: no ride to the center. By the time the afternoon arrives, there is no realistic way for Marcus to get to his chair, and the appointment passes.

At the center, the absence does not announce itself with an alarm. It announces itself with an empty chair. In PDMS, on the chairside `/pdms/chairs` board, the slot reserved for Marcus's Wednesday treatment sits open as the shift turns over — a DialysisSession that should have opened and never did. No pre-dialysis weight is entered. No vitals stream into the TimescaleDB hypertable. No cannulation, no prescription, no four hours. Over on the facility side, `/his/today` shows the same hole from the operations angle: a scheduled treatment against an unfilled chair, a no-show standing out against a day that otherwise ran on time. From a pure throughput view, it's a gap in the day's grid. From a clinical view, it is the first domino.

Because for Marcus, a missed Wednesday is not a missed Wednesday. It is a five-day interdialytic gap — Monday's session to Friday's, with nothing in between to take the water off. His heart cannot pump it out. His kidneys cannot pass it. The diabetic nephropathy that put him here also blunts every backup mechanism his body might have used to dump potassium between treatments. So from the moment that chair stays empty, the clock starts on two clocks at once: fluid climbing kilogram by kilogram back over his dry weight, and serum potassium creeping upward with nowhere to go. Neither makes a sound. Both are doing their work right now, in the gap, while Marcus is at home believing — as patients reasonably do — that one missed session can be made up by simply coming in next time.

This is the moment the platform is built to catch. The empty chair is not just a scheduling miss; it is a signal. The expected session that never opened is the seed of a missed-treatment event that will ride the Transponder outbox across module boundaries — out of PDMS, into the care team's awareness, onto a scheduler's queue, toward a portal message and a nephrologist's check-in. Not a replayed log, not a reconstructed history — just one durable fact published once and consumed by everyone who needs to act on it: Marcus Bell missed his Wednesday session, and the people who keep him off that hospital bed need to know before Friday, not after.

The five-day gap has begun. The water is rising. And somewhere in the quiet plumbing of the system, the first event is about to fire.

# Interactive User Journey

> *The Missed Session That Almost Became a Hospitalization* — walking the same arc a clinical and operational team would live, screen by real screen. Marcus Bell, 58, ESKD on in-center HD (M/W/F, 4 h), dry weight 78.0 kg. Every route below is a shipping surface; predictive risk *scoring* is flagged as the platform's designed-for clinical decision support (roadmap-honest), never as a shipped predictive product.

---

**1. Confirm the standing HD schedule and chair assignment**
- **Actor:** Tara Nguyen (Scheduler)
- **Goal:** Verify Marcus's recurring Mon/Wed/Fri 4-hour in-center slots are booked and a chair is reserved for the week.
- **Screen Name:** `/pdms/chairs`
- **Action Taken:** Opens the chair board, confirms Marcus's recurring assignment across the three treatment days, and cross-checks staffing coverage.
- **System Response:** The chair board renders the week's chair-by-shift grid with Marcus's standing slots reserved; the Wednesday chair shows as scheduled-not-yet-open.
- **Business Value:** Capacity is locked before the week starts — no double-booking, predictable chair utilization, and a clean baseline against which a no-show stands out instantly.

**2. Review the day's operational picture**
- **Actor:** James Whitfield (Administrator)
- **Goal:** Get a single-pane view of staff, chairs, inventory, and the billing queue for the day.
- **Screen Name:** `/his/today`
- **Action Taken:** Loads the HIS ops dashboard and scans chair fill, staffing, and inventory levels.
- **System Response:** The dashboard shows today's schedule against live chair status; Wednesday's grid carries an expected-but-unopened session for Marcus.
- **Business Value:** Operations leadership sees facility throughput and exceptions in one place — the foundation for catching the missed session as an operational signal, not an afterthought.

**3. The missed Wednesday session surfaces as an empty chair**
- **Actor:** Kevin Osei (Dialysis Technician)
- **Goal:** Open and run the day's scheduled sessions; flag any chair that never goes live.
- **Screen Name:** `/pdms/sessions`
- **Action Taken:** Works down the session list to start each patient's treatment and finds Marcus's expected DialysisSession never opened — no arrival, no cannulation.
- **System Response:** Marcus's Wednesday session stays in a not-started state; the unfilled chair is recorded as a missed treatment rather than silently dropped.
- **Business Value:** A continuity-of-care gap is captured at the source the moment it happens — the inciting event that the rest of the platform can act on, instead of a discovery days later in arrears.

**4. The missed treatment lands on the care-coordination worklist**
- **Actor:** Tara Nguyen (Scheduler)
- **Goal:** Pick up the missed treatment as an actionable coordination task.
- **Screen Name:** `/ehr/care-coordination/worklist`
- **Action Taken:** Opens the worklist and finds Marcus's missed-treatment item routed in from PDMS, with the 5-day interdialytic gap risk called out.
- **System Response:** The worklist displays the missed-treatment item carried across as an integration event over the Transponder outbox (RabbitMQ) — PDMS detected the gap, EHR care-coordination received it; no direct module coupling, no event log replay.
- **Business Value:** The modular monolith behaves like one care team — a chairside no-show in PDMS becomes a coordinated EHR task automatically, closing the loop that would otherwise fall through the cracks between front office and floor.

**5. Surface the rising clinical risk on the longitudinal chart**
- **Actor:** Dr. Priya Anand (Nephrologist)
- **Goal:** Understand how dangerous the gap is for *this* patient before reaching out.
- **Screen Name:** `/ehr/patients/:id`
- **Action Taken:** Opens Marcus's chart, reviews his ESKD + HFrEF (EF 40%) + diabetic-nephropathy profile, and reads the safety flag raised against the missed session.
- **System Response:** The chart shows the longitudinal history; ClinicalSafetyChecker raises a hyperkalemia-risk flag given the 5-day gap and diabetic ESKD substrate — presented as the platform's designed-for CDS output, kept clinically plausible (elevated, not a shipped predictive product).
- **Business Value:** Decision support concentrates clinician attention on the patient most likely to deteriorate — turning a generic "missed appointment" into a triaged, high-acuity outreach.

**6. Reach out to Marcus through the portal**
- **Actor:** Dr. Priya Anand (Nephrologist)
- **Goal:** Urge Marcus to confirm Friday and report symptoms before he decompensates.
- **Screen Name:** `/portal`
- **Action Taken:** Sends a secure message asking Marcus to confirm his Friday session and describe any swelling, shortness of breath, or weight change.
- **System Response:** The portal delivers the secure message and surfaces it for Marcus (and Denise, assisting him); he replies reporting mild dyspnea and weight gain.
- **Business Value:** Two-way patient engagement happens on a real, audited surface today — closing the outreach loop without waiting for a callback, and capturing patient-reported symptoms that sharpen the Friday plan. *(Native mobile app + dedicated caregiver portal are roadmap; responsive web + Marcus-assisted access today.)*

**7. Conduct a telehealth check-in**
- **Actor:** Dr. Priya Anand (Nephrologist)
- **Goal:** Briefly assess Marcus remotely and reinforce the urgency of Friday's return.
- **Screen Name:** `/portal`
- **Action Taken:** Runs a scheduled check-in via portal secure messaging, reviews reported symptoms, and confirms the Friday plan with Marcus and Denise.
- **System Response:** The portal threads the check-in and scheduling exchange against Marcus's record, time-stamped and audited.
- **Business Value:** A near-admission is intercepted with a low-cost virtual touch. *(A full Telehealth Platform is roadmap; portal secure messaging + scheduling deliver the check-in today — demo-honest.)*

**8. Approve the Friday re-slot and arrange transport**
- **Actor:** Tara Nguyen (Scheduler)
- **Goal:** Re-confirm Friday's session and resolve the transport gap that caused the miss.
- **Screen Name:** `/ehr/appointment-requests`
- **Action Taken:** Approves Marcus's Friday appointment request and records the transport arrangement and adherence note tied to the missed-session root cause (Denise's shift conflict).
- **System Response:** The request moves to approved; the Friday session is reconfirmed and the transport plan is attached to the coordination record.
- **Business Value:** The platform fixes the *cause*, not just the symptom — booking a slot is meaningless without a ride, and addressing both is what actually prevents the next miss.

**9. Marcus arrives Friday — open the session and capture pre-HD vitals**
- **Actor:** Kevin Osei (Dialysis Technician)
- **Goal:** Open the DialysisSession, cannulate the AV fistula, and record pre-HD status.
- **Screen Name:** `/pdms/sessions/:id`
- **Action Taken:** Opens Marcus's Friday session, cannulates the left upper-arm fistula, and enters pre-HD vitals: weight 81.8 kg, BP 168/92, pulse 96, temp 36.8 °C.
- **System Response:** The live chairside view goes active; vitals stream into the TimescaleDB hypertable over the ~2 s ticker on the Valkey-backed SignalR backplane; +3.8 kg over dry weight is immediately visible.
- **Business Value:** The full weight of the missed session is quantified at the chair in real time — 81.8 kg against a 78.0 kg dry weight tells the whole story before a single mL is pulled.

**10. Flag the critical pre-HD potassium and set the prescription**
- **Actor:** Rosa Martinez, RN (Charge Nurse)
- **Goal:** Act on the dangerous pre-HD chemistry and lock the dialysis prescription.
- **Screen Name:** `/ehr/patients/:id`
- **Action Taken:** Starts the Encounter, reviews pre-HD labs (K⁺ 6.6 mmol/L, creatinine 9.8, Hgb 9.6), drafts the ClinicalNote, and confirms the prescription: UF goal 3.8 L, Qb 400, dialysate K⁺ bath 2.0.
- **System Response:** ClinicalSafetyChecker flags the K⁺ 6.6 as critical hyperkalemia; the OrderSet and prescription are recorded against the encounter; the access marked `[PhiAccess]` writes a FHIR AuditEvent.
- **Business Value:** A life-threatening potassium is caught and codified into the treatment plan at point of care — the prescription is built around the emergency before it becomes one.

**11. Intradialytic hypotension emergency — alarm fires chairside**
- **Actor:** Rosa Martinez, RN (Charge Nurse)
- **Goal:** Recognize and respond to a sudden BP collapse mid-session.
- **Screen Name:** `/pdms/sessions/:id`
- **Action Taken:** At ~T0+90 min Marcus cramps and goes near-syncopal; Rosa reads the live nadir (BP 78/44, pulse 58), pauses UF, drops Qb to 250, places him in Trendelenburg, and lowers the dialysate temperature.
- **System Response:** A TreatmentAlarm fires in real time on the live vitals stream; the nadir is plotted from the hypertable; PDMS alarm thresholds surface intradialytic-hypotension risk as designed-for CDS, kept clinically plausible.
- **Business Value:** The crash is detected and acted on within seconds at the chair — the difference between a managed dip and a code, and the pivot point of the whole save.

**12. Chart the saline bolus on the MAR via the infusion pump**
- **Actor:** Rosa Martinez, RN (Charge Nurse)
- **Goal:** Deliver and document the rescue fluid and potassium-bath maneuver.
- **Screen Name:** `/pdms/sessions/:id`
- **Action Taken:** Administers a 200 mL saline bolus through the IV pump, adjusts the potassium bath as a rescue maneuver against the cramping and falling serum K⁺, and charts both on the MAR.
- **System Response:** The MedicationAdministrationRecord captures the bolus with the IvPumpInfusion vendor driver (Alaris/Baxter/Hospira); the RecordReading durable command bus underpins telemetry write durability so no rescue reading is lost.
- **Business Value:** Every emergency intervention is captured the instant it happens, with pump-level fidelity — defensible documentation that also feeds the audit and billing trail downstream.

**13. On-call escalation pages the nephrologist**
- **Actor:** Kevin Osei (Dialysis Technician)
- **Goal:** Get the attending nephrologist into the loop without leaving the chair.
- **Screen Name:** `/pdms/admin/oncall/audit`
- **Action Taken:** The EscalationPolicy triggers on the unresolved alarm; Kevin confirms the page went out and watches the per-attempt dispatch audit.
- **System Response:** AlarmDispatch pages Dr. Anand via ClinicianNotification (Twilio SMS / APNs / FCM) with minimal PHI; the on-call audit logs each attempt and acknowledgement per-attempt.
- **Business Value:** Escalation is automatic, multi-channel, and audited — the right clinician is reached in seconds with a defensible paper trail, not a frantic phone tree.

**14. Stabilize and resume at a lower rate**
- **Actor:** Dr. Priya Anand (Nephrologist)
- **Goal:** Confirm recovery and authorize cautious resumption of fluid removal.
- **Screen Name:** `/pdms/sessions/:id`
- **Action Taken:** Reviews the live stream remotely after the page, confirms recovery at T0+120 (BP 104/64, pulse 74), and approves resuming UF at a lower rate.
- **System Response:** The live view shows the rebound off the nadir; the resumed-lower-rate change is recorded against the session; vitals continue streaming on the backplane.
- **Business Value:** The team threads the needle — pulling enough fluid to address the +3.8 kg overload without re-crashing a HFrEF patient — under attending oversight, all on one synchronized live surface.

**15. Pull the outside ED record and reconcile medications**
- **Actor:** Dr. Priya Anand (Nephrologist)
- **Goal:** Retrieve community records and reconcile Marcus's medications against an external encounter.
- **Screen Name:** `/hie/fhir-exchange`
- **Action Taken:** Queries community records for Marcus, retrieves an outside ED document via XCA, and confirms consent permits the read.
- **System Response:** HIE performs the Query/XCA retrieval behind TEFCA IAS-JWT; Consent policies gate the cross-module read; a FHIR AuditEvent is written; the result is handed to EHR for reconciliation against his home-med list (insulin glargine, lisinopril, carvedilol, sevelamer, calcitriol, epoetin alfa, atorvastatin, ASA 81).
- **Business Value:** A complete, consented medication picture prevents duplicate therapy and drug interactions at exactly the moment clinical decisions are being made — interoperability as patient safety, not paperwork.

**16. Ingest the post-correction lab result via HL7**
- **Actor:** Dr. Priya Anand (Nephrologist)
- **Goal:** Confirm the hyperkalemia corrected before discharge.
- **Screen Name:** `/smartconnect/integrations`
- **Action Taken:** Reviews the inbound HL7 ORU carrying Marcus's basic metabolic panel and confirms it routed cleanly into the chart.
- **System Response:** The Hl7V2ToFhirPipeline routes the ORU by MSH-9 trigger to US Core FHIR, ClamAV scans any attachment, headless Lab records the result via `LabResultReceivedIntegrationEvent`, and the post-correction K⁺ 4.9 surfaces in the EHR chart.
- **Business Value:** The loop from crisis to confirmed correction closes on real interoperability rails — the clinician sees objective proof (6.6 → 4.9) that the intervention worked, on the same record everyone else is using.

**17. Complete the session to near dry weight**
- **Actor:** Rosa Martinez, RN (Charge Nurse)
- **Goal:** Close out the treatment with final vitals and net fluid removed.
- **Screen Name:** `/pdms/sessions/:id`
- **Action Taken:** Records end-of-session status at T0+240: weight 78.3 kg, BP 132/80, pulse 80, K⁺ 4.9 mmol/L, and ends the treatment.
- **System Response:** The session finalizes; the stream records 3.5 L removed against the 3.8 L goal — leaving Marcus +0.3 kg over dry weight, the clinically correct outcome after the emergency cost ~0.3 L of goal.
- **Business Value:** A safe, honest endpoint — the emergency was survived *and* the fluid target nearly met, with the small residual transparently documented rather than overstated.

**18. File the outpatient HD claim from the billing-export queue**
- **Actor:** James Whitfield (Administrator)
- **Goal:** Hand the completed session off to the claim pipeline.
- **Screen Name:** `/his/admin/billing/exports`
- **Action Taken:** Reviews the BillingExportJob for Marcus's Friday session and clicks Execute to hand it to EHR for filing.
- **System Response:** The export job executes; an integration event over the Transponder outbox hands the encounter to EHR Billing for claim creation.
- **Business Value:** Revenue capture is one click off a completed clinical event — no re-keying, no end-of-month reconciliation gap between the floor and the back office.

**19. Code, validate, and submit the Medicare claim**
- **Actor:** James Whitfield (Administrator)
- **Goal:** Turn the session into a clean, payer-validated 837 claim.
- **Screen Name:** `/ehr/admin/billing/dialysis-charges`
- **Action Taken:** Captures the charge coded for outpatient HD (CPT 90935 / 90937 / 90999), validates against the fee schedule, and submits the EDI 837 with Medicare primary / Medicaid secondary.
- **System Response:** Charge → Claim → Remittance flow advances; charge-edit blocking checks the codes; the 837 is filed and 277CA / 999 acknowledgements return against the claim.
- **Business Value:** Revenue-cycle integrity end-to-end — correct coding, edit-checked before submission, with ESRD-entitlement Medicare/Medicaid coordination handled, so the facility gets paid for the care it delivered without denials.

**20. Sign the note and push the After-Visit Summary**
- **Actor:** Dr. Priya Anand (Nephrologist)
- **Goal:** Close the encounter and give Marcus a clear discharge picture.
- **Screen Name:** `/ehr/patients/:id`
- **Action Taken:** Signs the ClinicalNote, closes the Encounter, and generates the After-Visit Summary documenting the emergency, the correction, and the adherence + transport plan.
- **System Response:** The note is signed; the AfterVisitSummary is generated (EHR owns the PatientPortal domain) and pushed to Marcus's portal via an integration event over the Transponder outbox.
- **Business Value:** The patient leaves with an accurate, readable account of a frightening day and a concrete plan — the single best lever against the *next* missed session and re-decompensation.

**21. Marcus reviews his AVS and plan in the portal**
- **Actor:** Marcus Bell (Patient, assisted by Denise Bell)
- **Goal:** Understand what happened and what to do before Monday.
- **Screen Name:** `/portal`
- **Action Taken:** Opens the After-Visit Summary, reads the adherence + transport plan, and confirms the next Monday session.
- **System Response:** The portal renders the AVS, the plan, and the upcoming schedule against Marcus's aggregated record across HIS / EHR / PDMS.
- **Business Value:** Care continuity is handed to the patient in plain language — closing the loop that started with a missed ride, and turning a near-hospitalization into an engaged, informed return.

**22. Enroll the RPM home BP cuff against the device registry**
- **Actor:** James Whitfield (Administrator)
- **Goal:** Stand up home blood-pressure monitoring so interdialytic trends are caught before the next chair.
- **Screen Name:** `/his/admin/devices`
- **Action Taken:** Registers the RPM home BP cuff in the device registry and binds it to Marcus.
- **System Response:** The device is registered and bound-to-patient; future home BP readings will ingest through HIS via IngestDeviceReading on the durable command bus, with telemetry governance applied.
- **Business Value:** The save extends beyond the chair — continuous home monitoring means the next fluid-overload or hypertensive trend is caught early, converting a one-time rescue into durable, between-session safety. Hospitalization avoided, and the substrate for the next near-miss actively monitored.

# Platform Modules Demonstrated

This scenario — *The Missed Session That Almost Became a Hospitalization* — was deliberately authored to exercise every browser app behind the one edge Gateway plus the headless Lab module. Marcus Bell's five-day gap doesn't just make for a tense clinical story; it forces all seven apps to coordinate as a single care team, talking *only* through integration events over the Transponder outbox. Below, each module is mapped honestly to what ships today versus what is on the roadmap.

| Module | Why it appears in this scenario | Honest mapping to real capability |
|---|---|---|
| **HIS** (Hospital Information System) | The empty chair on Wednesday surfaces first as an operations problem, not a clinical one. James Whitfield watches the no-show against the day's schedule on `/his/today`, runs the outpatient-HD claim through the billing-export queue on `/his/admin/billing/exports`, and registers Marcus's RPM home BP cuff on `/his/admin/devices`. | **Real.** Ops dashboard (staff/chairs/inventory/billing queue), `BillingExportJob` queue with Execute hand-off to EHR, and the device registry with durable-command-bus `IngestDeviceReading` all ship today. |
| **EHR** (Electronic Health Record) | The longitudinal home of Marcus's story: the K⁺ 6.6 flag, the encounter, the signed note, the medication reconciliation, the After-Visit Summary, and the missed-treatment care-coordination worklist. | **Real.** `/ehr/patients/:id` chart, `ClinicalSafetyChecker`, `QualityMeasureEvaluator`, `/ehr/care-coordination/worklist`, `/ehr/appointment-requests`, Billing, and the PatientPortal domain (SecureMessage / AfterVisitSummary) all ship. |
| **HIE** (Health Information Exchange, FHIR R4 / IHE) | After a five-day gap with possible outside contact, Dr. Anand needs the community record. HIE pulls an outside ED record and gates the read by consent so reconciliation is safe. | **Real.** `/hie/fhir-exchange` (Query/XCA, inbound feed, consent), US Core mapping with FHIR `AuditEvent`, TEFCA IAS-JWT inbound POST. |
| **PDMS** (Patient Data Management System — chairside) | The dramatic spine: the live chairside session, the intradialytic-hypotension nadir at T0+90, the `TreatmentAlarm`, the saline-bolus MAR entry, and the on-call escalation that pages Dr. Anand. | **Real.** `/pdms/sessions/:id` live chairside on TimescaleDB, ~2s vitals ticker over the Valkey-backed SignalR backplane, MAR + IvPump vendor drivers, on-call escalation via ClinicianNotification. *This is the platform's true "Dialysis Management System."* |
| **SmartConnect** (Mirth-style integration engine) | The post-correction Basic Metabolic Panel arrives from the lab as an HL7 ORU and must be routed, scanned, and normalized to FHIR before it reaches the chart. | **Real.** `/smartconnect/integrations`, `Hl7V2ToFhirPipeline` routing by MSH-9 trigger, ClamAV attachment scanning. |
| **Lab** (headless) | The K⁺ 6.6 at arrival and the K⁺ 4.9 at session end are real LOINC-coded lab results that must flow without a screen of their own. | **Real, headless.** LOINC-coded `LabOrder` lifecycle; consumes `LabResultReceivedIntegrationEvent`; surfaced through the EHR chart — no SPA by design. |
| **Patient Portal** (`/portal`) | Where Marcus (with Denise's help) receives the missed-session nudge, the telehealth check-in messages, and the final After-Visit Summary with his adherence + transport plan. | **Real** as responsive web today. Native mobile app and caregiver delegated/proxy access are **roadmap** — today Denise assists Marcus through his own portal session. |
| **Identity / Admin** (`/admin`) | Every PHI access in this story — the K⁺ result, the medication reconciliation, the device bind — is governed. Admin is where permission gates, HIPAA safeguards, and the audit trail live. | **Real.** `/admin/identity` permission catalog → SPA gates, `/admin/hipaa` safeguard registry + `[PhiAccess]` FHIR `AuditEvent` pipeline + column-level PHI encryption, `/admin/data-protection` GDPR Art.15/17. |

**Template modules mapped honestly (the investor-candor layer):**

- **Mobile Application** — The portal nudge and AVS reach Marcus on his phone *today* via responsive web; a **native app is roadmap**. We do not claim a shipped native binary.
- **Telehealth Platform** — Dr. Anand's post-gap check-in runs **today** on portal secure messaging + a scheduled slot; a **full telehealth product (live video/waiting room) is roadmap**.
- **Analytics / Population Health** — The empty-chair trend and quality view are real on HIS `/his/today` dashboards and EHR `/ehr/population/quality`; this *is* our population-health surface, not a separate analytics product.
- **Billing / Revenue Cycle** — Real end-to-end: HIS `/his/admin/billing/exports` → EHR `/ehr/admin/billing/dialysis-charges` with **EDI 837/277CA/999** and charge-edit blocking.
- **Scheduling** — Real: Tara re-slots Friday and clears the appointment-request via EHR scheduling + `/ehr/appointment-requests`.
- **AI Clinical Decision Support** — **Real today:** EHR `ClinicalSafetyChecker` / `QualityMeasureEvaluator`, PDMS alarm thresholds, and SmartConnect's pluggable imaging inference gate. **Predictive risk *scoring* (hospitalization / readmission / mortality) is presented as the platform's designed-for CDS output — roadmap, kept clinically plausible**, never as a shipped predictive product.

# Real-Time Clinical Data Stream

Captured chairside on PDMS `/pdms/sessions/:id` (vitals over the ~2s ticker into the TimescaleDB hypertable), with K⁺/creatinine/Hgb flowing from Lab → EHR chart. T-2d is the *missed* Wednesday session — no encounter occurred, so no vitals exist; it is the inciting event, not a data row.

| Timepoint | Weight (kg) | Blood Pressure (mmHg) | Pulse (bpm) | Temperature (°C) | Dialysis Parameters (dialysate K⁺ bath / status) | UF Goal (L) | Blood Flow Rate Qb (mL/min) | Potassium K⁺ (mmol/L) | Creatinine (mg/dL) | Hemoglobin (g/dL) |
|---|---|---|---|---|---|---|---|---|---|---|
| **T-2d** — *missed Wednesday session* | — | — | — | — | — *(no encounter)* | — | — | — | — | — |
| **T0** — *Friday arrival, pre-HD* | 81.8 | 168/92 | 96 | 36.8 | K⁺ bath 2.0 / running | 3.8 | 400 | 6.6 | 9.8 | 9.6 |
| **T0+90 min** — *intradialytic hypotension emergency* | ~80.6 | 78/44 | 58 | 36.6 | K⁺ bath 2.0 / **UF paused** | paused | 250 | ~6.0 (falling) | — | — |
| **T0+120 min** — *post-intervention* | ~80.0 | 104/64 | 74 | 36.5 | K⁺ bath 2.0, **potassium-bath maneuver applied**, dialysate temp lowered / **UF resumed at lower rate** | resumed (lower) | 300 | ~5.6 | — | — |
| **T0+240 min** — *session end* | 78.3 | 132/80 | 80 | 36.7 | K⁺ bath 2.0 / completed | 3.5 achieved | 350 | 4.9 | — | 9.6 |

**Reading the stream.** Marcus arrives **+3.8 kg over his 78.0 kg dry weight** at **81.8 kg**, hypertensive (**168/92**) and hyperkalemic (**K⁺ 6.6**) — the classic substrate of a five-day gap in a diabetic ESKD patient with HFrEF EF 40%. The prescription is aggressive by necessity: **UF goal 3.8 L, Qb 400, K⁺ bath 2.0**. Ninety minutes in, the fluid pull outruns his reduced cardiac reserve: **BP collapses to 78/44 with a paradoxically slow pulse of 58** — near-syncope. Rosa Martinez RN pauses UF, drops Qb to 250, gives Trendelenburg + a 200 mL saline bolus, lowers the dialysate temperature, and applies the potassium-bath maneuver. By T0+120 he is recovering (**104/64, pulse 74**) and UF resumes at a gentler rate. By session end he is **stable at 132/80**, **K⁺ corrected to 4.9**, and **down to 78.3 kg**. Net: **3.5 L removed against a 3.8 L goal — Marcus ends +0.3 kg over dry weight**, the correct, honest cost of the emergency. Hospitalization avoided.

# AI Clinical Decision Support

The CDS narrative below distinguishes what fires *today* from what is *designed-for*. **Shipping today:** EHR's `ClinicalSafetyChecker` (the K⁺ 6.6 hard flag), `QualityMeasureEvaluator`, and PDMS real-time alarm thresholds (the 78/44 nadir). **Designed-for / roadmap:** the predictive risk *scores* below — clinically plausible, tied directly to Marcus's data stream, and framed as the decision-support output the platform is built to surface, not a shipped predictive product.

### Risk Scores (designed-for CDS, computed at T0 arrival)

| Risk domain | Score | Why the CDS produced it — tied to the data stream |
|---|---|---|
| **Missed-treatment / non-adherence risk** | **High — 0.82** | A documented missed Wednesday HD with a *transport* root cause; transport-gap misses are highly recurrent. This is the score that first lit up at T-2d and drove the portal nudge + scheduler outreach. |
| **Hyperkalemia / fluid-overload risk** | **Critical — 0.91** | Five-day interdialytic gap + **+3.8 kg over dry weight** + diabetic ESKD. Confirmed, not just predicted, when Lab returned **K⁺ 6.6** — the model's pre-result probability matched the measured value. |
| **30-day hospitalization risk** | **High — 0.74** | The interaction of **HFrEF EF 40%**, K⁺ 6.6, and a large fluid load is the canonical pre-admission picture; rapid UF in a low-EF heart is exactly what risks the intradialytic event the model is warning about. |
| **30-day readmission / ED-return risk** | **Elevated — 0.58** | No index admission yet (this is an averted event), but the same adherence + cardiorenal substrate keeps post-discharge return risk meaningfully above baseline until the transport plan holds. |
| **Intradialytic-hypotension risk** | **High — 0.71 (rising to Critical intra-session)** | EF 40% + an aggressive **3.8 L / Qb 400** prescription. PDMS alarm thresholds converted this designed-for score into a *real* fired alarm at T0+90 when BP hit **78/44, pulse 58**. |
| **12-month mortality risk** | **Moderate-High — 0.34** | Cumulative ESKD + diabetes + HFrEF EF 40% + anemia (Hgb 9.6) + secondary hyperparathyroidism — a longitudinal context score, not an acute trigger; it raises the stakes on every adherence decision. |

### Predictions

- **Pre-session (T0):** "Without rate modulation, probability of a symptomatic intradialytic hypotensive event this session ≈ 0.71, peaking near minute 90." **This prediction came true at exactly T0+90** — the strongest validation in the case.
- **Trajectory:** "If the full 3.8 L is pulled at the planned rate, expect BP instability before dry weight is reached." Realized — the team removed **3.5 L** and accepted **+0.3 kg over dry weight** rather than push a failing heart.
- **Post-discharge:** "Without a durable transport solution, next-missed-session probability remains > 0.6 within 14 days" — the rationale behind enrolling the RPM home BP cuff and locking the adherence + transport plan.

### Alerts (what actually fired vs. designed-for)

- **FIRED (real, today) — `ClinicalSafetyChecker`, EHR `/ehr/patients/:id`:** *Critical hyperkalemia — K⁺ 6.6 mmol/L.* Hard safety flag on the chart at arrival.
- **FIRED (real, today) — PDMS `TreatmentAlarm`, `/pdms/sessions/:id`:** *Intradialytic hypotension — BP 78/44, pulse 58.* Triggered the `EscalationPolicy` → `AlarmDispatch` audit → **ClinicianNotification** page to Dr. Anand (Twilio/APNs/FCM).
- **DESIGNED-FOR — risk-stream alerts:** missed-treatment (T-2d) and 30-day-hospitalization banners on the EHR chart and care-coordination worklist, surfaced as decision support, not auto-action.

### Recommendations (CDS suggestions, clinician-confirmed)

1. **Modulate the pull, don't chase the goal** — reduce UF rate and lower dialysate temperature in an EF-40% patient; accept a partial fluid removal over an unstable session. *Adopted: 3.5 L of 3.8 L, ending +0.3 kg over dry weight.*
2. **Treat the potassium at the source** — maintain the K⁺ 2.0 bath and apply the chairside potassium-bath maneuver as serum K⁺ falls and cramping appears. *Adopted by Rosa Martinez RN; confirmed by end-session K⁺ 4.9.*
3. **Close the adherence loop, not just the session** — enroll an RPM home BP cuff via HIS `/his/admin/devices` and lock a transport plan, because the model's residual readmission risk is adherence-driven. *Adopted at discharge; future home BP ingests via the durable command bus.*
4. **Reconcile against the outside record** — pull and consent-gate the community ED record through HIE before signing the note, given the unobserved five-day gap. *Adopted in EHR medication reconciliation.*

# Interoperability Showcase

Marcus Bell's near-hospitalization is, underneath the clinical drama, an interoperability story. No single module saw the whole picture on its own — HIE pulled the outside ED record, SmartConnect transformed the reference-lab HL7 message into FHIR, Lab recorded the LOINC-coded result, and EHR reconciled it all against his home-med list. Below is the *actual data* that moved across those wires on Friday, not a mock-up.

### The patient, as a US Core Patient resource

When HIE's Outbound slice maps Marcus's identity into FHIR R4 for partner exchange, it emits a US Core `Patient`. This is what a TEFCA partner receives:

```json
{
  "resourceType": "Patient",
  "id": "marcus-bell-eskd",
  "meta": {
    "profile": [
      "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"
    ]
  },
  "identifier": [
    {
      "use": "usual",
      "type": {
        "coding": [
          {
            "system": "http://terminology.hl7.org/CodeSystem/v2-0203",
            "code": "MR",
            "display": "Medical Record Number"
          }
        ]
      },
      "system": "urn:oid:2.16.840.1.113883.19.5.99999.2",
      "value": "MB-0058431"
    },
    {
      "use": "official",
      "type": {
        "coding": [
          {
            "system": "http://terminology.hl7.org/CodeSystem/v2-0203",
            "code": "MC",
            "display": "Patient's Medicare Number"
          }
        ]
      },
      "system": "http://hl7.org/fhir/sid/us-mbi",
      "value": "1EG4-TE5-MK73"
    }
  ],
  "active": true,
  "name": [
    { "use": "official", "family": "Bell", "given": ["Marcus"] }
  ],
  "telecom": [
    { "system": "phone", "value": "+1-216-555-0142", "use": "home" }
  ],
  "gender": "male",
  "birthDate": "1967-09-12",
  "address": [
    {
      "use": "home",
      "line": ["4218 Larchmere Blvd"],
      "city": "Cleveland",
      "state": "OH",
      "postalCode": "44120",
      "country": "US"
    }
  ],
  "communication": [
    {
      "language": {
        "coding": [
          {
            "system": "urn:ietf:bcp:47",
            "code": "en",
            "display": "English"
          }
        ]
      },
      "preferred": true
    }
  ]
}
```

### The critical lab, as a US Core Observation — serum potassium 6.6 mmol/L

This is the value that turned a no-show into a clinical emergency. LOINC **6298-4** (Potassium [Moles/volume] in Blood), drawn pre-HD on Friday, flagged High and surfaced to **EHR `/ehr/patients/:id`** where `ClinicalSafetyChecker` raised it against the order set:

```json
{
  "resourceType": "Observation",
  "id": "obs-k-marcus-t0",
  "meta": {
    "profile": [
      "http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-lab"
    ]
  },
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory",
          "display": "Laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "6298-4",
        "display": "Potassium [Moles/volume] in Blood"
      }
    ],
    "text": "Potassium, Blood"
  },
  "subject": { "reference": "Patient/marcus-bell-eskd" },
  "effectiveDateTime": "2026-06-08T07:42:00-04:00",
  "issued": "2026-06-08T08:05:00-04:00",
  "valueQuantity": {
    "value": 6.6,
    "unit": "mmol/L",
    "system": "http://unitsofmeasure.org",
    "code": "mmol/L"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "HH",
          "display": "Critical high"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "low": { "value": 3.5, "unit": "mmol/L", "system": "http://unitsofmeasure.org", "code": "mmol/L" },
      "high": { "value": 5.1, "unit": "mmol/L", "system": "http://unitsofmeasure.org", "code": "mmol/L" }
    }
  ]
}
```

### The panel, as a DiagnosticReport

The pre-HD Basic Metabolic Panel that carried the potassium also carried the creatinine 9.8 mg/dL. HIE wraps the panel as a US Core `DiagnosticReport` bundling the member observations:

```json
{
  "resourceType": "DiagnosticReport",
  "id": "dr-bmp-marcus-t0",
  "meta": {
    "profile": [
      "http://hl7.org/fhir/us/core/StructureDefinition/us-core-diagnosticreport-lab"
    ]
  },
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v2-0074",
          "code": "CH",
          "display": "Chemistry"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "51990-0",
        "display": "Basic metabolic panel - Blood"
      }
    ],
    "text": "Basic Metabolic Panel"
  },
  "subject": { "reference": "Patient/marcus-bell-eskd" },
  "effectiveDateTime": "2026-06-08T07:42:00-04:00",
  "issued": "2026-06-08T08:05:00-04:00",
  "performer": [
    { "display": "Regional Reference Laboratory (CLIA 36D0652988)" }
  ],
  "result": [
    { "reference": "Observation/obs-k-marcus-t0", "display": "Potassium 6.6 mmol/L (HH)" },
    { "reference": "Observation/obs-creatinine-marcus-t0", "display": "Creatinine 9.8 mg/dL" }
  ]
}
```

### Medication reconciliation, as a MedicationStatement

When HIE pulls Marcus's outside ED record, EHR reconciles it against the home-med list. Here is the `MedicationStatement` for the ACE inhibitor — clinically the one that matters most, because lisinopril plus a 5-day potassium load is exactly what drives K⁺ to 6.6:

```json
{
  "resourceType": "MedicationStatement",
  "id": "medstmt-lisinopril-marcus",
  "meta": {
    "profile": [
      "http://hl7.org/fhir/us/core/StructureDefinition/us-core-medicationstatement"
    ]
  },
  "status": "active",
  "medicationCodeableConcept": {
    "coding": [
      {
        "system": "http://www.nlm.nih.gov/research/umls/rxnorm",
        "code": "314076",
        "display": "Lisinopril 10 MG Oral Tablet"
      }
    ],
    "text": "Lisinopril 10 mg PO daily"
  },
  "subject": { "reference": "Patient/marcus-bell-eskd" },
  "effectiveDateTime": "2026-06-08",
  "dateAsserted": "2026-06-08T07:50:00-04:00",
  "informationSource": {
    "display": "Reconciled from community HIE record + patient-confirmed home med list"
  },
  "reasonCode": [
    {
      "coding": [
        {
          "system": "http://hl7.org/fhir/sid/icd-10-cm",
          "code": "I12.0",
          "display": "Hypertensive chronic kidney disease, stage 5 or ESRD"
        }
      ]
    }
  ],
  "dosage": [
    {
      "text": "10 mg by mouth once daily",
      "timing": { "repeat": { "frequency": 1, "period": 1, "periodUnit": "d" } },
      "route": {
        "coding": [
          {
            "system": "http://snomed.info/sct",
            "code": "26643006",
            "display": "Oral route"
          }
        ]
      },
      "doseAndRate": [
        {
          "doseQuantity": {
            "value": 10,
            "unit": "mg",
            "system": "http://unitsofmeasure.org",
            "code": "mg"
          }
        }
      ]
    }
  ]
}
```

### The raw HL7 v2 ORU^R01 — what actually came off the reference-lab interface

Before any of the FHIR above existed, the reference laboratory sent a classic HL7 v2.5.1 `ORU^R01` result message across the LIS interface. **SmartConnect `/smartconnect/integrations`** received it; the `Hl7V2ToFhirPipeline` routed it by MSH-9 trigger (`ORU^R01`) into the per-trigger mapper, ClamAV cleared the (absent) attachment, and the pipeline produced the US Core `Observation` and `DiagnosticReport` shown above. **Lab** consumed the resulting `LabResultReceivedIntegrationEvent` and recorded the LOINC-coded observation, which surfaced in the EHR chart:

```text
MSH|^~\&|REFLAB|REGREF^36D0652988^CLIA|SMARTCONNECT|DIALYSIS|20260608080500-0400||ORU^R01^ORU_R01|MSG00006298|P|2.5.1
PID|1||MB-0058431^^^DIALYSIS^MR~1EG4TE5MK73^^^CMS^MC||Bell^Marcus^^^^^L||19670912|M|||4218 Larchmere Blvd^^Cleveland^OH^44120^US||^PRN^PH^^^216^5550142
PV1|1|O|DIAL^CHAIR-07^^DIALYSIS||||1234567890^Anand^Priya^^^^MD^^NPI|||NEPH
ORC|RE|EHR-LO-44128|REFLAB-77310|||||||||1234567890^Anand^Priya^^^^MD^^NPI
OBR|1|EHR-LO-44128|REFLAB-77310|51990-0^Basic metabolic panel - Blood^LN|||20260608074200-0400|||||||20260608074200-0400|SER^Serum|1234567890^Anand^Priya^^^^MD|||||||20260608080500-0400|||F
OBX|1|NM|6298-4^Potassium [Moles/volume] in Blood^LN||6.6|mmol/L|3.5-5.1|HH|||F|||20260608074200-0400
OBX|2|NM|2160-0^Creatinine [Mass/volume] in Serum or Plasma^LN||9.8|mg/dL|0.7-1.3|H|||F|||20260608074200-0400
OBX|3|NM|2823-3^Potassium [Moles/volume] in Serum or Plasma^LN||6.5|mmol/L|3.5-5.1|HH|||F|||20260608074200-0400
OBX|4|NM|1963-8^Bicarbonate [Moles/volume] in Serum or Plasma^LN||18|mmol/L|22-29|L|||F|||20260608074200-0400
NTE|1||Critical value K+ 6.6 mmol/L called to Rosa Martinez, RN at 0808 per critical-result protocol.
```

### External Lab Integration

The reference lab never talks to the EHR directly. The path is **SmartConnect → Lab → EHR**, all decoupled by integration events:

1. **Order out.** Dr. Anand's pre-HD BMP order is a `LabOrder` in the headless **Lab** module (Placed → Transmitted). Lab publishes `LabOrderPlacedIntegrationEvent`; **SmartConnect** consumes it and transmits the order to the LIS over an outbound HL7 channel. The order moves Placed → Transmitted → InProgress.
2. **Result in.** The `ORU^R01` above arrives inbound at SmartConnect `/smartconnect/integrations`. The `Hl7V2ToFhirPipeline` keys on MSH-9, maps OBX segments to LOINC-coded US Core `Observation`s, and ClamAV scans any attachment before anything is trusted.
3. **Record + surface.** Lab consumes `LabResultReceivedIntegrationEvent`, records the observations (LabOrder → Resulted), and the values surface in the **EHR `/ehr/patients/:id`** chart — where the K⁺ 6.6 trips `ClinicalSafetyChecker`. The same channel later carries the post-correction **K⁺ 4.9 mmol/L** at session end.

No direct module reference anywhere — every hop is a Transponder outbox event over RabbitMQ.

### External Hospital Integration

Marcus had a brush with the system during his 5-day gap that the dialysis center did not order: a community ED visit. **HIE `/hie/fhir-exchange`** is how the platform learned about it:

- **Pull-side query.** HIE's Query slice issues a FHIR patient-discovery + record-retrieval call against the community partner, with **XCA** document queries for any associated C-CDA, all behind **Polly retry**. The partner endpoint authenticates the request with a **TEFCA IAS-JWT** minted by HIE's QHIN onboarding material (`/hie/admin/tefca/partners` — trust anchors + mTLS).
- **Consent-gated.** Before EHR can read the returned resources cross-module, HIE's **Consent** policies (`IsResourceAccessPermittedQuery`) gate the access, and every retrieval writes a FHIR **`AuditEvent`** trail.
- **Inbound feed.** Anything a partner pushes lands via `POST /fhir/{Type}` behind the same TEFCA IAS-JWT middleware, validated against US Core profiles and consent, then enqueued as `Hl7FhirResourceReceivedIntegrationEvent` for the owning module to consume.

The community ED `Encounter` and its discharge med list are exactly what feed the medication-reconciliation step.

### Referral Exchange

When Dr. Anand decides Marcus needs cardiology follow-up for his HFrEF (EF 40%) — the comorbidity that made the rapid ultrafiltration so dangerous — the referral is a first-class EHR `Referral` raised from **EHR `/ehr/patients/:id`** within the encounter (`RequestReferral`). On the wire it leaves the building as a US Core `ServiceRequest` through **HIE Outbound**, which maps the integration event to FHIR, dispatches to the partner endpoint with Polly retry and TEFCA IAS-JWT auth, and tracks it through Pending → Dispatching → Delivered. The accompanying clinical packet (the signed note, the AVS) rides as a **`DocumentReference`** from HIE's Documents slice — branded PDF, PAdES-signed so the signature covers the filled values.

### Medication Reconciliation

This is where the threads converge in the **EHR `/ehr/patients/:id`** chart. The reconciliation compares three sources:

- **Home-med list (canonical):** insulin glargine, lisinopril, carvedilol, sevelamer carbonate, calcitriol, epoetin alfa, atorvastatin, aspirin 81 mg.
- **Community ED record** pulled by HIE (the `MedicationStatement` above is one reconciled entry).
- **Chairside MAR** from PDMS for what was actually given in the chair (the saline bolus during the emergency).

The clinically load-bearing finding: **lisinopril** (ACE inhibitor) plus a missed Wednesday session is a textbook hyperkalemia substrate. Reconciliation surfaces it, `ClinicalSafetyChecker` flags the K⁺ 6.6 against the active ACE-inhibitor statement, and the plan carries a hold/recheck decision into the After-Visit Summary. The reconciliation output itself is republished as a US Core resource through HIE so the community record and the dialysis record finally agree.

---

# Exception Path

The interoperability story above is the happy path. It is not what happened first. The very mechanism that makes external lab integration work — matching an inbound HL7 identifier to the right patient — is also where it can quietly go wrong, and on Friday morning it did.

### The complication: an ADT identifier mismatch caught by the MPI

The reference lab's `ORU^R01` carried Marcus in PID-3 as two identifiers: the dialysis MRN `MB-0058431` and a Medicare MC identifier rendered without dashes, `1EG4TE5MK73`. But the community ED that HIE had queried during the gap had registered Marcus under a *different* local MRN and a transposed date-of-birth digit (`1967-09-21` instead of `1967-09-12`) — a classic registration typo at an outside facility. When HIE tried to link the inbound community `Encounter` to Marcus's existing record, the demographic keys did not cleanly agree.

The platform did **not** silently auto-merge, and it did **not** attach a critical K⁺ 6.6 to the wrong chart. Here is exactly how it behaved.

**1. The pipeline holds, it does not guess.**
SmartConnect's `Hl7V2ToFhirPipeline` mapped the `ORU^R01` cleanly — the *lab* result's PID-3 MRN `MB-0058431` matched Marcus unambiguously, so the K⁺ 6.6 flowed to the correct chart and the clinical emergency response was never delayed. The mismatch was on the *community ED record* HIE had pulled, not the lab result. This separation matters: a demographic ambiguity on an outside ADT record must never block a critical local lab value, and it didn't.

**2. The MPI flags the duplicate for human review.**
The ambiguous community record did not get auto-linked. It landed on **HIE `/hie/admin/mpi/reviews`** as a candidate duplicate — the master-patient-index duplicate-review queue. The record showed the two contributing identities side by side: matching name (Bell, Marcus) and Medicare MBI, but a 1-digit DOB transposition and a different local MRN. Confidence was high-but-not-certain — precisely the band where the system asks a human rather than acting.

**3. Cross-module reads stay gated.**
While the candidate sat unresolved, HIE's **Consent** policies (`IsResourceAccessPermittedQuery`) and the unresolved MPI link meant the EHR reconciliation step treated the community ED meds as *unconfirmed external source*, not as authoritative chart entries. Marcus's home-med list remained the canonical source; the community `MedicationStatement` was surfaced to Dr. Anand as "pending reconciliation," not auto-applied.

**4. A human resolves it, and the resolution is audited.**
James Whitfield (administrator) — or a designated HIM reviewer — opens `/hie/admin/mpi/reviews`, confirms from the matching Medicare MBI and address that this is the same Marcus Bell, and approves the link. The DOB typo is recorded as a known external-source discrepancy, not propagated into the local record. The merge writes a FHIR **`AuditEvent`** (the `[PhiAccess]` audit pipeline marks the review action), so there is a permanent, replayable record of *who linked which identities and why* — exactly what a CMS or OCR auditor would ask for.

**5. The reconciled record flows downstream cleanly.**
Once linked, the community ED `Encounter` and its meds become a confirmed reconciliation source in **EHR `/ehr/patients/:id`**, the lisinopril `MedicationStatement` is finalized, and the corrected, single Marcus Bell identity republishes outbound through HIE so the community partner and the dialysis center converge on one patient.

**Why this is the right behavior.** The naive design auto-merges on a name+MBI match and hopes the DOB typo doesn't matter. That is how the wrong potassium ends up on the wrong chart and a hyperkalemic patient gets sent home. This platform's design — *match the unambiguous local lab immediately, queue the ambiguous external identity for human review, keep cross-module reads consent-gated, and audit the merge* — is slower by one human step and dramatically safer. The exception cost minutes of a reviewer's time. The alternative costs a patient.

---

# Emergency Path

At T0+90 minutes, ninety minutes into a session built to pull 3.8 liters off a man with an ejection fraction of 40%, the chair telemetry turned. This is the spine of the case, and it is where the modular monolith stopped being an architecture diagram and became a care team moving in seconds.

### T0+90 — The alarm fires

The ~2-second vitals ticker streaming into the **PDMS** TimescaleDB hypertable over the Valkey-backed SignalR backplane caught the fall in real time on **`/pdms/sessions/:id`**:

- **BP 78/44 mmHg** (nadir), down from a pre-HD 168/92
- **Pulse 58 bpm** — a relative bradycardia against the hypotension, ominous in a beta-blocked HFrEF patient on carvedilol
- Marcus reporting leg **cramping** and **near-syncope** ("the room is going")

PDMS raised a `TreatmentAlarm` against the live `DialysisSession`. On the chairside screen the vitals tile went red and the alarm banner surfaced the intradialytic-hypotension condition. This is the platform's real-time CDS today: **PDMS alarm thresholds** firing on streamed telemetry — not a roadmap predictive score, but a deterministic threshold breach on actual vitals.

### T0+90 to ~T0+95 — Kevin and Rosa respond at the chair

Kevin Osei (PCT), watching the machine, called it the instant the banner hit. Rosa Martinez, RN (charge nurse) was at the chair within seconds and ran the intradialytic-hypotension protocol, charting each step as she went:

1. **Reduce the fluid pull.** UF rate **paused**; blood pump **Qb dropped 400 → 250 mL/min** to ease the hemodynamic load. These writes landed on the live session via the **RecordReading durable command bus** — telemetry-shape writes where durability matters most, so the record of *what the machine was set to during a crisis* cannot be lost to a transient broker hiccup.
2. **Trendelenburg.** Chair tipped head-down to autotransfuse central volume.
3. **200 mL normal saline bolus.** Delivered through the **IvPumpInfusion** path with the vendor pump driver (Alaris/Baxter/Hospira); Rosa charted it on the chairside **MedicationAdministrationRecord (MAR)** in **PDMS**, so the bolus is a discrete, timestamped, auditable medication event — and is exactly the entry that later reconciles against the home-med list.
4. **Lower the dialysate temperature** to improve vascular tone (cool-temp dialysis).
5. **Adjust the potassium bath** as a maneuver in response to the falling serum K⁺ and the cramping — an intervention on the prescription, with the canonical dialysate K⁺ bath of 2.0 as the written baseline.

### T0+90 onward — Escalation: paging Dr. Anand

A nadir of 78/44 with bradycardia in an HFrEF patient is not a "chart it and watch" event. The PDMS on-call machinery escalated automatically, in parallel with the chairside response:

- The `TreatmentAlarm` triggered the **EscalationPolicy** configured at **`/pdms/admin/oncall/policies`**, resolving the active rotation at **`/pdms/admin/oncall/rotation`** — Dr. Priya Anand, on call that Friday.
- **ClinicianNotification** paged her: a PHI-light alert ("Intradialytic hypotension, Chair 07, BP 78/44, paged per IDH policy") fanned across the registered channels — **Twilio SMS / APNs iOS / FCM Android** — returning a per-channel `ChannelOutcome`.
- Every attempt was written to the **AlarmDispatch per-attempt audit** at **`/pdms/admin/oncall/audit`**: which clinician, which channel, sent/delivered/acknowledged, with timestamps. When Dr. Anand acknowledged, the audit row closed the loop. This is the artifact that proves, after the fact, that the on-call physician was notified and responded — the question every M&M review and every regulator asks.

### Team communication

The response was not three people working three screens in isolation:

- **Chairside (PDMS `/pdms/sessions/:id`):** Kevin and Rosa shared the single live session view — same red banner, same streaming vitals, same MAR — so the technician setting the machine and the nurse pushing saline saw identical state.
- **Physician (ClinicianNotification + the chart):** Dr. Anand acknowledged the page, pulled up the live session and Marcus's **EHR `/ehr/patients/:id`** chart remotely, and concurred with the protocol — cool-temp, slower UF, hold the aggressive pull. The escalation and her acknowledgement are linked in the AlarmDispatch audit.
- **Front office (EHR care-coordination):** the event also kept Tara Nguyen's coordination context current, so transport and follow-up planning reflected what had just happened in the chair.

All of it crossed module boundaries the one legal way: integration events over the Transponder outbox on RabbitMQ. No module reached into another's database.

### T0+120 — Recovery

Thirty minutes after the nadir, the interventions held:

- **BP 104/64 mmHg**, **pulse 74 bpm** — perfusing again, off the floor
- **UF resumed at a lower rate**, the pull restarted cautiously rather than re-chasing the original aggressive goal
- Cramping settled; the near-syncope passed

Marcus stabilized. The session continued, deliberately gentler.

### T0+240 — Session end and the documentation produced

The session ran to completion on the slower trajectory:

- **Weight 78.3 kg** (from 81.8) — **3.5 L actually removed against the 3.8 L goal**, leaving Marcus **+0.3 kg over dry weight**. The emergency cost ~0.3 L of the goal. That is the honest, correct outcome: not a perfect run, a *safe* one.
- **BP 132/80, pulse 80**
- **Post-correction serum K⁺ 4.9 mmol/L** — from a critical 6.6 down into range, the single number that says the day worked.

The crisis produced a complete, defensible documentation trail, each artifact on a real surface:

1. **PDMS chairside record** — the `DialysisSession` with the full intradialytic vitals stream in the TimescaleDB hypertable (the 78/44 nadir is permanently in the curve), the `TreatmentAlarm`, and the MAR entry for the 200 mL saline bolus via the IvPumpInfusion driver.
2. **On-call escalation audit** — the AlarmDispatch per-attempt record at `/pdms/admin/oncall/audit`: who was paged, on which channels, sent/delivered/acknowledged, when.
3. **Signed clinical note** — Dr. Anand documents the intradialytic-hypotension event and the response in **EHR `/ehr/patients/:id`** and signs it (`SignClinicalNote`).
4. **PDMS shift/discharge report** — a Mustache-templated PDF from **`/pdms/admin/reporting/templates`** capturing the session and the event for the shift handoff.
5. **After-Visit Summary** — EHR generates the `AfterVisitSummary` (EHR owns the PatientPortal domain) and pushes it to **Patient Portal `/portal`**, where Marcus and Denise can read what happened, the adjusted plan, and the next-steps.

**The outcome.** Hospitalization avoided. A man arrived hyperkalemic and fluid-overloaded after a missed session, hit a genuine hemodynamic crisis mid-treatment, and walked out the same day at near-dry-weight with a normal potassium, a signed note, an AVS in his portal, an RPM home BP cuff being registered to him in the **HIS device registry** (`/his/admin/devices`), and a transport-and-adherence plan to keep the next Wednesday from ever going dark. The platform didn't prevent the crisis — patients are not algorithms. It detected it in two seconds, escalated it in parallel, documented it completely, and turned a near-admission into a managed outpatient event.

# Patient Experience

Marcus Bell is fifty-eight, and for the last three years his life has run on a rhythm of three: Monday, Wednesday, Friday, four hours in the chair, a left-arm fistula that hums under his fingers, a dry weight of 78.0 kilograms he has learned to feel in his own breathing. He is not a "user." He is a man with diabetic kidney failure, a heart that pumps at forty percent, and a wife, Denise, who has rearranged a decade of shift work around getting him to dialysis. The platform's job, on the worst week of his recent memory, was not to dazzle him. It was to catch him.

## The Wednesday that didn't happen

On Wednesday, the ride fell through. Denise's shift moved, the transport gap opened, and the chair Marcus should have filled at the center stayed empty. There was no alert on his phone that morning, no dramatic red banner — just a quiet absence that, in the old world, nobody would have noticed until Friday, or until the ambulance.

What Marcus didn't see is the part that mattered. The session that never opened in chairside dialysis became a signal. By that evening, when he opened the Patient Portal on his phone — responsive web, the same `/portal` he uses to check his next appointment (a native app is on the roadmap, but tonight the browser does the job) — there was a secure message waiting. Not a form letter. It named the missed Wednesday, asked plainly how he was feeling, whether he was short of breath, whether his ankles were swelling, and pushed him to confirm Friday. It told him a ride was being arranged.

That message did not come from nowhere. It came because the missed treatment had traveled, as an integration event over the platform's outbox, into the care team's worklists — but to Marcus it simply felt like someone at the center had been paying attention to him. He tapped "Yes, I'll be there Friday." Denise, sitting beside him, read it too, helping him through his own portal the way she helps with everything (a true delegated caregiver login is still on the roadmap; for now she leans over his shoulder). For the first time in two days, the dread eased a notch.

## A doctor, on a screen, before it got bad

Thursday, a second portal notification: Dr. Priya Anand wanted a quick check-in. Through the portal's secure messaging and a scheduled slot, she asked the questions that a nephrologist asks when a heart-failure patient has gone five days without dialysis — the breathlessness lying flat, the weight, the missed doses. (A full telehealth platform is roadmap; what exists today, and what Marcus used, is the portal's secure-message thread and scheduled check-in — and it was enough.) She did not frighten him. She told him the truth: this Friday's session mattered more than usual, come in, we'll take it slow, we'll watch you closely.

Marcus would later say that conversation was the moment he stopped feeling like a problem and started feeling like a patient again.

## Friday, in the chair

He arrived Friday eighty-one point eight kilograms — nearly four kilos of fluid his kidneys couldn't shed, his blood pressure 168 over 92, a tightness in his chest when he climbed the two steps into the center. He didn't see the potassium of 6.6 that lit up the clinical chart, didn't see the safety checker flag it. He saw Kevin Osei cannulate the fistula with the easy competence of someone who'd done it a hundred times, saw Rosa Martinez set the machine, felt the cool start of the circuit.

Ninety minutes in, the floor tilted. Cramping seized his legs; the room went gray and distant; his pressure had crashed to 78 over 44. What Marcus experienced as near-fainting, the team experienced as a system already responding — the chairside alarm, the saline bolus, the chair tipped back, the machine easing off. He doesn't remember the numbers. He remembers Rosa's hand on his shoulder and her voice telling him he was safe, that this happens, that they had him. Twenty minutes later he was back — 104 over 64, the gray receding, his own pulse steady under his thumb.

## Going home with a plan, not a discharge slip

By the end — 78.3 kilograms, blood pressure 132 over 80, potassium down to a safe 4.9 — Marcus was tired but upright. Three and a half liters off; he ended a hair above his dry weight, and Rosa explained exactly that, in his words: *we pulled almost everything, we stopped a little short on purpose because your body told us to, and that's the right call.*

Then the part that turns a scary day into a managed one. His After-Visit Summary arrived in the portal before Denise had pulled the car around — what happened today, what changed, what to watch for, when to come back, written for him and not for a chart. With it: a plan he could actually see. A home blood-pressure cuff, enrolled through the device registry, so his morning readings would flow back to the team between sessions instead of disappearing into a notebook. A transport arrangement so that Wednesday could never quietly vanish again. An adherence plan that treated the missed session as a logistics failure to solve, not a character flaw to scold.

Marcus drove home Friday night. Not to an emergency department. Not to a hospital bed. To his own kitchen, with a summary in his pocket, a cuff on the counter, and the quiet, hard-won sense that the people — and the system behind them — had been watching the whole time.

# Administrative Experience

James Whitfield runs this facility on margins that don't forgive surprises. A missed session is a clinical risk; it is also an empty chair, a stranded staff hour, an ESRD claim that may or may not clean. His view of Marcus Bell's week is not the chart — it's the operations surface at `/his/today`, and what it tells him is whether the center is running, and whether it's solvent.

## The empty chair, seen in real time

Wednesday, when Marcus's expected chairside session never opened, the consequence showed up on James's ops dashboard before lunch. The `/his/today` board ties the day's schedule to staff, chairs, inventory, and the billing queue in one frame; alongside the chair board at `/pdms/chairs`, the no-show read as exactly what it was — a slot that paid nothing, staffed for a patient who hadn't come. In the old world a missed treatment was a clinical event the front office learned about days later. Here it was an operational line item the same morning, and because the missed-session signal had already traveled as an integration event into care coordination and scheduling, James could see the chair wasn't just empty — it was already being re-slotted for Friday by Tara Nguyen, transport in motion. Capacity recovered instead of capacity lost.

## Capacity, staffing, and the chairs that pay the rent

The numbers James watches are the unglamorous ones that decide whether the center survives: chair utilization across the day's shifts, machine availability, the staffing ratio behind those chairs — Rosa on the floor as charge nurse, Kevin on telemetry, the coverage that has to be there before a single session opens. When Friday's emergency forced Marcus's session to slow, pause, and re-ramp, the chair stayed occupied longer than the nominal four hours — a utilization fact James can see, and one that matters because a chair held by a stabilizing patient is a chair not available to the next shift. The platform doesn't hide that tension; it surfaces it where the person who has to balance it can act.

The reference-architecture capability map at `/his/workflows` gives James the wider operational frame — the facility's processes laid out against a known healthcare-operations model — so that "what happened to one chair on Friday" sits inside "how this facility is supposed to run."

## Turning a saved patient into a clean claim

A hospitalization avoided is the clinical headline. The revenue story runs alongside it, and James owns both ends. Marcus's Friday session is an outpatient hemodialysis encounter — coded against CPT 90935 / 90937 / 90999, Medicare primary on his ESRD entitlement, Medicaid secondary. James's lever is the billing-export queue at `/his/admin/billing/exports`: the `BillingExportJob` lands there, and the **Execute** action hands it off — as an integration event, never a back-channel — to EHR's billing engine, where the charge becomes a claim, the 837 goes out, and the 277CA/999 acknowledgements come back. He doesn't have to leave his operational surface to know the day's care converted into a filed claim; he watches the queue drain and the hand-off complete.

What James cares about at the dashboard altitude is the shape of the funnel: sessions delivered, jobs queued, jobs executed, claims that cleared versus claims that snagged on a charge edit. A near-admission that becomes a managed outpatient session is, to the revenue-cycle line, the difference between a clean outpatient claim and the downstream cost of an inpatient stay the program would have absorbed. The platform lets him see that the right, cheaper, better outcome also billed correctly.

## Governance: the devices and the access behind the care

The Friday plan added a home blood-pressure cuff for Marcus, and that, too, is James's responsibility — not as a clinical order but as governed infrastructure. The RPM device registry at `/his/admin/devices` is where the cuff is registered and bound to Marcus, where its future home readings will ingest under the durable command bus so a telemetry write is acknowledged into a durable queue, not merely hoped into a database. James governs the fleet: which devices exist, which patient each is bound to, what's allowed to flow.

Underneath all of it sits the access and compliance posture James answers for — the identity and HIPAA safeguard surfaces under `/admin`, where the permission catalog decides who can touch what and the safeguard registry runs live checks on the protections around every PHI-marked access. A week where a vulnerable patient was caught early, stabilized safely, billed cleanly, and sent home on a monitored plan is, from the administrator's chair, also a week where the chairs were utilized, the staff was covered, the claim was clean, and the access was governed. That is what `/his/today` is for: proof, by the end of the day, that doing right by Marcus and running a sustainable center were the same decision.

# Billing and Revenue Cycle

The clinical save is only half the story. For Marcus Bell's Friday session to keep the lights on at the dialysis center, the encounter has to become a clean, payable claim — and on the Dialysis platform that journey is split deliberately across two modules, joined only by an integration event over the Transponder outbox. **HIS owns the operational hand-off; EHR owns the claim lifecycle.** No direct module reference connects them; the entire revenue cycle is event-driven.

## From completed session to billing queue

When Rosa Martinez, RN closes Marcus's session at T0+240 (weight 78.3 kg, K⁺ 4.9, 3.5 L removed), the chairside record in PDMS and the closed Encounter in EHR are now the billable substrate. James Whitfield, the facility's revenue-cycle administrator, works the **HIS billing-export queue at `/his/admin/billing/exports`**. Here a `BillingExportJob` for the day's outpatient hemodialysis encounters sits ready. James reviews it and clicks **Execute** — and that single action is the hinge of the whole revenue cycle.

Execute does not file a claim itself. It publishes an integration event over the Transponder outbox (RabbitMQ); EHR's Billing slice consumes it and begins the claim lifecycle on its own side. This is the same coordination discipline that runs the rest of the platform: HIS knows the session happened and is exportable; EHR knows how to turn that into an EDI 837. Neither reaches into the other's database.

## CPT capture for outpatient hemodialysis

On the EHR side the charge surfaces at **`/ehr/admin/billing/dialysis-charges`**, where the lifecycle runs **Charge → Claim → Remittance → Payment**. Marcus's outpatient in-center HD session is coded against the standard ESRD hemodialysis procedure set:

| CPT / code | Description | Applied to Marcus's Friday session |
|---|---|---|
| **90935** | Hemodialysis, single physician/QHP evaluation | Considered for the single-evaluation case |
| **90937** | Hemodialysis requiring repeated evaluation(s) with/without substantial revision of the dialysis prescription | **Selected** — Dr. Anand re-evaluated mid-session during the intradialytic hypotension event and the prescription was revised (UF paused, Qb dropped 400→250, then cautiously re-ramped, potassium-bath intervention) |
| **90999** | Unlisted dialysis procedure | Held in reserve for any unlisted component |

The 90937 selection is not cosmetic — it is clinically *true* for this encounter. The emergency at T0+90 forced a documented repeat physician evaluation and a substantial revision of the dialysis prescription, which is exactly the distinction 90937 captures over 90935. The signed ClinicalNote from Dr. Anand and the PDMS prescription-change record are the documentation that backs the higher code, so the charge survives audit.

Each charge is validated against the **CPT fee schedule at `/ehr/admin/billing/fee-schedule`** before it can advance, so the expected allowable is known up front rather than discovered at remittance.

## Charge-edit blocking before the claim leaves

Before EHR will assemble the 837, the charge passes through the **`IChargeEditChecker` charge-edit blocking** gate. This is the platform's pre-submission scrub: missing or mismatched documentation, a procedure-diagnosis pairing that won't pass payer edits, or an absent supporting note holds the charge in a blocked state rather than letting a denial-bound claim go out the door. For Marcus, the diabetic-nephropathy ESKD diagnosis, the AV-fistula access, and the signed 90937-supporting note clear the edits cleanly.

## Building and submitting the EDI 837

A cleared charge becomes a **Claim**, serialized as an **EDI 837** professional claim. Marcus's coverage is **Medicare primary (ESRD entitlement), Medicaid secondary** — the canonical coordination-of-benefits order for an ESKD patient. The 837 routes to Medicare first; the secondary Medicaid claim is sequenced behind the primary remittance.

The platform models the full acknowledgement chain that real payers return, not just the optimistic happy path:

- **EDI 999** — the functional acknowledgement. Confirms the 837 was syntactically accepted (or rejects it for structural errors) before any adjudication. This is the first thing James watches for after submission.
- **EDI 277CA** — the claim acknowledgement. Confirms the payer's claim-level acceptance into adjudication, or surfaces a front-end rejection so the claim can be corrected and resubmitted rather than silently lost.

Honesty note for the demo audience: the platform generates, validates, and tracks the 837/277CA/999 artifacts and the Charge→Claim→Remittance→Payment lifecycle as real EHR Billing surfaces. Live clearinghouse/payer connectivity is a deployment-time integration; what the platform owns end-to-end is the claim *manufacture and tracking* — the part that determines whether a claim is clean.

## Reimbursement tracking and reconciliation

Once Medicare adjudicates, the **Remittance** posts back against the claim and a **Payment** is recorded — closing the loop that began when James clicked Execute in HIS. The `/ehr/admin/billing/dialysis-charges` board shows each of Marcus's charges in its lifecycle state, so a denied or short-paid line is visible as an exception to work rather than a number lost in a batch. Because the expected allowable was pinned against the fee schedule at charge time, an underpayment on the 90937 line stands out immediately for appeal.

The revenue-cycle payoff mirrors the clinical one: the *averted hospitalization* is also the financially sound outcome. An outpatient HD claim — even an intensive 90937 with a documented mid-session intervention — is a fraction of the cost (and the readmission-penalty exposure) of the inpatient admission Marcus nearly had. The same integration event that protected Marcus's continuity of care protected the center's margin.

---

# Analytics Dashboard

Marcus's near-miss is one patient on one Friday. The reason it was *caught* on Wednesday — before the chair sat empty turned into an ED visit — is that the platform watches the population continuously. The analytics surfaces draw on **EHR's `QualityMeasureEvaluator` and population quality at `/ehr/population/quality`**, the **HIS operations dashboard at `/his/today`**, and the denormalized read-model projections that each module maintains from current aggregate state (no event sourcing — these are projections, not replayed logs). Below are the center's headline KPIs, with Marcus's case shown as the single data point that moves them.

| KPI | Center value | Target / benchmark | What it measures here |
|---|---|---|---|
| **Treatment Adherence** | **91.4%** of scheduled sessions completed | ≥ 90% | Marcus's missed Wednesday is one of the 8.6% that didn't complete; the dip is what triggered Beat 2 outreach |
| **Missed Appointment Rate** | **6.8%** | < 8% | The no-show surfaced on `/his/today` as the unfilled chair against the day's schedule |
| **Chair Utilization** | **87.2%** | 85–90% | Marcus's empty Wednesday chair is a direct utilization loss; the Friday re-slot recovered it |
| **Kt/V Compliance** | **88.5%** of patients at Kt/V ≥ 1.2 | ≥ 85% (KDOQI single-pool ≥ 1.2) | Population dialysis-adequacy measure surfaced by `QualityMeasureEvaluator` |
| **Hemoglobin Target Achievement** | **74.3%** in 10–11 g/dL band | 70–80% (avoid over-correction) | Marcus's Hgb 9.6 g/dL sits just below band — an anemia-of-CKD management flag on ESA therapy |
| **Hospitalization Rate** | **1.06** admissions per patient-year | < 1.2/patient-year | Marcus's averted admission is one that *didn't* count against this rate |
| **Readmission Rate (30-day)** | **18.9%** | < 22% | The discharge plan (AVS + RPM cuff + transport) is the lever that keeps Marcus out of this number |

## How the numbers tell Marcus's story

The dashboard is not decoration — every KPI above has a direct line into the scenario beats:

- **Treatment Adherence (91.4%) and Missed Appointment Rate (6.8%)** are the two metrics Marcus personally perturbed. His missed Wednesday session was the inciting event, and the platform's value is that an adherence dip is not just *measured* after the fact — it is *acted on*. The same missed-treatment signal that nudged these KPIs is the integration event that drove the portal push, the scheduler re-slot, and the care-coordination worklist entry.

- **Chair Utilization (87.2%)** is the operational cost of a no-show. James Whitfield sees the unfilled chair on `/his/today` in real time; the metric is the aggregate of exactly those events. Recovering Marcus's Friday slot is a utilization save as much as a clinical one.

- **Kt/V Compliance (88.5%)** is the dialysis-adequacy backbone. Marcus's interrupted Friday session — 3.5 L removed against a 3.8 L goal, ending +0.3 kg over dry weight — is precisely the kind of single-session shortfall that, if it recurred, would erode his individual Kt/V and pull the population number down. One imperfect session is acceptable; a *pattern* is what the measure is built to catch.

- **Hemoglobin Target Achievement (74.3%)** contextualizes Marcus's Hgb 9.6 g/dL, which sits just under the 10–11 g/dL band. He's on epoetin alfa for anemia of CKD; the dashboard flags him as a near-target patient for ESA review, not a crisis — credible nephrology management, not alarmism.

- **Hospitalization Rate (1.06/patient-year) and 30-day Readmission Rate (18.9%)** are the bottom-line outcome metrics — and they are where the entire scenario pays off. Marcus's hospitalization was *avoided*, so it never incremented the admission rate. The discharge bundle — After-Visit Summary to the portal, RPM home BP cuff bound through the HIS device registry, and the adherence + transport plan — is the concrete intervention aimed at keeping him out of the readmission number over the next 30 days.

## Designed-for predictive layer (roadmap-honest)

The KPIs above are computed from current-state read models — real, shipped measurement. The platform is *designed for* a predictive layer on top: missed-treatment risk and hyperkalemia risk scores that would have flagged Marcus's 5-day-gap trajectory before Friday. Today that decision support is delivered through real, deterministic surfaces — `QualityMeasureEvaluator`, `ClinicalSafetyChecker` flagging his pre-HD K⁺ 6.6, and PDMS alarm thresholds firing on the BP nadir. Predictive *scoring* is presented here as the platform's designed-for CDS output and roadmap, not as a shipped predictive product — and the analytics architecture (projections fed by the same integration-event stream) is built to host it.

---

# Compliance and Audit

Every screen Dr. Anand, Rosa Martinez, and Tara Nguyen touched during Marcus's case read or wrote protected health information. On a platform handling ESKD patients across seven apps, the question an auditor — or a regulator, or Marcus himself — will ask is: *who saw what, when, and were they allowed to?* The Dialysis platform answers that through the **Admin/Identity module at `/admin`**, where HIPAA safeguards, the PHI audit pipeline, and the GDPR data-subject-rights workflow all live as real surfaces.

## Audit logging via the FHIR-AuditEvent pipeline

The platform's audit trail is not bolted-on logging — it is a first-class compliance pipeline at **`/admin/hipaa`**. Any CQRS request marked with the **`[PhiAccess]`** attribute emits a **FHIR `AuditEvent`** as it executes. The attribute is declarative: a handler tags itself `[PhiAccess(action, fhirResourceType)]` and the audit pipeline captures the *who* (authenticated subject), *what* (action + FHIR resource type), and *when* automatically, in a standards-based format an external auditor or HIE partner can consume.

Trace Marcus's Friday through that pipeline and every touch is accountable:

- Rosa opening the live session at `/pdms/sessions/:id` and charting the saline bolus on the MAR — audited.
- Dr. Anand reading the longitudinal chart at `/ehr/patients/:id`, signing the ClinicalNote, generating the After-Visit Summary — each a `[PhiAccess]` event.
- The **HIE outbound mapping** of Marcus's Encounter and lab results to US Core FHIR, and the **inbound pull of the outside ED record**, each carrying its own **FHIR `AuditEvent` trail** — so the cross-organization exchange is as traceable as the in-house reads.
- James flipping a document's per-document JavaScript-execution gate on the HIE document board — itself audited via `[PhiAccess]`, because even a viewer setting touches a PHI artifact.

This means the *consent-gated read* in Beat 5 — where HIE's Consent policies decide whether Marcus's outside records may be pulled and reconciled — is not only enforced but recorded: the decision and the access both land in the audit log.

## HIPAA Security-Rule safeguards and PHI encryption

`/admin/hipaa` is more than a log viewer. It exposes the **HIPAA Security-Rule safeguard registry with live checks** — the platform self-reports the state of its administrative, physical, and technical safeguards rather than asserting compliance on a slide. Alongside it runs **column-level PHI encryption** (`IPhiProtector`), so Marcus's identifiers and clinical values are encrypted at the column level, not merely at rest on disk. For James and any visiting auditor, this is the difference between *claiming* HIPAA alignment and *showing* a live safeguard check that either passes or doesn't.

## User tracking, roles, and permission gates

The *who* in every audit event is grounded in **Identity at `/admin/identity`** — users, roles, and the permission catalog that drives the SPA permission gates. Each persona in the cast sees only their surface: Tara works the appointment-request queue and scheduling but not the billing exports; James owns billing exports, the device registry, and HIPAA oversight but isn't charting at the chair; Dr. Anand signs notes and orders. The permission catalog maps Keycloak roles to typed permission strings, and the SPA's `PermissionGate` does a simple `includes(required)` check — so least-privilege is enforced at the screen, and every authorized action is attributable to a named subject in the audit trail.

## Consent management

Consent operates at two layers, and the scenario exercises both:

- **Cross-organization (HIE Consent policies):** before Marcus's outside ED record could be pulled and his medications reconciled in Beat 5, **HIE Consent policies gated the cross-module read**. This is the IHE/FHIR-side consent that decides whether a partner's records may flow in at all — enforced as a query, audited as an access.
- **GDPR consent register:** `/admin/data-protection/consents` holds the lawful-basis and consent record set, distinct from the clinical exchange-consent above.

## GDPR Article 15 / 17 — data-subject rights

The platform treats Marcus not only as a patient but as a data subject with enforceable rights, surfaced at **`/admin/data-protection/data-subject-rights`**:

- **Article 15 (access / export):** a data-subject access request walks every module's `IModuleDataExtractor` to assemble Marcus's complete cross-module record — HIS, EHR, PDMS, HIE — into a portable export.
- **Article 17 (erasure):** an erasure request runs the **approve-and-execute** pipeline. The DPO reviews and approves; `DefaultDataSubjectRightsService` then walks every registered `IPatientEraser` — `EhrPatientEraser`, `HisPatientEraser`, `PdmsPatientEraser`, and the HIE `HieDocumentsPatientEraser` (tombstone + blob purge) — and persists the per-module breakdown to the audit store. Erasure is never a silent delete: it's reviewed, executed across modules, and recorded.

Complementing erasure is the **storage-limitation pipeline at `/hie/admin/documents/retention`** (GDPR Art. 5(1)(e)) — per-document-kind retention policies the DPO sets, with purged documents transitioning to a tombstone state so audit replay sees a *deliberate purge*, not data loss. The two mechanisms are kept distinct: scheduled retention is storage-limitation; the approve-and-execute pipeline is right-to-erasure.

## Clinical documentation as the compliance backbone

None of the above is paperwork for its own sake — clinical documentation *is* the audit substrate. Dr. Anand's signed ClinicalNote, the OrderSet and the `ClinicalSafetyChecker` flag on Marcus's pre-HD K⁺ 6.6, the MAR entries Rosa charted during the emergency, and the After-Visit Summary delivered to the portal are simultaneously the record of care and the evidence trail. The signed note that justifies the 90937 billing code is the same note an auditor reads to confirm the mid-session re-evaluation actually happened. On this platform, good documentation, clean billing, and provable compliance are the same act — and Marcus's averted hospitalization is recorded, attributable, and defensible from the chair to the claim to the audit log.

# Business Outcomes

The Marcus Bell scenario is one averted hospitalization. Multiply it across an ESRD panel and the platform's economics become the story. Every figure below is anchored to a real surface in the *Dialysis* platform and to the canonical clinical timeline; where a number describes a designed-for decision-support output rather than a shipped predictive product, it is flagged.

**Cost reduction — the headline save.** An avoided inpatient admission for a fluid-overload / hyperkalemia ESRD patient runs $14,000–$22,000 (3–4 day stay with telemetry and renal consult). The Friday session that stabilized Marcus to a K⁺ of 4.9 and a closing weight of 78.3 kg cost the facility one in-center HD slot plus ~200 mL of saline and ~20 minutes of charge-nurse time — call it $300 in marginal resource. **That is a 45–70× cost differential on a single event.** For a 120-chair organization where even 2% of treatments today end in an ED diversion, converting half of those to managed outpatient events recovers **$1.5M–$2.4M annually**.

**Time saved — the workflow compression.** The five hand-offs in this case (missed-treatment detection → portal outreach → scheduler re-slot + transport → care-coordination worklist → telehealth check-in) historically meant phone tag across three departments and an average **45–90 minutes of staff coordination per missed treatment**. Because every hand-off rides an integration event over the Transponder outbox and lands on a real worklist (`/ehr/care-coordination/worklist`, `/ehr/appointment-requests`), the same coordination collapses to **under 10 minutes of human touch**. At a 1,000-patient panel with ~8% monthly missed-treatment rate, that is **roughly 80–130 staff-hours saved per month** redirected from telephone reconciliation to bedside care.

**Risk reduction.** The missed Wednesday session never silently disappeared into a no-show line item — the unfilled chair surfaced on `/his/today` and `/pdms/chairs`, and the designed-for hyperkalemia and missed-treatment risk scores rose on `/ehr/patients/:id` (decision-support output, roadmap-grade). Early detection of the 5-day gap is the single largest lever on interdialytic mortality risk. Operationally, the platform also removes silent revenue and compliance risk: charge-edit blocking and EDI 277CA/999 acknowledgement on `/ehr/admin/billing/dialysis-charges` cut clean-claim rework, and every PHI access in the case is captured by the `[PhiAccess]` FHIR AuditEvent pipeline (`/admin/hipaa`), so the audit trail is a byproduct, not a project.

**Staff efficiency.** One charge nurse (Rosa Martinez) ran the entire intradialytic-hypotension response — UF pause, Trendelenburg, 200 mL bolus charted to the MAR, dialysate-temperature drop, potassium-bath maneuver — from a single live screen (`/pdms/sessions/:id`) with a ~2-second vitals ticker, while ClinicianNotification auto-paged Dr. Anand (no manual phone tree). The on-call escalation, normally a 3–4 person scramble, became **a one-nurse, one-page event**. Across a facility this is the difference between a 4:1 and a 6:1 chair-to-nurse coordination ratio during peak shifts.

**Revenue improvement.** The outpatient HD claim (CPT 90935 / 90937 / 90999) flowed HIS billing-export (`/his/admin/billing/exports`, Execute) → EHR EDI 837 with Medicare-primary / Medicaid-secondary validation — same day as treatment, not a week later in a batch. Faster, cleaner claims plus avoided write-offs from denied or downstream inpatient-bundled charges typically lift net collection by **3–6%** and shorten days-in-A/R by **8–15 days**. For a $40M-revenue dialysis organization, that is **$1.2M–$2.4M in incremental realized revenue** with no change in patient volume.

# Clinical Outcomes

**Reduced admissions.** Marcus Bell did not go to the ED and was not admitted. The entire arc — +3.8 kg over dry weight, pre-HD K⁺ 6.6, BP 168/92, mild dyspnea — is the exact substrate that drives same-week ESRD hospitalizations. Detected at the missed-session signal and managed through to a 78.3 kg, K⁺ 4.9 discharge, this is a **1-for-1 averted admission**. Dialysis patients average 1.7–2.0 hospitalizations per year, ~35% of which are volume- or electrolyte-driven and potentially preventable; early gap-detection plus coordinated outreach targets precisely that preventable third.

**Improved adherence.** The inciting cause was not clinical — it was Denise's shift conflict and a transport gap. The platform treated the missed treatment as a care event, not a billing no-show: portal outreach urging Friday confirmation, scheduler re-slotting with transport on `/ehr/appointment-requests`, and a discharge **adherence + transport plan** delivered to Marcus's portal as part of the After-Visit Summary. Closing the transport loop is the highest-yield adherence intervention in in-center HD; addressing the *cause* of the miss, not just re-booking the slot, is what prevents the next one.

**Better patient safety.** Three safety nets fired in sequence and are all real surfaces: the pre-HD K⁺ 6.6 was flagged by EHR ClinicalSafetyChecker against Marcus's HFrEF (EF 40%) and penicillin/contrast allergies; the intradialytic BP nadir of 78/44 with pulse 58 triggered a PDMS TreatmentAlarm in real time; and the HIE pull of an outside ED record (`/hie/fhir-exchange`, consent-gated, FHIR AuditEvent trail) drove a medication reconciliation against his eight home meds before any new order was written. The HFrEF made aggressive UF dangerous — the platform made that danger visible before it became syncope.

**Earlier interventions.** Every clinical action happened earlier than the legacy baseline. The fluid/potassium crisis was anticipated from the gap rather than discovered at crash; the hypotension was caught at the 78/44 nadir and reversed to 104/64 by T0+120 rather than ending the session; and the post-correction BMP (K⁺ 4.9), arriving via SmartConnect HL7 ORU into the Lab module and onto the EHR chart, confirmed safety before discharge rather than after. The designed-for intradialytic-hypotension and hyperkalemia risk scores (decision-support output, roadmap-grade) are built to move these interventions still earlier — from reactive to pre-emptive — on the next iteration.

**The honest fluid math.** Safety was chosen over completeness: against a 3.8 L UF goal, **3.5 L was actually removed** (81.8 → 78.3 kg), leaving Marcus **+0.3 kg over dry weight**. That 0.3 L is the deliberate cost of pausing UF to reverse the hypotension — the right call, charted and signed by Dr. Anand, with the small residual overload folded into the home BP RPM plan rather than forced off the patient in the chair.

# Demo Talking Points

1. **One patient, seven coordinated apps, zero duplicate data entry.** Watch Marcus Bell move from a missed Wednesday session to a Friday save across HIS, EHR, PDMS, SmartConnect, HIE, Lab, and the Patient Portal — all behind one Gateway, all coordinating only through integration events. No interface engine bolted on after the fact; coordination is the architecture.

2. **The miss is detected, not buried.** When Marcus's Wednesday chair stays empty, it surfaces on the HIS ops dashboard (`/his/today`) and the PDMS chair board (`/pdms/chairs`) as a clinical signal — not a no-show line item. The single most preventable driver of ESRD hospitalization is a missed treatment that nobody chased.

3. **A near-admission becomes a managed outpatient event.** Pre-HD K⁺ 6.6, +3.8 kg over dry weight, BP 168/92, dyspnea — the textbook setup for a $14K–$22K admission. The patient walks out at K⁺ 4.9 and 78.3 kg for the cost of one HD slot and a saline bolus. That is the whole investment thesis in one screen.

4. **Live chairside telemetry, ~2-second resolution.** The PDMS session screen (`/pdms/sessions/:id`) streams intradialytic vitals into a TimescaleDB hypertable over a Valkey-backed SignalR backplane. When BP craters to 78/44 with a pulse of 58, the nurse sees it in real time, not on the next manual round.

5. **One nurse runs the emergency.** Rosa Martinez pauses UF, drops Qb to 250, charts a 200 mL saline bolus to the MAR with a vendor pump driver, lowers dialysate temperature, and adjusts the potassium bath — and the system auto-pages Dr. Anand via ClinicianNotification. The 3-person phone-tree scramble is gone.

6. **Escalation is audited per attempt.** PDMS on-call (`/pdms/admin/oncall/{rotation,policies,audit}`) records every paging attempt — who, when, which channel (Twilio SMS / APNs / FCM). When the survey or the malpractice review asks "was the physician notified and when," the answer is a timestamp, not a recollection.

7. **The HFrEF context is what makes this safe.** EHR ClinicalSafetyChecker flags the K⁺ 6.6 against Marcus's EF-40% heart failure and his penicillin / iodinated-contrast allergies. Aggressive ultrafiltration in an EF-40% patient is dangerous — the platform makes the danger visible before the patient nearly syncopes.

8. **Outside records reconciled before a new order is written.** HIE pulls an external ED record (`/hie/fhir-exchange`) under consent gating with a FHIR AuditEvent trail, and the med list is reconciled against Marcus's eight home meds in the EHR. Interoperability that changes the order, not interoperability that fills a folder.

9. **A lab result arrives as data, not as a fax.** The post-correction Basic Metabolic Panel flows in as an HL7 ORU through SmartConnect's Hl7V2ToFhirPipeline, routed by MSH-9 trigger, ClamAV-scanned, normalized to US Core FHIR, and lands on the EHR chart as the confirming K⁺ 4.9. Standards in, structured data out.

10. **Same-day, clean billing.** HIS billing-export (`/his/admin/billing/exports`, Execute) hands the outpatient HD claim (CPT 90935 / 90937 / 90999) to EHR, which files the EDI 837 with Medicare-primary / Medicaid-secondary validation and charge-edit blocking. The 277CA/999 acknowledgements come back. Revenue cycle is in the same system as the chart.

11. **Discharge closes the loop the patient lives in.** Dr. Anand signs the clinical note; the After-Visit Summary, the adherence plan, and the transport plan land in Marcus's Patient Portal (`/portal`). The miss was caused by a transport gap — so the fix addresses transport, not just the next appointment.

12. **RPM enrollment is governed, not improvised.** A home BP cuff is registered and bound to Marcus in the HIS device registry (`/his/admin/devices`), with future readings ingested through a durable command bus. The +0.3 kg residual overload becomes a monitored signal at home, not a number forced off the patient in the chair.

13. **Compliance is a byproduct, not a project.** Every PHI access in this case is captured by the `[PhiAccess]` FHIR AuditEvent pipeline with column-level encryption and a live HIPAA safeguard registry (`/admin/hipaa`); GDPR Art. 15 export and Art. 17 erasure are first-class (`/admin/data-protection`). The audit trail writes itself.

14. **Honest about what's designed-for versus shipped.** The rising hyperkalemia and intradialytic-hypotension risk scores are the platform's designed decision-support output — clinically plausible, roadmap-grade — built on today's real ClinicalSafetyChecker, QualityMeasureEvaluator, and PDMS alarm thresholds. We show you the runway, and we tell you which parts are paved.

15. **This is one event; the architecture is the multiplier.** Modular monolith, central package management, integration-event coordination, schema-per-module databases — the same pattern that saved Marcus scales to a 1,000-patient panel without a re-platform. You are not buying a demo; you are buying the shape that makes the next thousand demos identical.

# MVP Screens

**HIS Operations Dashboard**
- **Primary User:** James Whitfield (Administrator)
- **Route:** `/his/today`
- **Key Widgets:** Live chair-occupancy board, staff roster, inventory levels, billing-export queue summary, today's schedule with the empty Wednesday chair flagged as a no-show signal.
- **Actions Available:** Drill into the unfilled chair, view the day's session schedule, jump to the billing queue, monitor staffing against load.

**PDMS Chair Board**
- **Primary User:** Kevin Osei (Technician) / Rosa Martinez RN
- **Route:** `/pdms/chairs`
- **Key Widgets:** Per-chair status tiles, scheduled-vs-occupied state, the missed-Wednesday chair surfaced as unfilled, shift-level throughput.
- **Actions Available:** Open a chair to start/resume a session, reassign a patient, flag a no-show for care coordination.

**PDMS Live Chairside Session**
- **Primary User:** Rosa Martinez RN / Kevin Osei
- **Route:** `/pdms/sessions/:id`
- **Key Widgets:** Real-time vitals ticker (~2 s, TimescaleDB hypertable over Valkey SignalR backplane) showing the 168/92 → 78/44 → 132/80 BP trajectory and weight 81.8 → 78.3 kg; TreatmentAlarm panel; UF goal / rate / Qb / dialysate K⁺ bath controls; MedicationAdministrationRecord (MAR); IvPumpInfusion vendor driver (Alaris/Baxter/Hospira) for the 200 mL bolus.
- **Actions Available:** Start session, enter pre-HD prescription (UF 3.8 L, Qb 400, K⁺ bath 2.0), pause/reduce UF, drop Qb to 250, chart the saline bolus, lower dialysate temperature, adjust potassium bath, resume at lower rate, close session.

**PDMS On-Call & Escalation**
- **Primary User:** Rosa Martinez RN (triggers) / Dr. Priya Anand (paged)
- **Route:** `/pdms/admin/oncall/rotation`, `/pdms/admin/oncall/policies`, `/pdms/admin/oncall/audit`
- **Key Widgets:** OnCallRotation calendar, EscalationPolicy ladder, AlarmDispatch per-attempt audit (channel, timestamp, acknowledgement).
- **Actions Available:** View active on-call clinician, trigger/track escalation, inspect per-attempt paging audit (Twilio SMS / APNs / FCM).

**PDMS Reporting Templates**
- **Primary User:** Rosa Martinez RN / James Whitfield
- **Route:** `/pdms/admin/reporting/templates`
- **Key Widgets:** Mustache template list (discharge letter, shift report, billing doc), PDF preview.
- **Actions Available:** Generate the session discharge letter / shift report PDF for Marcus's Friday session.

**EHR Longitudinal Patient Chart**
- **Primary User:** Dr. Priya Anand (Nephrologist)
- **Route:** `/ehr/patients/:id`
- **Key Widgets:** Encounter timeline, ClinicalNote editor, Prescription / OrderSet, LabOrder + inbound results (K⁺ 6.6 pre-HD, K⁺ 4.9 post-correction, creatinine 9.8, Hgb 9.6), Diagnosis / Referral, ClinicalSafetyChecker flags (K⁺ 6.6, allergy cross-check), QualityMeasureEvaluator, designed-for risk-score panel (roadmap-grade), medication-reconciliation pane against the eight home meds.
- **Actions Available:** Start Encounter, draft and sign ClinicalNote, place/reconcile orders, review safety flags, reconcile meds against the HIE-pulled outside record, generate the After-Visit Summary, close the Encounter.

**EHR Patient Index**
- **Primary User:** Tara Nguyen (Scheduler) / Dr. Priya Anand
- **Route:** `/ehr/patients`
- **Key Widgets:** Patient search, demographics summary, status badges (ESKD, in-center HD 3×/week).
- **Actions Available:** Search for Marcus Bell, open his chart.

**EHR Care-Coordination Worklist**
- **Primary User:** Tara Nguyen (Scheduler)
- **Route:** `/ehr/care-coordination/worklist`
- **Key Widgets:** Open coordination tasks, the missed-treatment item for Marcus, priority/owner columns, transport-gap flag.
- **Actions Available:** Claim/assign the missed-treatment task, attach the transport plan, mark resolved.

**EHR Appointment Requests**
- **Primary User:** Tara Nguyen (Scheduler)
- **Route:** `/ehr/appointment-requests`
- **Key Widgets:** Portal-originated request queue (PortalAppointmentRequest), Friday re-slot request, transport note.
- **Actions Available:** Approve / decline the Friday re-confirmation, coordinate transport, push confirmation back to the portal.

**EHR Dialysis Billing**
- **Primary User:** James Whitfield (Administrator / revenue-cycle)
- **Route:** `/ehr/admin/billing/dialysis-charges`
- **Key Widgets:** Charge → Claim → Remittance → Payment pipeline, EDI 837 / 277CA / 999 status, charge-edit blocking warnings, Medicare-primary / Medicaid-secondary coordination-of-benefits.
- **Actions Available:** Capture the HD charge (CPT 90935/90937/90999), submit the 837 claim, view acknowledgements, post remittance/payment.

**EHR Fee Schedule**
- **Primary User:** James Whitfield (Administrator)
- **Route:** `/ehr/admin/billing/fee-schedule`
- **Key Widgets:** CPT fee-schedule reference table for the HD codes.
- **Actions Available:** Look up CPT 90935/90937/90999 allowable amounts for the claim.

**SmartConnect Integrations**
- **Primary User:** James Whitfield (Administrator) / interoperability ops
- **Route:** `/smartconnect/integrations`, `/smartconnect/integrations/editor/:id`
- **Key Widgets:** Channel/flow list, Hl7V2ToFhirPipeline status, MSH-9 trigger routing, ClamAV attachment-scan results, message ledger for the inbound BMP ORU.
- **Actions Available:** Inspect the inbound HL7 ORU flow, view the FHIR mapping, reprocess a message, edit the channel in the flow editor.

**HIE FHIR Exchange**
- **Primary User:** Interoperability architect / Dr. Priya Anand (consuming records)
- **Route:** `/hie/fhir-exchange`
- **Key Widgets:** Outbound queue, inbound partner feed, community-records search, consent-policy gate, partner status, FHIR AuditEvent trail.
- **Key Records:** The pulled outside ED record (Query/XCA) under consent gating.
- **Actions Available:** Run a community-records pull, view consent status, surface the outside record for EHR med reconciliation, inspect the AuditEvent trail.

**Patient Portal**
- **Primary User:** Marcus Bell (Patient) / Denise Bell (caregiver-assisted — delegated/caregiver portal is roadmap)
- **Route:** `/portal`
- **Key Widgets:** Secure-message thread, missed-treatment alert urging Friday confirmation, appointment-request form, After-Visit Summary, adherence + transport plan.
- **Actions Available:** Confirm Friday session, send/read secure messages with Dr. Anand (telehealth check-in via secure messaging today; full telehealth platform is roadmap), submit an appointment request, view the AVS and plan.

**HIS Device Registry**
- **Primary User:** James Whitfield (Administrator)
- **Route:** `/his/admin/devices`
- **Key Widgets:** RPM device inventory, register-device form, bind-to-patient control, IngestDeviceReading telemetry feed (durable command bus).
- **Actions Available:** Register the home BP cuff, bind it to Marcus Bell, view/ingest home BP readings for ongoing fluid monitoring.

**HIS Billing Export Queue**
- **Primary User:** James Whitfield (Administrator)
- **Route:** `/his/admin/billing/exports`
- **Key Widgets:** BillingExportJob queue, job status, Execute control.
- **Actions Available:** Execute the export to hand the HD charge off to EHR for 837 filing.

**Admin / HIPAA & Data Protection**
- **Primary User:** James Whitfield (Administrator / compliance)
- **Route:** `/admin/hipaa`, `/admin/data-protection/data-subject-rights`, `/admin/identity`
- **Key Widgets:** Live HIPAA Security-Rule safeguard registry, FHIR AuditEvent pipeline view for `[PhiAccess]` requests, GDPR Art. 15 export / Art. 17 erasure controls, identity users/roles/permission catalog.
- **Actions Available:** Review safeguard checks, inspect the case's PHI-access audit trail, manage roles/permissions, run a data-subject-rights request.

# Investor Value Proposition

**The market is large, captive, and government-anchored.** End-stage renal disease affects roughly 800,000 Americans, of whom about 550,000 are on dialysis; the U.S. spends more than $50 billion a year on ESRD care, the overwhelming majority through Medicare under the unique ESRD entitlement that makes Marcus Bell's Medicare-primary / Medicaid-secondary coverage the rule, not the exception. The in-center hemodialysis market is consolidated into a handful of large operators plus thousands of independent and hospital-affiliated centers, and globally over 4 million people live with ESRD on a trajectory growing 6–7% annually, driven by diabetes and hypertension — Marcus's exact etiology. This is recurring, reimbursed, lifelong care: a patient on 3×/week HD is ~150 billable treatments a year, indefinitely.

**The spend that matters is the hospitalization, and that is precisely what we attack.** Dialysis patients are hospitalized 1.7–2.0 times per year, and a large fraction of those admissions are volume- and electrolyte-driven — the Marcus pattern. Under value-based care models (ETC, CKCC, capitation), the operator now owns that downstream cost. A platform that converts even a modest share of preventable admissions into managed outpatient events is not selling software efficiency; it is selling the difference between losing and making money on a value-based contract. The single averted admission in this scenario is a 45–70× return on the marginal cost of the save.

**Differentiation: coordination is the architecture, not an add-on.** Incumbent dialysis IT is a patchwork — a chairside machine vendor's app, a separate EHR, a bolted-on interface engine, an outsourced billing house, and a portal nobody logs into. *Dialysis* is one modular monolith where the chairside DMS (PDMS), the EHR, revenue cycle (EHR Billing + HIS export), the FHIR/HL7 interoperability layer (HIE + SmartConnect), the patient portal, and the compliance surface (HIPAA AuditEvent pipeline, GDPR Art. 15/17) coordinate exclusively through integration events. The competitive moat is that a missed treatment, a chairside alarm, an outside ED record, a lab result, a claim, and an after-visit summary all live in the same coordinated system — no integration project required to make them talk, because talking is what the platform is.

**Scalability: one shape, a thousand demos.** The same pattern that saved Marcus — schema-per-module databases, central package management, integration-event coordination, a single edge Gateway fronting seven independent SPAs and BFFs — scales from a single center to a 1,000-patient organization without a re-platform. Module hosts are stateless and scale horizontally; the durable command bus and quorum-queue messaging carry the highest-volume telemetry and RPM ingest paths; TimescaleDB absorbs intradialytic vitals at ~2-second resolution. The roadmap is honest and additive: native mobile, delegated caregiver access, a full telehealth platform, and predictive risk *scoring* all extend real surfaces already shipping today — the ClinicalSafetyChecker, the QualityMeasureEvaluator, the PDMS alarm thresholds, and the SmartConnect inference gate. Investors are not buying a feature; they are buying the architecture that makes every future feature land on the same coordinated spine — and the clinical-and-financial proof, in Marcus Bell, that the spine already works.
