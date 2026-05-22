import "./styles.css";
// Slice H side-effect import — the DICOM viewer self-registers with the slice I
// viewer registry at module load.
import "./dicom-viewer";
import { mountAuthBar } from "./auth";
import { Router } from "./router";
import { renderFlows } from "./panels/flows";
import { renderMessages } from "./panels/messages";
import { renderAttachments } from "./panels/attachments";
import { renderAlertEvents, renderAlerts } from "./panels/alerts";
import { renderCodeTemplates } from "./panels/code-templates";
import { renderVariableMaps } from "./panels/variable-maps";
import { renderPruner } from "./panels/pruner";
import { renderHealth } from "./panels/health";

function boot(): void {
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
  router
    .add({ name: "flows", match: s => s[0] === "flows", render: renderFlows })
    .add({ name: "messages", match: s => s[0] === "messages", render: renderMessages })
    .add({ name: "attachments", match: s => s[0] === "attachments", render: renderAttachments })
    .add({ name: "alerts", match: s => s[0] === "alerts", render: renderAlerts })
    .add({ name: "alert-events", match: s => s[0] === "alert-events", render: renderAlertEvents })
    .add({ name: "code-templates", match: s => s[0] === "code-templates", render: renderCodeTemplates })
    .add({ name: "variable-maps", match: s => s[0] === "variable-maps", render: renderVariableMaps })
    .add({ name: "pruner", match: s => s[0] === "pruner", render: renderPruner })
    .add({ name: "health", match: s => s[0] === "health", render: renderHealth })
    .setFallback(renderFlows);
  router.start();
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", boot);
} else {
  boot();
}
