import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { cancelRequest, fetchMyRequests, requestAppointment } from "./appointmentRequestsApi";

const prefix = "/portal/api/_x/ehr/api/v1.0/portal/appointment-requests";

describe("appointmentRequestsApi", () => {
  afterEach(() => vi.restoreAllMocks());

  it("lists the patient's own requests through the portal _x/ehr aggregation path", async () => {
    const rows = [{ id: "r-1", patientId: "p-1", status: "Pending" }];
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({ data: rows } as never);

    expect(await fetchMyRequests("p-1")).toEqual(rows);
    expect(get).toHaveBeenCalledWith(`${prefix}/patients/p-1`);
  });

  it("degrades a missing body to an empty list", async () => {
    vi.spyOn(apiClient, "get").mockResolvedValue({ data: null } as never);
    expect(await fetchMyRequests("p-1")).toEqual([]);
  });

  it("submits a request with the preferred window and returns the new id", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: { id: "r-7" } } as never);

    const body = {
      reasonText: "Cramping during last session",
      earliestPreferredUtc: "2026-06-15T08:00:00Z",
      latestPreferredUtc: "2026-06-19T16:00:00Z",
    };
    expect(await requestAppointment("p-1", body)).toEqual({ id: "r-7" });
    expect(post).toHaveBeenCalledWith(`${prefix}/patients/p-1`, body);
  });

  it("cancels a still-pending request via the per-request cancel endpoint", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: undefined } as never);

    await cancelRequest("p-1", "r-7");
    expect(post).toHaveBeenCalledWith(`${prefix}/patients/p-1/r-7/cancel`);
  });
});
