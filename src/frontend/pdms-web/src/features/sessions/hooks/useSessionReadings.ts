import { useQuery } from "@tanstack/react-query";
import { fetchSessionReadings } from "../api/sessionsApi";
import type { VitalsReading } from "@/features/vitals/api/vitalsApi";

export const sessionReadingsKey = (sessionId: string) =>
  ["sessions", sessionId, "readings"] as const;

export const useSessionReadings = (sessionId: string | undefined) =>
  useQuery<VitalsReading[]>({
    queryKey: sessionReadingsKey(sessionId ?? "missing"),
    queryFn: () => fetchSessionReadings(sessionId as string),
    enabled: Boolean(sessionId),
  });
