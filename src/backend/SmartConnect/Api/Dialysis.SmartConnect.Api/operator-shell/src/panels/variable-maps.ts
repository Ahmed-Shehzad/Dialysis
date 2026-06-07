import { deleteConfigMapValue, getConfigMap, setConfigMapValue, type VariableMapScope } from "../api";
import { el, clear, errBlock } from "../dom";
import type { RouteContext } from "../router";

const SCOPES: VariableMapScope[] = ["Global", "GlobalChannel", "Configuration"];

export async function renderVariableMaps(ctx: RouteContext): Promise<void> {
  ctx.target.appendChild(el("h2", {}, "Variable maps"));
  ctx.target.appendChild(el("p", { class: "muted" }, [
    "Persists via ",
    el("code", {}, "/api/v1/admin/config-map/{scope}"),
    ". GlobalChannel scope optionally takes a flowId query.",
  ]));

  const scopeSelect = el("select", {}) as HTMLSelectElement;
  for (const s of SCOPES) scopeSelect.appendChild(el("option", { value: s }, s));
  const flowIdInput = el("input", { type: "text", size: 40, placeholder: "GlobalChannel only — flow guid" }) as HTMLInputElement;
  const errBox = el("div", { class: "err" });
  const tableBox = el("div");

  const load = async () => {
    clear(errBox);
    clear(tableBox);
    const scope = scopeSelect.value as VariableMapScope;
    const flowId = scope === "GlobalChannel" ? flowIdInput.value.trim() || undefined : undefined;
    try {
      const map = await getConfigMap(scope, flowId);
      tableBox.appendChild(renderTable(scope, flowId, map, load));
    } catch (e) {
      errBox.appendChild(errBlock(`Load failed: ${(e as Error).message ?? e}`));
    }
  };

  scopeSelect.addEventListener("change", load);
  flowIdInput.addEventListener("change", load);

  ctx.target.appendChild(el("p", {}, [
    el("label", {}, ["Scope ", scopeSelect]),
    " ",
    el("label", {}, ["flowId ", flowIdInput]),
    " ",
    el("button", { type: "button", onclick: load }, "Reload"),
  ]));
  ctx.target.appendChild(errBox);
  ctx.target.appendChild(tableBox);
  await load();
}

function renderTable(scope: VariableMapScope, flowId: string | undefined, map: Record<string, string>, reload: () => void): HTMLElement {
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
      tbody,
    ]));
  }

  const newKey = el("input", { type: "text", placeholder: "new key" }) as HTMLInputElement;
  const newVal = el("input", { type: "text", placeholder: "new value", size: 40 }) as HTMLInputElement;
  const newErr = el("span", { class: "err" });
  const addBtn = el("button", { type: "button" }, "Add") as HTMLButtonElement;
  addBtn.addEventListener("click", async () => {
    newErr.textContent = "";
    const k = newKey.value.trim();
    if (!k) { newErr.textContent = "key required"; return; }
    addBtn.disabled = true;
    try {
      await setConfigMapValue(scope, k, newVal.value, flowId);
      newKey.value = "";
      newVal.value = "";
      reload();
    } catch (e) {
      newErr.textContent = (e as Error).message ?? String(e);
    } finally {
      addBtn.disabled = false;
    }
  });
  root.appendChild(el("h3", {}, "Add entry"));
  root.appendChild(el("p", {}, [newKey, " ", newVal, " ", addBtn, " ", newErr]));
  return root;
}

function buildRow(scope: VariableMapScope, flowId: string | undefined, key: string, value: string, reload: () => void): HTMLElement {
  const valCell = el("td", {}, value);
  const actions = el("td");
  let editing = false;

  const renderActions = () => {
    clear(actions);
    if (editing) return;
    const editBtn = el("button", { type: "button" }, "Edit") as HTMLButtonElement;
    const delBtn = el("button", { type: "button" }, "Delete") as HTMLButtonElement;
    editBtn.addEventListener("click", () => {
      editing = true;
      clear(valCell);
      const input = el("input", { type: "text", value, size: 40 }) as HTMLInputElement;
      const saveBtn = el("button", { type: "button" }, "Save") as HTMLButtonElement;
      const cancelBtn = el("button", { type: "button" }, "Cancel") as HTMLButtonElement;
      saveBtn.addEventListener("click", async () => {
        saveBtn.disabled = true;
        try {
          await setConfigMapValue(scope, key, input.value, flowId);
          reload();
        } catch (e) {
          actions.appendChild(el("span", { class: "err" }, ` ${(e as Error).message ?? e}`));
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
        actions.appendChild(el("span", { class: "err" }, ` ${(e as Error).message ?? e}`));
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
