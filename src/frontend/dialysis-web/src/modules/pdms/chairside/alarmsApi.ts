/**
 * One active machine alarm as the chairside view will render it. Keeps the shape decoupled
 * from `TreatmentAlarm` so the SPA does not need to take a dependency on the PDMS domain
 * before the backend GET endpoint and SignalR push exist.
 */
export interface AlarmListItem {
  id: string;
  machineLabel: string;
  alarmCode: number;
  alarmSource?: string | null;
  alarmPhase?: string | null;
  /** `"present" | "inactivating" | "resolved"` from the PDMS domain. */
  state: "present" | "inactivating" | "resolved";
  firstObservedUtc: string;
  lastObservedUtc: string;
  acknowledgedUtc?: string | null;
  acknowledgedBy?: string | null;
}

/**
 * Active alarms for a single session, or every chair the user can see (no `sessionId` arg).
 *
 * Backend persistence is not in place yet — `TreatmentAlarmConsumer` still logs the
 * translated intent rather than writing the `TreatmentAlarm` aggregate. Until that lands
 * (and the matching `GET /api/pdms/api/v1.0/alarms?activeOnly=true` endpoint), this hook
 * returns an empty list. Swapping in `apiClient.get(...)` is one line when the endpoint
 * exists; the shape above is the contract.
 */
export const useActiveAlarms = (): { data: readonly AlarmListItem[] } => ({ data: [] });
