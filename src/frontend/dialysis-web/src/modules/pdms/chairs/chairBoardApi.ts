import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/apiClient";

/**
 * Wire shape of PDMS's `GET /api/pdms/api/v1.0/chairs`. Each entry is one chair currently
 * occupied by a patient, sourced from the in-memory `ChairOccupancyProjection` that
 * tracks HIS `PatientPlacedInChairIntegrationEvent`.
 */
export interface ChairAssignment {
  chair: string;
  patientId: string;
  placedAtUtc: string;
}

const fetchChairAssignments = async (): Promise<readonly ChairAssignment[]> => {
  const response = await apiClient.get<ChairAssignment[]>("/api/pdms/api/v1.0/chairs");
  return response.data ?? [];
};

/**
 * Polls the chair occupancy snapshot every 10 seconds. The projection is in-memory, so a
 * restart of the PDMS API resets it until the next HIS placement event re-hydrates;
 * polling gives the operator a deterministic refresh cadence without needing SignalR yet.
 */
export const useChairAssignments = () =>
  useQuery({
    queryKey: ["pdms", "chairs"],
    queryFn: fetchChairAssignments,
    refetchInterval: 10_000,
    staleTime: 10_000,
  });
