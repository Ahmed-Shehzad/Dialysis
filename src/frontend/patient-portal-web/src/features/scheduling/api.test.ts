import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { bookAppointment, DEMO_HIS_PROVIDER_ID } from "./api";

describe("scheduling api", () => {
  afterEach(() => vi.restoreAllMocks());

  it("books via the HIS scheduling endpoint, mapping camelCase input to the PascalCase wire shape", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({
      data: { data: { id: "appt-1" } },
    } as never);

    const id = await bookAppointment({
      patientId: "p-1",
      providerId: DEMO_HIS_PROVIDER_ID,
      slotStartUtc: "2026-06-15T08:00:00Z",
      slotEndUtc: "2026-06-15T12:00:00Z",
    });

    expect(post).toHaveBeenCalledWith("/portal/api/v1.0/scheduling/appointments", {
      PatientId: "p-1",
      ProviderId: DEMO_HIS_PROVIDER_ID,
      SlotStartUtc: "2026-06-15T08:00:00Z",
      SlotEndUtc: "2026-06-15T12:00:00Z",
    });
    // The HATEOAS envelope ({ data: { id } }) is unwrapped down to the bare id.
    expect(id).toBe("appt-1");
  });

  it("propagates a rejected booking so the dialog can surface the humanized error", async () => {
    vi.spyOn(apiClient, "post").mockRejectedValue(new Error("409 slot taken"));

    await expect(
      bookAppointment({
        patientId: "p-1",
        providerId: DEMO_HIS_PROVIDER_ID,
        slotStartUtc: "2026-06-15T08:00:00Z",
        slotEndUtc: "2026-06-15T12:00:00Z",
      }),
    ).rejects.toThrow("409 slot taken");
  });
});
