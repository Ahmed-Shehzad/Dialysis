import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { fetchLabOrder, fetchLabOrdersByPatient } from "./labApi";

describe("labApi", () => {
  afterEach(() => vi.restoreAllMocks());

  it("lists orders through the EHR _x/lab aggregation path and unwraps the envelope", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({
      data: { data: [{ id: "o1", placerOrderNumber: "LAB-1" }], links: [] },
    } as never);

    const rows = await fetchLabOrdersByPatient("p-123");

    expect(get).toHaveBeenCalledWith(
      "/ehr/api/_x/lab/api/v1.0/lab/orders/by-patient/p-123",
      expect.objectContaining({ params: { take: 25 } }),
    );
    expect(rows).toEqual([{ id: "o1", placerOrderNumber: "LAB-1" }]);
  });

  it("tolerates a raw (non-enveloped) array body", async () => {
    vi.spyOn(apiClient, "get").mockResolvedValue({
      data: [{ id: "o2", placerOrderNumber: "LAB-2" }],
    } as never);

    const rows = await fetchLabOrdersByPatient("p-9");
    expect(rows).toEqual([{ id: "o2", placerOrderNumber: "LAB-2" }]);
  });

  it("fetches a single order by id under the aggregation path", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({
      data: { data: { id: "o1", results: [] }, links: [] },
    } as never);

    const order = await fetchLabOrder("o1");

    expect(get).toHaveBeenCalledWith("/ehr/api/_x/lab/api/v1.0/lab/orders/o1");
    expect(order).toEqual({ id: "o1", results: [] });
  });
});
