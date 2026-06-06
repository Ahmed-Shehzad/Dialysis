import { describe, expect, it } from "vitest";
import { EHR_NAV } from "@/components/layout/AppShell";
import { EHR_ROUTES } from "@/routes/AppRouter";

// Navigation interactivity guard: every top-nav link must resolve to a registered route, otherwise
// clicking it falls through the catch-all <Navigate> and silently redirects (the dead-link bug that
// hid the Follow-up / Requests / Population / Safety pages). Keeps EHR_NAV and EHR_ROUTES in lockstep.
describe("EHR navigation", () => {
  const registered = new Set(EHR_ROUTES.map((route) => route.path));

  it("registers a route for every nav link", () => {
    const dead = EHR_NAV.filter((item) => !registered.has(item.to.replace(/^\//, "")));
    expect(dead.map((item) => item.to)).toEqual([]);
  });

  it("exposes the care-coordination, requests, population, and safety surfaces", () => {
    for (const path of [
      "care-coordination/worklist",
      "appointment-requests",
      "population/quality",
      "safety/surveillance",
    ]) {
      expect(registered.has(path)).toBe(true);
    }
  });
});
