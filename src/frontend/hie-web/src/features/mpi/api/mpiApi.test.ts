import { afterEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api/apiClient";
import { fetchPendingReviews, resolveReview } from "./mpiApi";

describe("mpiApi", () => {
  afterEach(() => vi.restoreAllMocks());

  it("lists pending reviews and unwraps the envelope", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue({
      data: { data: [{ id: "r1", grade: "Probable", score: 0.83 }], links: [] },
    } as never);

    const rows = await fetchPendingReviews();

    expect(get).toHaveBeenCalledWith(
      "/hie/api/v1.0/hie/mpi/reviews",
      expect.objectContaining({ params: { take: 100 } }),
    );
    expect(rows).toEqual([{ id: "r1", grade: "Probable", score: 0.83 }]);
  });

  it("posts a link adjudication to the resolve endpoint", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue({ data: null } as never);

    await resolveReview("r1", true);

    expect(post).toHaveBeenCalledWith("/hie/api/v1.0/hie/mpi/reviews/r1/resolve", {
      link: true,
      note: null,
    });
  });
});
