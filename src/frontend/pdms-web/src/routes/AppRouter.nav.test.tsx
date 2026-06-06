import { describe, expect, it } from "vitest";
import { PDMS_NAV } from "@/components/layout/AppShell";
import { PDMS_ROUTES } from "@/routes/AppRouter";

// Navigation interactivity guard: every top-nav link must resolve to a registered route, so a nav
// item can never silently fall through the catch-all redirect (keeps PDMS_NAV and PDMS_ROUTES in lockstep).
describe("PDMS navigation", () => {
  const registered = new Set(PDMS_ROUTES.map((route) => route.path));

  it("registers a route for every nav link", () => {
    const dead = PDMS_NAV.filter((item) => !registered.has(item.to.replace(/^\//, "")));
    expect(dead.map((item) => item.to)).toEqual([]);
  });

  it("exposes the on-call escalation and audit surfaces in the nav", () => {
    const navTargets = new Set(PDMS_NAV.map((item) => item.to));
    expect(navTargets.has("/admin/oncall/policies")).toBe(true);
    expect(navTargets.has("/admin/oncall/audit")).toBe(true);
  });
});
