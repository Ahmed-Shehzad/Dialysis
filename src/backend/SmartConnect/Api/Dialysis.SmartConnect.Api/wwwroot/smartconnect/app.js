"use strict";
(() => {
  // src/dom.ts
  function el(tag, attrs, children) {
    const node = document.createElement(tag);
    if (attrs) {
      for (const [k, v] of Object.entries(attrs)) {
        if (v === null || v === void 0 || v === false) continue;
        if (k.startsWith("on") && typeof v === "function") {
          node.addEventListener(k.slice(2).toLowerCase(), v);
        } else if (k === "class") {
          node.className = String(v);
        } else if (k === "for") {
          node.setAttribute("for", String(v));
        } else {
          node.setAttribute(k, String(v));
        }
      }
    }
    appendChildren(node, children);
    return node;
  }
  function appendChildren(node, children) {
    if (children === void 0 || children === null || children === false) return;
    const list = Array.isArray(children) ? children : [children];
    for (const c of list) {
      if (c === null || c === void 0 || c === false) continue;
      node.appendChild(typeof c === "string" || typeof c === "number" ? document.createTextNode(String(c)) : c);
    }
  }
  function clear(node) {
    while (node.firstChild) node.removeChild(node.firstChild);
  }
  function formatDate(iso) {
    if (!iso) return "\u2014";
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    return d.toISOString().replace("T", " ").replace(/\.\d+Z$/, "Z");
  }
  function errBlock(msg) {
    return el("p", { class: "err" }, msg);
  }

  // src/auth.ts
  var TOKEN_KEY = "smartconnect.token";
  function getToken() {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch {
      return null;
    }
  }
  function setToken(value) {
    try {
      if (value && value.trim() !== "") {
        localStorage.setItem(TOKEN_KEY, value.trim());
      } else {
        localStorage.removeItem(TOKEN_KEY);
      }
    } catch {
    }
  }
  function mountAuthBar(target) {
    const current = getToken() ?? "";
    const input = el("input", {
      type: "password",
      placeholder: "Paste JWT here (optional)",
      size: 40,
      value: current,
      autocomplete: "off",
      "aria-label": "Bearer token"
    });
    const status = el("span", { class: "muted", id: "auth-status" }, current ? "token set" : "no token");
    const save = el("button", {
      type: "button",
      onclick: () => {
        setToken(input.value);
        status.textContent = input.value.trim() ? "token saved" : "token cleared";
      }
    }, "Save");
    const clearBtn = el("button", {
      type: "button",
      onclick: () => {
        input.value = "";
        setToken(null);
        status.textContent = "token cleared";
      }
    }, "Clear");
    target.appendChild(el("div", { class: "auth-bar" }, [
      el("label", { class: "muted" }, "Auth:"),
      input,
      save,
      clearBtn,
      status
    ]));
  }

  // src/router.ts
  var Router = class {
    constructor(target) {
      this.target = target;
    }
    routes = [];
    fallback;
    add(route) {
      this.routes.push(route);
      return this;
    }
    setFallback(handler) {
      this.fallback = handler;
      return this;
    }
    start() {
      window.addEventListener("hashchange", () => this.dispatch());
      this.dispatch();
    }
    async dispatch() {
      const raw = window.location.hash.replace(/^#/, "");
      const segments = raw.split("/").filter(Boolean);
      const ctx = { hash: raw, segments, target: this.target };
      clear(this.target);
      const match = this.routes.find((r) => r.match(segments));
      try {
        if (match) {
          await match.render(ctx);
        } else if (this.fallback) {
          await this.fallback(ctx);
        } else {
          this.target.appendChild(el("p", { class: "err" }, `No panel for #${raw}.`));
        }
      } catch (e) {
        this.target.appendChild(el("p", { class: "err" }, `Panel error: ${e.message ?? e}`));
      }
    }
  };

  // src/api.ts
  function apiUrl(path) {
    return path.startsWith("/") ? path : `/${path}`;
  }
  async function apiFetch(path, init) {
    const headers = new Headers(init?.headers ?? {});
    const token = getToken();
    if (token) headers.set("Authorization", `Bearer ${token}`);
    const res = await fetch(apiUrl(path), { ...init, headers });
    if (!res.ok) {
      const err = new Error(`${res.status} ${res.statusText} for ${path}`);
      err.status = res.status;
      throw err;
    }
    return res;
  }
  async function apiJson(path, init) {
    const res = await apiFetch(path, init);
    if (res.status === 204) return void 0;
    return res.json();
  }
  function listFlows() {
    return apiJson("/smartconnect/v1/admin/flows");
  }
  function listMessages(opts) {
    const params = new URLSearchParams();
    params.set("take", String(opts.take ?? 30));
    if (opts.flowId) params.set("flowId", opts.flowId);
    if (opts.status) params.set("status", opts.status);
    return apiJson(`/smartconnect/v1/admin/messages?${params}`);
  }
  async function reprocessMessage(id) {
    await apiFetch(`/smartconnect/v1/admin/messages/${encodeURIComponent(id)}/reprocess`, { method: "POST" });
  }
  function listAttachmentsForMessage(messageId) {
    return apiJson(`/smartconnect/v1/admin/messages/${encodeURIComponent(messageId)}/attachments`);
  }
  function downloadAttachmentUrl(id) {
    return `/smartconnect/v1/admin/attachments/${encodeURIComponent(id)}`;
  }
  async function deleteAttachment(id) {
    await apiFetch(`/smartconnect/v1/admin/attachments/${encodeURIComponent(id)}`, { method: "DELETE" });
  }
  function listAlertRules() {
    return apiJson("/smartconnect/v1/admin/alert-rules");
  }
  function getAlertRule(id) {
    return apiJson(`/smartconnect/v1/admin/alert-rules/${encodeURIComponent(id)}`);
  }
  function listAlertEvents(opts = {}) {
    const params = new URLSearchParams();
    params.set("take", String(opts.take ?? 30));
    if (opts.ruleId) params.set("ruleId", opts.ruleId);
    return apiJson(`/smartconnect/v1/admin/alert-events?${params}`);
  }
  function testAlertRule(id) {
    return apiJson(`/smartconnect/v1/admin/alert-rules/${encodeURIComponent(id)}/test`, { method: "POST" });
  }
  function listCodeTemplateLibraries() {
    return apiJson("/smartconnect/v1/admin/code-template-libraries");
  }
  function getCodeTemplateLibrary(id) {
    return apiJson(`/smartconnect/v1/admin/code-template-libraries/${encodeURIComponent(id)}`);
  }
  function getConfigMap(scope, flowId) {
    const path = `/smartconnect/v1/admin/config-map/${encodeURIComponent(scope)}`;
    const url = flowId ? `${path}?flowId=${encodeURIComponent(flowId)}` : path;
    return apiJson(url);
  }
  async function setConfigMapValue(scope, key, value, flowId) {
    const path = `/smartconnect/v1/admin/config-map/${encodeURIComponent(scope)}/${encodeURIComponent(key)}`;
    const url = flowId ? `${path}?flowId=${encodeURIComponent(flowId)}` : path;
    await apiFetch(url, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ value })
    });
  }
  async function deleteConfigMapValue(scope, key, flowId) {
    const path = `/smartconnect/v1/admin/config-map/${encodeURIComponent(scope)}/${encodeURIComponent(key)}`;
    const url = flowId ? `${path}?flowId=${encodeURIComponent(flowId)}` : path;
    await apiFetch(url, { method: "DELETE" });
  }
  function getPrunerOptions() {
    return apiJson("/smartconnect/v1/admin/pruner/options");
  }
  function checkHealth() {
    return apiFetch("/health");
  }

  // src/panels/flows.ts
  async function renderFlows(ctx) {
    void formatDate;
    ctx.target.appendChild(el("h2", {}, "Flows"));
    const status = el("p", { class: "muted" }, "Loading\u2026");
    ctx.target.appendChild(status);
    try {
      const flows = await listFlows();
      if (!Array.isArray(flows) || flows.length === 0) {
        status.textContent = "No flows yet. POST a flow to /smartconnect/v1/admin/flows or use import.";
        return;
      }
      status.remove();
      const tbody = el("tbody");
      for (const f of flows) {
        tbody.appendChild(el("tr", {}, [
          el("td", {}, f.name || "\u2014"),
          el("td", {}, el("code", {}, f.id)),
          el("td", {}, f.runtimeState ?? "\u2014"),
          el("td", {}, el("a", {
            href: `/smartconnect/v1/admin/flows/${encodeURIComponent(f.id)}/statistics`,
            target: "_blank",
            rel: "noopener"
          }, "stats"))
        ]));
      }
      ctx.target.appendChild(el("table", {}, [
        el("thead", {}, el("tr", {}, [
          el("th", {}, "Name"),
          el("th", {}, "Id"),
          el("th", {}, "State"),
          el("th", {}, "Statistics")
        ])),
        tbody
      ]));
    } catch (e) {
      status.textContent = "";
      status.className = "err";
      status.appendChild(document.createTextNode(`Could not load flows: ${e.message ?? e}`));
    }
  }

  // src/panels/messages.ts
  async function renderMessages(ctx) {
    ctx.target.appendChild(el("h2", {}, "Message browser & ledger"));
    ctx.target.appendChild(el("p", { class: "muted" }, [
      "Filters: ",
      el("code", {}, "flowId"),
      " (guid), ",
      el("code", {}, "status"),
      " (numeric MessageLedgerStatus). Max 30 per page."
    ]));
    const flowInput = el("input", { type: "text", placeholder: "optional guid", size: 40 });
    const statusInput = el("input", { type: "number", placeholder: "e.g. 0", size: 4 });
    const errBox = el("div", { class: "err" });
    const resultsBox = el("div");
    const load = async () => {
      clear(errBox);
      clear(resultsBox);
      try {
        const flowId = flowInput.value.trim() || void 0;
        const status = statusInput.value.trim() || void 0;
        const opts = {};
        if (flowId) opts.flowId = flowId;
        if (status) opts.status = status;
        const data = await listMessages(opts);
        const items = data.items ?? [];
        if (items.length === 0) {
          errBox.appendChild(document.createTextNode("No messages match."));
          return;
        }
        const tbody = el("tbody");
        for (const m of items) {
          const reproBtn = el("button", { type: "button" }, "Reprocess");
          reproBtn.addEventListener("click", async () => {
            reproBtn.disabled = true;
            try {
              await reprocessMessage(m.id);
              reproBtn.textContent = "started";
            } catch (e) {
              reproBtn.textContent = `failed: ${e.message ?? e}`;
            }
          });
          tbody.appendChild(el("tr", {}, [
            el("td", {}, formatDate(m.createdAtUtc)),
            el("td", {}, String(m.status ?? "\u2014")),
            el("td", {}, el("code", {}, m.correlationId ?? "")),
            el("td", {}, m.detail ?? "\u2014"),
            el("td", {}, [
              el("a", { href: `/smartconnect/v1/admin/messages/${encodeURIComponent(m.id)}`, target: "_blank", rel: "noopener" }, "raw"),
              " ",
              el("a", { href: `#attachments/${encodeURIComponent(m.id)}` }, "attachments"),
              " ",
              reproBtn
            ])
          ]));
        }
        resultsBox.appendChild(el("table", {}, [
          el("thead", {}, el("tr", {}, [
            el("th", {}, "Created"),
            el("th", {}, "Status"),
            el("th", {}, "Correlation"),
            el("th", {}, "Detail"),
            el("th", {}, "Actions")
          ])),
          tbody
        ]));
      } catch (e) {
        errBox.appendChild(errBlock(`Load failed: ${e.message ?? e}`));
      }
    };
    const loadBtn = el("button", { type: "button", onclick: load }, "Load messages");
    ctx.target.appendChild(el("p", {}, [
      el("label", {}, ["flowId ", flowInput]),
      " ",
      el("label", {}, ["status ", statusInput]),
      " ",
      loadBtn
    ]));
    ctx.target.appendChild(errBox);
    ctx.target.appendChild(resultsBox);
  }

  // src/panels/attachments.ts
  async function renderAttachments(ctx) {
    const messageId = ctx.segments[1];
    ctx.target.appendChild(el("h2", {}, "Attachments"));
    if (!messageId) {
      ctx.target.appendChild(el("p", { class: "muted" }, [
        "Open from the message browser \u2014 the URL is ",
        el("code", {}, "#attachments/{messageId}"),
        "."
      ]));
      return;
    }
    ctx.target.appendChild(el("p", { class: "muted" }, ["Message ", el("code", {}, messageId)]));
    const status = el("p", { class: "muted" }, "Loading\u2026");
    ctx.target.appendChild(status);
    try {
      const attachments = await listAttachmentsForMessage(messageId);
      if (attachments.length === 0) {
        status.textContent = "No attachments for this message.";
        return;
      }
      status.remove();
      const tbody = el("tbody");
      for (const a of attachments) {
        const delBtn = el("button", { type: "button" }, "Delete");
        const row = el("tr", {}, [
          el("td", {}, el("code", {}, a.id)),
          el("td", {}, a.mimeType ?? "\u2014"),
          el("td", {}, a.sizeBytes !== void 0 ? String(a.sizeBytes) : "\u2014"),
          el("td", {}, formatDate(a.createdUtc)),
          el("td", {}, [
            el("a", { href: downloadAttachmentUrl(a.id), target: "_blank", rel: "noopener" }, "download"),
            " ",
            delBtn
          ])
        ]);
        delBtn.addEventListener("click", async () => {
          if (!confirm(`Delete attachment ${a.id}?`)) return;
          delBtn.disabled = true;
          try {
            await deleteAttachment(a.id);
            row.remove();
          } catch (e) {
            delBtn.textContent = `failed: ${e.message ?? e}`;
          }
        });
        tbody.appendChild(row);
      }
      ctx.target.appendChild(el("table", {}, [
        el("thead", {}, el("tr", {}, [
          el("th", {}, "Id"),
          el("th", {}, "MIME"),
          el("th", {}, "Size"),
          el("th", {}, "Created"),
          el("th", {}, "Actions")
        ])),
        tbody
      ]));
    } catch (e) {
      status.remove();
      ctx.target.appendChild(errBlock(`Could not load attachments: ${e.message ?? e}`));
    }
  }

  // src/panels/alerts.ts
  async function renderAlerts(ctx) {
    const ruleId = ctx.segments[1];
    if (ruleId) {
      await renderRuleDetail(ctx.target, ruleId);
    } else {
      await renderRulesList(ctx.target);
    }
  }
  async function renderAlertEvents(ctx) {
    ctx.target.appendChild(el("h2", {}, "Alert events"));
    ctx.target.appendChild(el("p", { class: "muted" }, "Most recent first; max 30 per page."));
    const status = el("p", { class: "muted" }, "Loading\u2026");
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
        const outcomes = (e.actionOutcomes ?? []).map((o) => `${o.kind ?? "?"}=${o.succeeded ? "ok" : "fail"}`).join(", ");
        tbody.appendChild(el("tr", {}, [
          el("td", {}, formatDate(e.occurredAtUtc)),
          el("td", {}, String(e.errorType ?? "\u2014")),
          el("td", {}, e.ruleId ? el("a", { href: `#alerts/${encodeURIComponent(e.ruleId)}` }, e.ruleId) : "\u2014"),
          el("td", {}, e.flowId ?? "\u2014"),
          el("td", {}, e.errorDetail ?? "\u2014"),
          el("td", {}, outcomes || "\u2014")
        ]));
      }
      ctx.target.appendChild(el("table", {}, [
        el("thead", {}, el("tr", {}, [
          el("th", {}, "Occurred"),
          el("th", {}, "ErrorType"),
          el("th", {}, "Rule"),
          el("th", {}, "Flow"),
          el("th", {}, "Detail"),
          el("th", {}, "Outcomes")
        ])),
        tbody
      ]));
    } catch (e) {
      status.remove();
      ctx.target.appendChild(errBlock(`Could not load events: ${e.message ?? e}`));
    }
  }
  async function renderRulesList(target) {
    target.appendChild(el("h2", {}, "Alert rules"));
    target.appendChild(el("p", { class: "muted" }, [
      "Rules and actions are created via REST (",
      el("code", {}, "POST /smartconnect/v1/admin/alert-rules"),
      "). This view lists, drills in, and runs the test trigger."
    ]));
    const status = el("p", { class: "muted" }, "Loading\u2026");
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
          el("td", {}, r.name ?? "\u2014"),
          el("td", {}, el("code", {}, r.id)),
          el("td", {}, r.enabled ? "yes" : "no"),
          el("td", {}, r.description ?? "\u2014"),
          el("td", {}, el("a", { href: `#alerts/${encodeURIComponent(r.id)}` }, "open"))
        ]));
      }
      target.appendChild(el("table", {}, [
        el("thead", {}, el("tr", {}, [
          el("th", {}, "Name"),
          el("th", {}, "Id"),
          el("th", {}, "Enabled"),
          el("th", {}, "Description"),
          el("th", {}, "")
        ])),
        tbody
      ]));
    } catch (e) {
      status.remove();
      target.appendChild(errBlock(`Could not load rules: ${e.message ?? e}`));
    }
  }
  async function renderRuleDetail(target, ruleId) {
    target.appendChild(el("h2", {}, "Alert rule"));
    target.appendChild(el("p", {}, el("a", { href: "#alerts" }, "\u2190 back to rules")));
    const status = el("p", { class: "muted" }, "Loading\u2026");
    target.appendChild(status);
    try {
      const rule = await getAlertRule(ruleId);
      status.remove();
      target.appendChild(el("pre", { class: "json-block" }, JSON.stringify(rule, null, 2)));
      const outBox = el("pre", { class: "json-block" });
      const testBtn = el("button", { type: "button" }, "Run test trigger");
      testBtn.addEventListener("click", async () => {
        testBtn.disabled = true;
        clear(outBox);
        outBox.textContent = "Running\u2026";
        try {
          const outcomes = await testAlertRule(ruleId);
          outBox.textContent = JSON.stringify(outcomes, null, 2);
        } catch (e) {
          outBox.textContent = `failed: ${e.message ?? e}`;
        } finally {
          testBtn.disabled = false;
        }
      });
      target.appendChild(el("h3", {}, "Test trigger"));
      target.appendChild(el("p", {}, testBtn));
      target.appendChild(outBox);
    } catch (e) {
      status.remove();
      target.appendChild(errBlock(`Could not load rule: ${e.message ?? e}`));
    }
  }

  // src/panels/code-templates.ts
  async function renderCodeTemplates(ctx) {
    const libId = ctx.segments[1];
    ctx.target.appendChild(el("h2", {}, "Code template libraries"));
    ctx.target.appendChild(el("p", { class: "muted" }, [
      "Read-only viewer. Author libraries via ",
      el("code", {}, "POST /code-template-libraries/import-mirth-xml"),
      " or per-resource CRUD."
    ]));
    const status = el("p", { class: "muted" }, "Loading\u2026");
    ctx.target.appendChild(status);
    try {
      if (libId) {
        const lib = await getCodeTemplateLibrary(libId);
        status.remove();
        ctx.target.appendChild(el("p", {}, el("a", { href: "#code-templates" }, "\u2190 back to libraries")));
        ctx.target.appendChild(renderLibraryDetail(lib));
      } else {
        const libs = await listCodeTemplateLibraries();
        if (libs.length === 0) {
          status.textContent = "No code template libraries.";
          return;
        }
        status.remove();
        const tbody = el("tbody");
        for (const lib of libs) {
          tbody.appendChild(el("tr", {}, [
            el("td", {}, lib.name ?? "\u2014"),
            el("td", {}, el("code", {}, lib.id)),
            el("td", {}, String((lib.templates ?? []).length)),
            el("td", {}, el("a", { href: `#code-templates/${encodeURIComponent(lib.id)}` }, "open"))
          ]));
        }
        ctx.target.appendChild(el("table", {}, [
          el("thead", {}, el("tr", {}, [
            el("th", {}, "Name"),
            el("th", {}, "Id"),
            el("th", {}, "Templates"),
            el("th", {}, "")
          ])),
          tbody
        ]));
      }
    } catch (e) {
      status.remove();
      ctx.target.appendChild(errBlock(`Could not load: ${e.message ?? e}`));
    }
  }
  function renderLibraryDetail(lib) {
    const root = el("div");
    root.appendChild(el("h3", {}, lib.name ?? lib.id));
    if (lib.description) root.appendChild(el("p", { class: "muted" }, lib.description));
    const templates = lib.templates ?? [];
    if (templates.length === 0) {
      root.appendChild(el("p", { class: "muted" }, "No templates in this library."));
      return root;
    }
    for (const t of templates) {
      root.appendChild(el("h4", {}, t.name ?? "(unnamed)"));
      if (t.contextString) root.appendChild(el("p", { class: "muted" }, `Context: ${t.contextString}`));
      root.appendChild(el("pre", { class: "code-block" }, t.body ?? ""));
    }
    return root;
  }

  // src/panels/variable-maps.ts
  var SCOPES = ["Global", "GlobalChannel", "Configuration"];
  async function renderVariableMaps(ctx) {
    ctx.target.appendChild(el("h2", {}, "Variable maps"));
    ctx.target.appendChild(el("p", { class: "muted" }, [
      "Persists via ",
      el("code", {}, "/smartconnect/v1/admin/config-map/{scope}"),
      ". GlobalChannel scope optionally takes a flowId query."
    ]));
    const scopeSelect = el("select", {});
    for (const s of SCOPES) scopeSelect.appendChild(el("option", { value: s }, s));
    const flowIdInput = el("input", { type: "text", size: 40, placeholder: "GlobalChannel only \u2014 flow guid" });
    const errBox = el("div", { class: "err" });
    const tableBox = el("div");
    const load = async () => {
      clear(errBox);
      clear(tableBox);
      const scope = scopeSelect.value;
      const flowId = scope === "GlobalChannel" ? flowIdInput.value.trim() || void 0 : void 0;
      try {
        const map = await getConfigMap(scope, flowId);
        tableBox.appendChild(renderTable(scope, flowId, map, load));
      } catch (e) {
        errBox.appendChild(errBlock(`Load failed: ${e.message ?? e}`));
      }
    };
    scopeSelect.addEventListener("change", load);
    flowIdInput.addEventListener("change", load);
    ctx.target.appendChild(el("p", {}, [
      el("label", {}, ["Scope ", scopeSelect]),
      " ",
      el("label", {}, ["flowId ", flowIdInput]),
      " ",
      el("button", { type: "button", onclick: load }, "Reload")
    ]));
    ctx.target.appendChild(errBox);
    ctx.target.appendChild(tableBox);
    await load();
  }
  function renderTable(scope, flowId, map, reload) {
    const root = el("div");
    const tbody = el("tbody");
    const entries = Object.entries(map).sort(([a], [b]) => a.localeCompare(b));
    if (entries.length === 0) {
      root.appendChild(el("p", { class: "muted" }, "(no entries)"));
    } else {
      for (const [key, value] of entries) {
        tbody.appendChild(buildRow(scope, flowId, key, value, reload));
      }
      root.appendChild(el("table", {}, [
        el("thead", {}, el("tr", {}, [el("th", {}, "Key"), el("th", {}, "Value"), el("th", {}, "Actions")])),
        tbody
      ]));
    }
    const newKey = el("input", { type: "text", placeholder: "new key" });
    const newVal = el("input", { type: "text", placeholder: "new value", size: 40 });
    const newErr = el("span", { class: "err" });
    const addBtn = el("button", { type: "button" }, "Add");
    addBtn.addEventListener("click", async () => {
      newErr.textContent = "";
      const k = newKey.value.trim();
      if (!k) {
        newErr.textContent = "key required";
        return;
      }
      addBtn.disabled = true;
      try {
        await setConfigMapValue(scope, k, newVal.value, flowId);
        newKey.value = "";
        newVal.value = "";
        reload();
      } catch (e) {
        newErr.textContent = e.message ?? String(e);
      } finally {
        addBtn.disabled = false;
      }
    });
    root.appendChild(el("h3", {}, "Add entry"));
    root.appendChild(el("p", {}, [newKey, " ", newVal, " ", addBtn, " ", newErr]));
    return root;
  }
  function buildRow(scope, flowId, key, value, reload) {
    const valCell = el("td", {}, value);
    const actions = el("td");
    let editing = false;
    const renderActions = () => {
      clear(actions);
      if (editing) return;
      const editBtn = el("button", { type: "button" }, "Edit");
      const delBtn = el("button", { type: "button" }, "Delete");
      editBtn.addEventListener("click", () => {
        editing = true;
        clear(valCell);
        const input = el("input", { type: "text", value, size: 40 });
        const saveBtn = el("button", { type: "button" }, "Save");
        const cancelBtn = el("button", { type: "button" }, "Cancel");
        saveBtn.addEventListener("click", async () => {
          saveBtn.disabled = true;
          try {
            await setConfigMapValue(scope, key, input.value, flowId);
            reload();
          } catch (e) {
            actions.appendChild(el("span", { class: "err" }, ` ${e.message ?? e}`));
            saveBtn.disabled = false;
          }
        });
        cancelBtn.addEventListener("click", () => {
          editing = false;
          clear(valCell);
          valCell.appendChild(document.createTextNode(value));
          renderActions();
        });
        valCell.appendChild(input);
        clear(actions);
        actions.appendChild(saveBtn);
        actions.appendChild(document.createTextNode(" "));
        actions.appendChild(cancelBtn);
      });
      delBtn.addEventListener("click", async () => {
        if (!confirm(`Delete ${scope}/${key}?`)) return;
        delBtn.disabled = true;
        try {
          await deleteConfigMapValue(scope, key, flowId);
          reload();
        } catch (e) {
          actions.appendChild(el("span", { class: "err" }, ` ${e.message ?? e}`));
          delBtn.disabled = false;
        }
      });
      actions.appendChild(editBtn);
      actions.appendChild(document.createTextNode(" "));
      actions.appendChild(delBtn);
    };
    renderActions();
    return el("tr", {}, [el("td", {}, el("code", {}, key)), valCell, actions]);
  }

  // src/panels/pruner.ts
  async function renderPruner(ctx) {
    ctx.target.appendChild(el("h2", {}, "Data pruner"));
    ctx.target.appendChild(el("p", { class: "muted" }, [
      "Background sweep configuration. Source: ",
      el("code", {}, "GET /smartconnect/v1/admin/pruner/options"),
      "."
    ]));
    try {
      const opts = await getPrunerOptions();
      ctx.target.appendChild(el("pre", { class: "json-block" }, JSON.stringify(opts, null, 2)));
    } catch (e) {
      ctx.target.appendChild(errBlock(`Could not load pruner options: ${e.message ?? e}`));
    }
  }

  // src/panels/health.ts
  async function renderHealth(ctx) {
    ctx.target.appendChild(el("h2", {}, "Health"));
    ctx.target.appendChild(el("p", {}, [
      el(
        "a",
        { href: "/health", target: "_blank", rel: "noopener" },
        el("code", {}, "GET /health")
      )
    ]));
    const out = el("pre", { class: "json-block" }, "(not checked yet)");
    ctx.target.appendChild(el("p", {}, el("button", {
      type: "button",
      onclick: async () => {
        out.textContent = "Checking\u2026";
        try {
          const res = await checkHealth();
          out.textContent = `${res.status} ${res.statusText}
${await res.text()}`;
        } catch (e) {
          out.textContent = `failed: ${e.message ?? e}`;
        }
      }
    }, "Check now")));
    ctx.target.appendChild(out);
  }

  // src/app.ts
  function boot() {
    const authMount = document.getElementById("auth-bar-mount");
    const panelRoot = document.getElementById("panel-root");
    if (!authMount || !panelRoot) {
      console.error("operator-shell: missing #auth-bar-mount or #panel-root in index.html");
      return;
    }
    mountAuthBar(authMount);
    if (!window.location.hash) {
      window.location.hash = "#flows";
    }
    const router = new Router(panelRoot);
    router.add({ name: "flows", match: (s) => s[0] === "flows", render: renderFlows }).add({ name: "messages", match: (s) => s[0] === "messages", render: renderMessages }).add({ name: "attachments", match: (s) => s[0] === "attachments", render: renderAttachments }).add({ name: "alerts", match: (s) => s[0] === "alerts", render: renderAlerts }).add({ name: "alert-events", match: (s) => s[0] === "alert-events", render: renderAlertEvents }).add({ name: "code-templates", match: (s) => s[0] === "code-templates", render: renderCodeTemplates }).add({ name: "variable-maps", match: (s) => s[0] === "variable-maps", render: renderVariableMaps }).add({ name: "pruner", match: (s) => s[0] === "pruner", render: renderPruner }).add({ name: "health", match: (s) => s[0] === "health", render: renderHealth }).setFallback(renderFlows);
    router.start();
  }
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
//# sourceMappingURL=app.js.map
