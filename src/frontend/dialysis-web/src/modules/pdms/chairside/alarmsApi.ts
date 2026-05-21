import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/apiClient";

/**
 * One active machine alarm as the chairside view will render it. Field names match the
 * PDMS `ActiveAlarmDto` so the wire shape and the UI shape stay in lockstep.
 */
export interface AlarmListItem {
  id: string;
  sessionId?: string | null;
  machineId: string;
  alarmCode: number;
  alarmSource?: string | null;
  alarmPhase?: string | null;
  state: "present" | "inactivating" | "resolved";
  firstObservedUtc: string;
  lastObservedUtc: string;
  acknowledgedUtc?: string | null;
  acknowledgedBy?: string | null;
}

const ACTIVE_ALARMS_KEY = ["pdms", "alarms", "active"] as const;

/**
 * Display label for the strip: a short prefix of the synthetic machine Guid is plenty
 * to disambiguate two chairs at a glance, and we don't have a serial-to-friendly-label
 * mapping yet. When a real Machine catalog exists, swap this for a name lookup.
 */
export const machineLabel = (alarm: AlarmListItem): string =>
  `Machine ${alarm.machineId.slice(0, 8)}`;

const fetchActiveAlarms = async (): Promise<readonly AlarmListItem[]> => {
  const response = await apiClient.get<AlarmListItem[]>("/api/pdms/api/v1.0/alarms");
  return response.data ?? [];
};

/**
 * Polls `GET /api/pdms/api/v1.0/alarms` every 5 seconds for active alarms. Polling is the
 * demo path; a SignalR push channel is the natural follow-up — when it lands the same
 * hook becomes the cache target the hub writes into.
 */
export const useActiveAlarms = () => {
  const query = useQuery({
    queryKey: ACTIVE_ALARMS_KEY,
    queryFn: fetchActiveAlarms,
    refetchInterval: 5_000,
    staleTime: 5_000,
  });
  // Keep the public shape compatible with the previous stub so the consumer doesn't change.
  return { data: query.data ?? ([] as readonly AlarmListItem[]) };
};

const acknowledgeAlarm = async (alarmId: string): Promise<void> => {
  await apiClient.post(`/api/pdms/api/v1.0/alarms/${alarmId}/acknowledge`);
};

/**
 * Records a clinician acknowledgement on a live alarm. The acknowledger is sourced
 * server-side from the authenticated user — the SPA does not choose. The mutation
 * applies an optimistic stamp so the row falls out of the unacknowledged pulse
 * immediately, and invalidates the active-alarms cache on settle so the next poll
 * reconciles with the server.
 */
export const useAcknowledgeAlarm = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: acknowledgeAlarm,
    onMutate: async (alarmId) => {
      await queryClient.cancelQueries({ queryKey: ACTIVE_ALARMS_KEY });
      const previous = queryClient.getQueryData<readonly AlarmListItem[]>(ACTIVE_ALARMS_KEY);
      if (previous) {
        const nowIso = new Date().toISOString();
        queryClient.setQueryData<readonly AlarmListItem[]>(
          ACTIVE_ALARMS_KEY,
          previous.map((a) =>
            a.id === alarmId && !a.acknowledgedUtc
              ? { ...a, acknowledgedUtc: nowIso, acknowledgedBy: "you" }
              : a,
          ),
        );
      }
      return { previous };
    },
    onError: (_err, _alarmId, context) => {
      if (context?.previous) {
        queryClient.setQueryData(ACTIVE_ALARMS_KEY, context.previous);
      }
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: ACTIVE_ALARMS_KEY });
    },
  });
};
