import type { VitalsReading } from "../api/vitalsApi";

/**
 * Audio-alarm severity for the chairside monitor.
 * - `critical` — a life-threatening excursion (mirrors the red "alert" tiles in KioskVitals).
 * - `moderate` — everything else, including perfectly normal vitals (the steady monitor beep).
 */
export type VitalsSeverity = "moderate" | "critical";

/**
 * Classifies a single reading. Thresholds intentionally mirror the visual escalation in
 * `KioskVitals`/`VitalsLatestPanel` so the sound the clinician hears matches the tiles they see:
 * any value in an "alert" band makes the whole reading `critical`.
 */
export const classifyVitalsSeverity = (reading: VitalsReading): VitalsSeverity => {
  const isCritical =
    reading.systolicBloodPressure < 90 ||
    reading.systolicBloodPressure > 180 ||
    reading.diastolicBloodPressure < 40 ||
    reading.diastolicBloodPressure > 120 ||
    reading.heartRateBpm < 50 ||
    reading.heartRateBpm > 120 ||
    reading.venousPressureMmHg > 260 ||
    reading.arterialPressureMmHg < -260 ||
    reading.conductivityMsPerCm < 12.5 ||
    reading.conductivityMsPerCm > 15.5;

  return isCritical ? "critical" : "moderate";
};
