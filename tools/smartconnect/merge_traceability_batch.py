#!/usr/bin/env python3
"""Merge batch N/A and Done entries into traceability-overrides.json from guide-toc.json rules."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOC = ROOT / "docs" / "book" / "guide-toc.json"
OVR = ROOT / "docs" / "smartconnect" / "traceability-overrides.json"

# Keep these as explicit In progress (real backlog), do not auto-N/A.
PROTECT_IN_PROGRESS = frozenset(
    {
        "bd7cdd708bef",  # Getting Started — host wiring
        "746ed16e96e7",  # Database requirements / EF
        "1048ab38243e",  # Management HTTP API
        "c304f38f491b",  # Import channel
        "620627148862",  # Web dashboard / operator shell
    }
)

EV_ONC = "ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md)"
EV_ADMIN = "No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md)"
EV_SERVER_MGR = "No MC Server Manager UI; Kubernetes + config + management API (scope-vs-mirth.md)"
EV_INSTALLER = "Mirth installer / backup-restore UI N/A; .NET deployment model (scope-vs-mirth.md)"
EV_TRAINING = "Training / commercial marketing prose; not SmartConnect implementation backlog"
EV_LEGAL = "PDF front matter; not product behavior"
EV_MGMT = "src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/ManagementEndpointExtensions.cs"
EV_EVENTS = "src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/EventsEndpointExtensions.cs"
EV_UI = "src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/wwwroot/smartconnect/index.html"


def swing_admin_ui_na(title: str, page: int | None) -> str | None:
    """MC Administrator chapters with table/view/column/task UI (no Swing parity)."""
    if page is None or not (80 <= page <= 260):
        return None
    tl = title.lower()
    # Do not N/A rows that are covered by Done elsewhere (exact titles).
    if title in ("Events View",):
        return None
    patterns = (
        "alerts view",
        "alerts table",
        "alerts table columns",
        "alerts tasks",
        "channels view",
        "channel table",
        "channel table columns",
        "users view",
        "users table",
        "users table columns",
        "users tasks",
        "add/update a user account",
        "settings view",
        "server settings tab",
        "general settings",
        "channel settings",
        "email settings",
        "notification settings",
        "resources settings",
        "code template libraries",
        "management views",
        "group tasks",
        "edit group details",
        "import group",
        "export group",
        "channel tasks",
        "debug channel",
        "filter by channel name or tag",
        "use drag and drop",
        "get the channel name",
        "assign channels to a group",
        "import channels/groups using drag-and-drop",
        "from the dashboard",
        "from the channels view",
        "server log",
        "connection log",
        "global maps table columns",
        "dashboard tasks",
        "send message",
        "remove all messages",
        "clear statistics",
        "metadata table",
        "add a column to the metadata",
        "metadata table columns",
        "custom metadata columns",
        "message content tab",
        "view message content",
        "message content types",
        "formatting messages",
        "mappings tab",
        "view mappings",
        "mappings table columns",
        "errors tab",
        "view message errors",
        "error content types",
        "attachments tab",
        "attachment table columns",
        "view attachments",
        "text attachment viewer",
        "image attachment viewer",
        "dicom attachment viewer",
        "pdf attachment viewer",
        "message browser tasks",
        "import messages",
        "export results",
        "remove results",
        "export attachment",
        "events table",
        "phi events",
        "event attributes table",
        "event tasks",
        "view all available tags",
        "auto-complete tags",
        "filter by channel tags",
        "filter by channel",
        "filter by partial channel name",
        "filter by multiple criteria",
        "clear filter criteria",
        "system preferences",
        "user preferences",
        "code editor preferences",
        "tags settings tab",
        "tags table",
        "adding a tag",
        "removing a tag",
        "channels table",
        "indeterminate check boxes",
        "database tasks settings tab",
        "database tasks table columns",
        "affected channels table",
        "running a database task",
        "resources table columns",
        "reload resource",
        "directory resource",
        "using resources in channels",
        "restore config",
        "clear all statistics",
    )
    if tl == "navigation" and 95 <= page <= 175:
        return EV_ADMIN
    if any(x in tl for x in patterns):
        return EV_ADMIN
    return None


def classify_na(title: str, page: int | None) -> str | None:
    t = title.strip()
    tl = t.lower()

    if t.startswith("§170"):
        return EV_ONC

    if t.startswith("Required Extensions") or t.startswith("Features that Support the Certification"):
        return EV_ONC

    if t.startswith("Required Actions"):
        return EV_ONC

    if "Cures Certification" in t or t == "Summary" or t == "Required Extensions:":
        return EV_ONC

    if t in ("Single Patient Export", "Multi-Patient Export", "Viewing Exported Attachments"):
        return EV_ONC

    if any(
        x in tl
        for x in (
            "administrator launcher",
            "launch the administrator",
            "download the administrator",
            "mirth connect administrator overview",
            "mirth connect administrator",
            "administrator layout",
            "log on",
            "logging on for the first time",
            "working with tables",
            "monitor views",
            "dashboard view",
            "dashboard table",
            "view messages for a channel",
            "show or hide channel groups",
            "change how tags display",
            "unable to launch mirth",
            "high dpi",
            "administrator settings tab",
            "administrator launcher",
        )
    ):
        return EV_ADMIN

    if re.search(r"\badministrator\b", tl) and page and page < 200 and "api" not in tl:
        if "overview" in tl or "layout" in tl or "fundamentals" in tl:
            return EV_ADMIN

    if any(x in tl for x in ("service tab", "server tab", "database tab", "info tab")):
        return EV_SERVER_MGR

    if "server manager" in tl or ("mirth connect server" in tl and "api" not in tl):
        return EV_SERVER_MGR

    if any(
        x in tl
        for x in (
            "backup current server configuration",
            "change database settings",
            "editing the properties file",
            "restart the mirth connect server",
            "restore server configuration",
            "changing the database type",
        )
    ):
        return EV_INSTALLER

    if "java 9" in tl or "java requirements" in tl:
        return "No JVM channel runtime in SmartConnect host"

    if tl in ("contents", "legal notice"):
        return EV_LEGAL

    if "training" in tl and page and page >= 600:
        return EV_TRAINING

    if t in ("Introduction to Mirth Connect", "About Mirth Connect", "The Healthcare Interoperability Challenge and Solution"):
        return "Product overview; SmartConnect uses separate technical docs"

    if "nextgen connected health" in tl or "mirth connect as open source" in tl:
        return EV_TRAINING

    if any(
        x in tl
        for x in (
            "http authentication settings",
            "choose an authentication type",
            "basic http authentication",
            "digest http authentication",
            "javascript http authentication",
            "custom java class http authentication",
        )
    ):
        return "MC channel HTTP authentication UI; configure auth at reverse proxy or in host HTTP client (scope-vs-mirth.md)"

    if "role-based access control" in tl or "user authorization extension" in tl:
        return "MC commercial RBAC extension; use platform IdP + optional JWT (ManagementSecurityExtensions.cs; scope-vs-mirth.md)"

    if tl == "authentication" and page == 476:
        return "Release-notes topic; not SmartConnect feature matrix"

    if any(
        x in tl
        for x in (
            "encrypt database password",
            "password requirements",
            "keystore password",
            "manually reset a password",
        )
    ):
        return "MC server/keystore password UI; use Kubernetes secrets / ASP.NET configuration (scope-vs-mirth.md)"

    # Swing Administrator: message browser / dashboard / management views (subset).
    sw = swing_admin_ui_na(t, page)
    if sw:
        return sw

    # Remaining connector / protocol names (core SmartConnect does not ship these).
    if any(
        x in tl
        for x in (
            "soap web service",
            "web service listener",
            "web service sender",
            "wsdl",
            "llp listener",
            "socket connector",
            "vm connector",
            "jvm",
            "channel reader",
        )
    ):
        return "Not in core SmartConnect; use HTTP/TCP adapters or host integration (scope-vs-mirth.md)"

    p = page
    if p is not None and 603 <= p <= 619:
        return EV_ONC

    return None


EV_CORE = "src/backend/SmartConnect/Dialysis.SmartConnect.Core"
EV_EXT = f"{EV_CORE}/ExtendedPlugins"
EV_TRANS = f"{EV_CORE}/Transforms"
EV_ABS = "src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction"
EV_IN = "src/backend/SmartConnect/Inbound"
EV_IN_ABS = f"{EV_IN}/Dialysis.SmartConnect.Inbound.Abstractions"
EV_IN_HOST = f"{EV_IN}/Dialysis.SmartConnect.Inbound.Hosting"

DONE_BATCH: dict[str, tuple[str, str]] = {
    "473c249a4370": (
        "Channel route filters: JavaScript (Jint), declarative rule-builder",
        f"{EV_EXT}/JavascriptRouteFilter.cs; {EV_EXT}/RuleBuilderRouteFilter.cs; {EV_CORE}/SmartConnectServiceCollectionExtensions.cs",
    ),
    "b71747f1c8c4": (
        "Transform pipeline: JS, XSLT, JSON path, XML XPath, mapper, message-builder stages",
        f"{EV_EXT}/JavascriptTransformStage.cs; {EV_EXT}/XsltTransformStage.cs; {EV_TRANS}/JsonTransformStage.cs; {EV_TRANS}/XmlTransformStage.cs; {EV_EXT}/MapperTransformStage.cs; {EV_EXT}/MessageBuilderTransformStage.cs",
    ),
    "049e1774caf1": (
        "Route filters registered on IFlowPluginRegistry (JS + rule-builder)",
        f"{EV_CORE}/SmartConnectServiceCollectionExtensions.cs; {EV_CORE}/FlowRuntimeEngine.cs",
    ),
    "055deed2a6dc": (
        "Transform stages on IFlowPluginRegistry; response transform slot on outbound routes",
        f"{EV_CORE}/FlowRuntimeEngine.cs; {EV_ABS}/IntegrationFlowPipelineDefinition.cs",
    ),
    "3439031dbadf": (
        "Filter scripts use payload + metadata on IntegrationMessage (Jint context)",
        f"{EV_EXT}/JavascriptRouteFilter.cs; {EV_ABS}/IntegrationMessage.cs",
    ),
    "0043eaf93157": (
        "Supported filter types: allow-all, JavaScript, rule-builder (JSON rules)",
        f"{EV_CORE}/BuiltInPlugins/AllowAllRouteFilter.cs; {EV_EXT}/JavascriptRouteFilter.cs; {EV_EXT}/RuleBuilderRouteFilter.cs",
    ),
    "dd16e2703a60": (
        "Transform input: IntegrationMessage payload + metadata",
        f"{EV_CORE}/FlowRuntimeEngine.cs; {EV_ABS}/IntegrationMessage.cs",
    ),
    "30de38489db7": (
        "Per-route ResponseTransformStages on pipeline definition",
        f"{EV_ABS}/IntegrationFlowPipelineDefinition.cs; {EV_CORE}/FlowRuntimeEngine.cs",
    ),
    "803fb9390d22": (
        "Transform step kinds: javascript, xslt, json, xml, mapper-transform, message-builder",
        f"{EV_CORE}/SmartConnectServiceCollectionExtensions.cs",
    ),
    "dc7441c576af": (
        "IntegrationMessage: payload bytes, format enum, metadata dictionary, correlation id",
        f"{EV_ABS}/IntegrationMessage.cs; {EV_ABS}/MessageLedgerEntry.cs",
    ),
    "41d81271f349": (
        "Message metadata on IntegrationMessage at runtime; ledger stores payload snapshot only",
        f"{EV_ABS}/IntegrationMessage.cs; src/backend/SmartConnect/Persistence/Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Abstractions/Entities/MessageLedgerEntryEntity.cs",
    ),
    "644e25fd5ab8": (
        "Message body: payload + PayloadFormat; persisted snapshot on ledger rows",
        f"{EV_ABS}/IntegrationMessage.cs; {EV_ABS}/MessageLedgerEntry.cs",
    ),
    # Phase B — conceptual channel / lifecycle rows
    "f08a7e51d0c2": (
        "Channel-style integration flows: IntegrationFlow + pipeline JSON",
        f"{EV_ABS}/IntegrationFlow.cs; {EV_ABS}/IntegrationFlowPipelineDefinition.cs",
    ),
    "cb6cf78e7048": (
        "Flows: source ingress + route filters + outbound routes (Mirth channel analogue)",
        f"{EV_IN_ABS}/ISourceConnector.cs; {EV_CORE}/FlowRuntimeEngine.cs",
    ),
    "8f8485279c1d": (
        "Pipeline components: RouteFilters, OutboundRoutes, Scripts, ResponseTransforms",
        f"{EV_ABS}/IntegrationFlowPipelineDefinition.cs",
    ),
    "5584b7a1846b": (
        "Flow properties: Id, Name, RuntimeState, Pipeline definition",
        f"{EV_ABS}/IntegrationFlow.cs",
    ),
    "6ff60c0decb8": (
        "Source connectors: HTTP, MLLP, FileReader, TcpListener, DatabaseReader + registry",
        f"{EV_IN_HOST}/SourceConnectorHostedService.cs; {EV_IN_ABS}/ISourceConnectorRegistry.cs",
    ),
    "b30529234a69": (
        "Connector model: outbound adapter kinds + per-route parameters JSON",
        f"{EV_ABS}/IntegrationFlowPipelineDefinition.cs; {EV_EXT}/*OutboundAdapter.cs",
    ),
    "5e271d1b17c5": (
        "General outbound route: adapter kind, transform stages, retries, response transforms",
        f"{EV_ABS}/IntegrationFlowPipelineDefinition.cs",
    ),
    "e6c5e2baab30": (
        "Connector-specific settings: OutboundParametersJson + metadata keys",
        f"{EV_ABS}/IntegrationFlowPipelineDefinition.cs; {EV_CORE}/FlowRuntimeEngine.cs",
    ),
    "27fd5020ecec": (
        "Source-side: PreProcessor script, ledger Received, route filters before destinations",
        f"{EV_CORE}/FlowRuntimeEngine.cs; {EV_CORE}/Scripts/ChannelScriptExecutor.cs",
    ),
    "08ca078d887e": (
        "Destination-side: per-route transforms, SendAsync, optional response transforms",
        f"{EV_CORE}/FlowRuntimeEngine.cs",
    ),
    "66290c37fc29": (
        "Final steps: ledger Completed, PostProcessor script, aggregate success",
        f"{EV_CORE}/FlowRuntimeEngine.cs",
    ),
    "906e1dbd0a6a": (
        "Destination chains: OutboundRoutesSequential + Channel Writer in-process chaining",
        f"{EV_ABS}/IntegrationFlowPipelineDefinition.cs; {EV_EXT}/ChannelWriterOutboundAdapter.cs",
    ),
    "a1192781e4bc": (
        "Payload formats + HL7/JSON/XML/datatype helpers and transform stages",
        f"{EV_ABS}/PayloadFormat.cs; {EV_CORE}/DataTypes/Hl7V2Parser.cs; {EV_TRANS}/*.cs",
    ),
    # Phase C — message browser / events API parity (subset)
    "e0f496104a04": (
        "Reprocess single ledger entry: POST /admin/messages/{ledgerEntryId}/reprocess",
        f"{EV_MGMT}; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/MessageBrowserApiTests.cs",
    ),
    "341763e159ff": (
        "Reprocess one entry at a time via API; MC multi-select batch UI N/A",
        f"{EV_MGMT}",
    ),
    "6d1db4c03a71": (
        "Search audit events: GET /admin/events with category, level, flowId, date range, skip/take",
        EV_EVENTS,
    ),
    "6982d7114011": (
        "Event query filters (same query endpoint); no Swing advanced filter UI",
        EV_EVENTS,
    ),
    "18d71fe936b8": (
        "Ledger query filters: flowId, correlationId, dates, status on GET /admin/messages",
        f"{EV_MGMT}; src/backend/SmartConnect/Persistence/Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Abstractions/IMessageLedgerQuery.cs",
    ),
    "736247a4bb0a": (
        "Message list query parameters (subset of MC search options)",
        f"{EV_MGMT}",
    ),
    "eb6a41544dcf": (
        "Operator shell: message list, reprocess, flow links (subset of MC Message Browser tasks)",
        EV_UI,
    ),
    "89e43ed350c9": (
        "Export channel definition: GET /admin/flows/{flowId}/export (JSON)",
        f"{EV_MGMT}",
    ),
    "65722f321eb3": (
        "Export flow group JSON: GET /admin/groups/{groupId}/export",
        "src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/GroupEndpointExtensions.cs",
    ),
    "9897d4a86607": (
        "Create group: POST /admin/groups (JSON body)",
        "src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/GroupEndpointExtensions.cs",
    ),
    "45e19e372f55": (
        "Pruner interval/retention configuration + GET readout (subset of MC Prune Settings)",
        "src/backend/SmartConnect/Dialysis.SmartConnect.Core/DataPrunerHostedService.cs; src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/PrunerEndpointExtensions.cs",
    ),
}

EXTRA_NA: dict[str, tuple[str, str]] = {
    "32cd9bafea30": (
        "N/A",
        "No MC-style attachment objects; binary payload + metadata only (scope-vs-mirth.md)",
    ),
    "d8fb6242d453": (
        "N/A",
        "No separate tmp object; use IntegrationMessage.Metadata or channel scripts (scope-vs-mirth.md)",
    ),
    "d19f2efda9df": (
        "N/A",
        "ASTM E1381 framing not in core; use TCP listener with custom delimiter or host adapter",
    ),
    "8bdffc727247": (
        "N/A",
        "ASTM E1394 datatype plugin not in core; use raw TCP or custom transform",
    ),
    "7453863f9f44": (
        "N/A",
        "MC cluster control plane; scaling/HA is Kubernetes / host concern (scope-vs-mirth.md)",
    ),
    "279545f2881f": (
        "N/A",
        "MC clustering sync UI; not applicable (scope-vs-mirth.md)",
    ),
    "8c24dedf8572": (
        "N/A",
        "No MC Advanced Alerting; use platform metrics/alerting (scope-vs-mirth.md)",
    ),
    "c7ca9d757744": (
        "N/A",
        "No Administrator channel history view; ledger + audit APIs (scope-vs-mirth.md)",
    ),
    "968b1ad9d9ed": (
        "N/A",
        "No MC Message Generator connector in core",
    ),
    "799a49bda76d": (
        "N/A",
        "Commercial support/extensions marketing; not SmartConnect core backlog",
    ),
    "4d7068ec34d9": (
        "N/A",
        "MC Enhancement Bundle; not applicable to SmartConnect core distribution",
    ),
    "0d35ad441457": (
        "N/A",
        "Vendor interoperability bundle; explicit HTTP/TCP/DB adapters in core instead",
    ),
    "ccce3ca312f3": (
        "N/A",
        "Vendor NextGen Results CDR connector; not in core",
    ),
    "857ba44219fd": (
        "N/A",
        "No MC Administrator debug runner; test flows via HTTP dispatch + ledger (scope-vs-mirth.md)",
    ),
    "f1ab76555544": (
        "N/A",
        f"No bulk message import UI; ingest via connectors or POST /flows/{{id}}/messages ({EV_ADMIN})",
    ),
    "eddf45bc29b4": (
        "N/A",
        f"No MC message-browser export-results file; use ledger JSON APIs or custom export ({EV_ADMIN})",
    ),
    "0858c2e885b0": (
        "N/A",
        f"No MC remove-results UI; retention via DataPruner + admin configuration ({EV_ADMIN})",
    ),
    "27e26a88f711": (
        "N/A",
        "No attachment export; attachments out of scope (scope-vs-mirth.md)",
    ),
    "204d09872d15": (
        "N/A",
        "No MC message archive settings UI; retention/pruning only (scope-vs-mirth.md)",
    ),
}


def main() -> int:
    toc = json.loads(TOC.read_text(encoding="utf-8"))
    entries = toc.get("entries") or []
    if not isinstance(entries, list):
        print("bad toc", file=sys.stderr)
        return 1

    data = json.loads(OVR.read_text(encoding="utf-8"))
    by_id: dict = data.setdefault("byId", {})
    if not isinstance(by_id, dict):
        print("bad byId", file=sys.stderr)
        return 1

    if "a4c05bf787c0" in by_id:
        by_id["a4c05bf787c0"]["status"] = "N/A"
    if "6c09f0e130ae" in by_id:
        by_id["6c09f0e130ae"]["status"] = "N/A"

    added_na = updated_na = added_done = skipped = 0
    for e in entries:
        if not isinstance(e, dict) or "id" not in e:
            continue
        eid = str(e["id"])
        title = str(e.get("title", ""))
        page = e.get("page")
        p = int(page) if isinstance(page, int) or (isinstance(page, str) and page.isdigit()) else None

        cur = by_id.get(eid)
        if isinstance(cur, dict) and cur.get("status") == "Done":
            continue

        if eid in EXTRA_NA:
            mapping, evid = EXTRA_NA[eid]
            by_id[eid] = {"mapping": mapping, "status": "N/A", "evidence": evid}
            added_na += 1
            continue

        if eid in DONE_BATCH and eid not in PROTECT_IN_PROGRESS:
            mapping, evid = DONE_BATCH[eid]
            if not isinstance(cur, dict) or cur.get("status") != "Done":
                by_id[eid] = {
                    "mapping": mapping,
                    "status": "Done",
                    "evidence": evid
                    if evid.startswith("src/")
                    else f"{EV_CORE}/{evid}",
                }
                added_done += 1
            continue

        reason = classify_na(title, p)
        if not reason:
            skipped += 1
            continue
        if eid in PROTECT_IN_PROGRESS:
            continue

        if not isinstance(cur, dict):
            by_id[eid] = {"mapping": "N/A", "status": "N/A", "evidence": reason}
            added_na += 1
        elif cur.get("status") == "N/A":
            continue
        elif cur.get("status") == "In progress":
            if cur.get("mapping") == "N/A" or "N/A" in str(cur.get("mapping", "")):
                by_id[eid] = {"mapping": "N/A", "status": "N/A", "evidence": reason}
                updated_na += 1
            elif str(cur.get("mapping", "")).startswith("—") or cur.get("mapping") in (None, ""):
                by_id[eid] = {"mapping": "N/A", "status": "N/A", "evidence": reason}
                updated_na += 1
            else:
                pass

    OVR.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"wrote {OVR} added_na={added_na} updated_na={updated_na} added_done={added_done}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
