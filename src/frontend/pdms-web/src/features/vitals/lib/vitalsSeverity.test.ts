import { describe, expect, it } from "vitest";
import type { VitalsReading } from "../api/vitalsApi";
import { classifyVitalsSeverity } from "./vitalsSeverity";

const reading = (overrides: Partial<VitalsReading> = {}): VitalsReading => ({
  readingId: "r1",
  sessionId: "s1",
  observedAtUtc: "2026-06-07T00:00:00Z",
  systolicBloodPressure: 120,
  diastolicBloodPressure: 75,
  heartRateBpm: 76,
  arterialPressureMmHg: -150,
  venousPressureMmHg: 150,
  ultrafiltrationRateMlPerHour: 700,
  conductivityMsPerCm: 13.8,
  ...overrides,
});

describe("classifyVitalsSeverity", () => {
  it("treats normal vitals as moderate", () => {
    expect(classifyVitalsSeverity(reading())).toBe("moderate");
  });

  it("flags hypertensive crisis (high systolic) as critical", () => {
    expect(classifyVitalsSeverity(reading({ systolicBloodPressure: 195 }))).toBe("critical");
  });

  it("flags hypotension (low systolic) as critical", () => {
    expect(classifyVitalsSeverity(reading({ systolicBloodPressure: 80 }))).toBe("critical");
  });

  it("flags tachycardia (high heart rate) as critical", () => {
    expect(classifyVitalsSeverity(reading({ heartRateBpm: 135 }))).toBe("critical");
  });

  it("flags bradycardia (low heart rate) as critical", () => {
    expect(classifyVitalsSeverity(reading({ heartRateBpm: 44 }))).toBe("critical");
  });

  it("flags a venous-pressure circuit alarm as critical", () => {
    expect(classifyVitalsSeverity(reading({ venousPressureMmHg: 280 }))).toBe("critical");
  });

  it("keeps mild deviations within moderate", () => {
    expect(classifyVitalsSeverity(reading({ systolicBloodPressure: 135, heartRateBpm: 88 }))).toBe(
      "moderate",
    );
  });
});
