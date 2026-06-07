import { getAlertRule, listAlertEvents, listAlertRules, testAlertRule } from "../api";
import { el, formatDate, errBlock, clear } from "../dom";
import type { RouteContext } from "../router";

export async function renderAlerts(ctx: RouteContext): Promise<void> {
  // Routes: #alerts             → rules list
  //         #alerts/{id}        → rule detail + test trigger
  //         #alert-events       → handled by renderAlertEvents (separate spec)
  const ruleId = ctx.segments[1];
  if (ruleId) {
    await renderRuleDetail(ctx.target, ruleId);
  } else {
    await renderRulesList(ctx.target);
  }
}

export async function renderAlertEvents(ctx: RouteContext): Promise<void> {
  ctx.target.appendChild(el("h2", {}, "Alert events"));
  ctx.target.appendChild(el("p", { class: "muted" }, "Most recent first; max 30 per page."));
  const status = el("p", { class: "muted" }, "Loading…");
  ctx.target.appendChild(status);
  try {
    const events = await listAlertEvents({});
    if (events.length === 0) {
      status.textContent = "No alert events.";
      return;
    }
    status.remove();
    const tbody = el("tbody");
    for (const e of events) {
      const outcomes = (e.actionOutcomes ?? [])
        .map(o => `${o.kind ?? "?"}=${o.succeeded ? "ok" : "fail"}`)
        .join(", ");
      tbody.appendChild(el("tr", {}, [
        el("td", {}, formatDate(e.occurredAtUtc)),
        el("td", {}, String(e.errorType ?? "—")),
        el("td", {}, e.ruleId ? el("a", { href: `#alerts/${encodeURIComponent(e.ruleId)}` }, e.ruleId) : "—"),
        el("td", {}, e.flowId ?? "—"),
        el("td", {}, e.errorDetail ?? "—"),
        el("td", {}, outcomes || "—"),
      ]));
    }
    ctx.target.appendChild(el("table", {}, [
      el("thead", {}, el("tr", {}, [
        el("th", {}, "Occurred"), el("th", {}, "ErrorType"), el("th", {}, "Rule"),
        el("th", {}, "Flow"), el("th", {}, "Detail"), el("th", {}, "Outcomes"),
      ])),
      tbody,
    ]));
  } catch (e) {
    status.remove();
    ctx.target.appendChild(errBlock(`Could not load events: ${(e as Error).message ?? e}`));
  }
}

async function renderRulesList(target: HTMLElement): Promise<void> {
  target.appendChild(el("h2", {}, "Alert rules"));
  target.appendChild(el("p", { class: "muted" }, [
    "Rules and actions are created via REST (",
    el("code", {}, "POST /api/v1/admin/alert-rules"),
    "). This view lists, drills in, and runs the test trigger.",
  ]));
  const status = el("p", { class: "muted" }, "Loading…");
  target.appendChild(status);
  try {
    const rules = await listAlertRules();
    if (rules.length === 0) {
      status.textContent = "No alert rules. POST one to /alert-rules.";
      return;
    }
    status.remove();
    const tbody = el("tbody");
    for (const r of rules) {
      tbody.appendChild(el("tr", {}, [
        el("td", {}, r.name ?? "—"),
        el("td", {}, el("code", {}, r.id)),
        el("td", {}, r.enabled ? "yes" : "no"),
        el("td", {}, r.description ?? "—"),
        el("td", {}, el("a", { href: `#alerts/${encodeURIComponent(r.id)}` }, "open")),
      ]));
    }
    target.appendChild(el("table", {}, [
      el("thead", {}, el("tr", {}, [
        el("th", {}, "Name"), el("th", {}, "Id"), el("th", {}, "Enabled"), el("th", {}, "Description"), el("th", {}, ""),
      ])),
      tbody,
    ]));
  } catch (e) {
    status.remove();
    target.appendChild(errBlock(`Could not load rules: ${(e as Error).message ?? e}`));
  }
}

async function renderRuleDetail(target: HTMLElement, ruleId: string): Promise<void> {
  target.appendChild(el("h2", {}, "Alert rule"));
  target.appendChild(el("p", {}, el("a", { href: "#alerts" }, "← back to rules")));
  const status = el("p", { class: "muted" }, "Loading…");
  target.appendChild(status);
  try {
    const rule = await getAlertRule(ruleId);
    status.remove();
    target.appendChild(el("pre", { class: "json-block" }, JSON.stringify(rule, null, 2)));

    const outBox = el("pre", { class: "json-block" });
    const testBtn = el("button", { type: "button" }, "Run test trigger") as HTMLButtonElement;
    testBtn.addEventListener("click", async () => {
      testBtn.disabled = true;
      clear(outBox);
      outBox.textContent = "Running…";
      try {
        const outcomes = await testAlertRule(ruleId);
        outBox.textContent = JSON.stringify(outcomes, null, 2);
      } catch (e) {
        outBox.textContent = `failed: ${(e as Error).message ?? e}`;
      } finally {
        testBtn.disabled = false;
      }
    });
    target.appendChild(el("h3", {}, "Test trigger"));
    target.appendChild(el("p", {}, testBtn));
    target.appendChild(outBox);
  } catch (e) {
    status.remove();
    target.appendChild(errBlock(`Could not load rule: ${(e as Error).message ?? e}`));
  }
}
