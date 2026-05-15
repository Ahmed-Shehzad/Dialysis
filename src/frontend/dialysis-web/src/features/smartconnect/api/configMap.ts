import { apiClient } from "@/lib/api/apiClient";
import { ADMIN_PREFIX, type VariableMapScopeValue } from "./types";

export type ConfigMapEntry = { key: string; value: string };

const path = (scope: VariableMapScopeValue, key?: string) =>
  key
    ? `${ADMIN_PREFIX}/config-map/${scope}/${encodeURIComponent(key)}`
    : `${ADMIN_PREFIX}/config-map/${scope}`;

export const fetchConfigMap = async (
  scope: VariableMapScopeValue,
  flowId?: string,
): Promise<ConfigMapEntry[]> => {
  const res = await apiClient.get<Record<string, string>>(path(scope), {
    params: flowId ? { flowId } : undefined,
  });
  const dict = res.data ?? {};
  return Object.entries(dict)
    .map(([key, value]) => ({ key, value }))
    .sort((a, b) => a.key.localeCompare(b.key));
};

export const upsertConfigMapEntry = async (
  scope: VariableMapScopeValue,
  entry: ConfigMapEntry,
  flowId?: string,
): Promise<void> => {
  await apiClient.put(
    path(scope, entry.key),
    { value: entry.value },
    { params: flowId ? { flowId } : undefined },
  );
};

export const deleteConfigMapEntry = async (
  scope: VariableMapScopeValue,
  key: string,
  flowId?: string,
): Promise<void> => {
  await apiClient.delete(path(scope, key), {
    params: flowId ? { flowId } : undefined,
  });
};
