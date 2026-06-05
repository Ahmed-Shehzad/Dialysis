import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, type PrunerOptions } from "./types";

export const fetchPrunerOptions = async (): Promise<PrunerOptions> => {
  const res = await apiClient.get<PrunerOptions>(`${ADMIN_PREFIX}/pruner/options`);
  return res.data;
};
