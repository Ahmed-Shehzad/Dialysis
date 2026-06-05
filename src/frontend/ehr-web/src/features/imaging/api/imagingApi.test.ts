import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { fetchImagingOrders, orderImagingStudy } from "./imagingApi";

describe("imagingApi", () => {
  afterEach(() => vi.restoreAllMocks());

  it("lists imaging orders under the EHR clinical path", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({
      data: [{ id: "i1", modalityCode: "US", status: "Ordered" }],
    } as never);

    const rows = await fetchImagingOrders("p-1");

    expect(get).toHaveBeenCalledWith(
      "/ehr/api/v1.0/clinical/patients/p-1/imaging-orders",
      expect.objectContaining({ params: { take: 25 } }),
    );
    expect(rows).toEqual([{ id: "i1", modalityCode: "US", status: "Ordered" }]);
  });

  it("posts an imaging order and returns its id", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: { id: "img-1" } } as never);

    const id = await orderImagingStudy({
      patientId: "p-1",
      encounterId: "e-1",
      orderingProviderId: "pr-1",
      modalityCode: "CT",
      bodySiteCode: "Chest",
    });

    expect(post).toHaveBeenCalledWith(
      "/ehr/api/v1.0/clinical/imaging-orders",
      expect.objectContaining({ modalityCode: "CT", bodySiteCode: "Chest" }),
    );
    expect(id).toBe("img-1");
  });
});
