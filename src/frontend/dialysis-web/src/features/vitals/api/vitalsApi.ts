export type VitalsReading = {
  readingId: string;
  sessionId: string;
  observedAtUtc: string;
  systolicBloodPressure: number;
  diastolicBloodPressure: number;
  heartRateBpm: number;
  arterialPressureMmHg: number;
  venousPressureMmHg: number;
  ultrafiltrationRateMlPerHour: number;
  conductivityMsPerCm: number;
  notes?: string | null;
};

export const VITALS_HUB_URL = "/hubs/vitals";
