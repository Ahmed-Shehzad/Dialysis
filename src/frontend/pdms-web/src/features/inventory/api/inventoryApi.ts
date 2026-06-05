import { apiClient } from "@/lib/api/apiClient";

export type InventoryItem = {
  id: string;
  medicationCodeSystem: string;
  medicationCode: string;
  medicationDisplay: string;
  lotNumber: string;
  expiryUtc: string;
  onHandUnits: number;
  threshold: number;
  lowStock: boolean;
};

export const fetchInventory = async (lowStockOnly = false): Promise<InventoryItem[]> => {
  const response = await apiClient.get<InventoryItem[]>("/pdms/api/v1.0/inventory", {
    params: { lowStockOnly },
  });
  return response.data ?? [];
};

export const receiveStock = async (
  id: string,
  units: number,
  reason: string,
): Promise<InventoryItem> => {
  const response = await apiClient.post<InventoryItem>(
    `/pdms/api/v1.0/inventory/${id}/receive`,
    { units, reason },
  );
  return response.data;
};

export const adjustStock = async (
  id: string,
  newOnHandUnits: number,
  reason: string,
): Promise<InventoryItem> => {
  const response = await apiClient.post<InventoryItem>(
    `/pdms/api/v1.0/inventory/${id}/adjust`,
    { newOnHandUnits, reason },
  );
  return response.data;
};
