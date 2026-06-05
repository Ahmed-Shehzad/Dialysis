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

/** One itemised line of the live cost estimate pushed over the vitals hub. */
export type SessionCostLine = {
  label: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  amount: number;
};

/** Running cost estimate for an in-progress session (SignalR message `"cost"`). */
export type SessionCost = {
  sessionId: string;
  currencyCode: string;
  total: number;
  elapsedMinutes: number;
  asOfUtc: string;
  lines: SessionCostLine[];
};

export const VITALS_HUB_URL = "/portal/hubs/vitals";
