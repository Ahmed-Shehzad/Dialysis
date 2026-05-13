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
        # Variable maps — real backlog (pages 449-454)
        "9e0a1a681980",  # Variable Maps
        "7005e791f922",  # Connector Map
        "85183e12a370",  # Channel Map
        "95e469fce0de",  # Source Map
        "c8d4405adeff",  # Response Map
        "b839f4a9582a",  # Global Channel Map
        "91920d9ae4e3",  # Global Map
        "d58569767feb",  # The Variable Map Lookup Sequence
        # Source/connector map variables surfaced by source connectors — real concept
        "933ef4ca2258",  # Source Map Variables (Database Reader)
        "a10513ed9df1",  # Source Map Variables (File Reader)
        "e24813c8155e",  # Source Map Variables (HTTP Listener)
        "d2df0999352d",  # Source Map Variables (TCP Listener)
        "ecd047bb8c78",  # Source Map Variables (Channel Writer)
        "b0420a814a90",  # Connector Map Variables (File Writer)
        "1355b11537ce",  # Connector Map Variables (HTTP Sender)
        # Code Template Libraries — real backlog
        "7cb9a8890739",  # Edit Code Templates View
        "bd08744ff76d",  # Code Template Library Table
        "1b419734c6fa",  # Edit Library Panel
        "e56460f67110",  # Link Libraries to Channels
        "af35697d05d5",  # Edit Code Template Panel
        "38b667d9e077",  # Code Template Contexts
        "02b976218fe6",  # Use JSDoc in Code Templates
        "fd99234d2268",  # Code Template Tasks
        "21fe3d03e7ea",  # Import Code Templates/Libraries
        "22cab570408f",  # Built-In Code Templates
        # Alerts — real backlog
        "185f2e590ff8",  # Edit Alert View
        # Phase 1 (scheduling + iterator + DSF) IDs were here during Phase 0; now they live in DONE_BATCH.
        # "Remove From Iterators" stays — UI-task wording; iterator engine itself is Done elsewhere.
        "8a5d6f5134bb",  # Remove From Iterators
        # External Script filter / transformer — separate batch
        "85f102539778",  # External Script Filter Rule
        "bbf2966bbe5f",  # External Script Transformer Step
        # OAuth 2.0 token verification — real backlog
        "1a89aa729221",  # OAuth 2.0 Token Verification
        # Response transformers — real concept (engine already supports the slot)
        "d9edaa4d4de8",  # Response Transformers (page 286)
        # Source / destination settings & properties — real backlog
        "073aa9db064d",  # Advanced Settings (source)
        "96c4d761f215",  # Source Settings
        "eb51dc5af4aa",  # Source Connector Properties
        "42d9c9b636e0",  # Destinations Tab
        "701d10fc4cf4",  # Destination Settings
        "02eb96bc6a08",  # Advanced Queue Settings
        "fbfaaf461730",  # Destination Connector Properties
        "03af45160d82",  # Destination Mappings
        "dc0dc437a07e",  # Standard Variables/Templates
        "05f49cb52519",  # Listener Settings
        # Data type backlog (genuine gaps)
        "97f595a57c72",  # Delimited Text Data Type
        "470fdb353236",  # NCPDP Data Type
        "53b493eef0a0",  # Raw Data Type
        "e17335a4fb83",  # Batch Processing
        "0b734799ad47",  # JavaScript Batch Script
        # Channel script editor surface — real backlog (Scripts already Done in matrix)
        "9cd7666923ab",  # Scripts Tab (Done elsewhere) — kept here to avoid accidental N/A
        # Database driver registration — partial; we use DI factories. Real concept, not Mirth dbdrivers.xml
        "bb60791d2158",  # Editing Database Drivers
        # Connector heading rows — chapter-level; SmartConnect implements concrete connectors, mark Done in DONE_BATCH below
    }
)

EV_ONC = "ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md)"
EV_ADMIN = "No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md)"
EV_SERVER_MGR = "No MC Server Manager UI; Kubernetes + config + management API (scope-vs-mirth.md)"
EV_INSTALLER = "Mirth installer / backup-restore UI N/A; .NET deployment model (scope-vs-mirth.md)"
EV_TRAINING = "Training / commercial marketing prose; not SmartConnect implementation backlog"
EV_LEGAL = "PDF front matter; not product behavior"
EV_DEBUGGER = "No MC Administrator Debugger; test flows via HTTP dispatch + ledger + script unit tests (scope-vs-mirth.md)"
EV_VELOCITY = "Mirth Velocity-template substitution chapter; SmartConnect transform stages cover the substitution use cases directly"
EV_MIRTH_CLI = "No Mirth CLI; SmartConnect host CLI + REST API replace it (scope-vs-mirth.md)"
EV_MIRTH_FILES = "Mirth-server configuration / log4j / properties files; SmartConnect uses ASP.NET configuration + Serilog (scope-vs-mirth.md)"
EV_FAQ = "Mirth product FAQ chapter; not a SmartConnect implementation surface"
EV_BEST_PRACTICES = "Mirth channel-development guidance chapter; SmartConnect equivalents are documented in scope-vs-mirth.md + architecture-flowcharts.md"
EV_SECURITY_BP = "Mirth security best-practices chapter; SmartConnect security is platform-driven (TLS at gateway, ASP.NET auth, K8s secrets)"
EV_TROUBLESHOOT = "Mirth troubleshooting chapter (JVM, Java cache, client console); SmartConnect runs on .NET — operator concerns are handled via host logs + telemetry"
EV_UPGRADE = "Mirth upgrade-notes chapter; SmartConnect has no equivalent versioned upgrade matrix"
EV_JS_TUTORIAL = "Mirth's embedded JavaScript / E4X reference chapter; SmartConnect scripts use Jint with standard JS — generic JS tutorial content is not a SmartConnect feature"
EV_SWING_JS_EDITOR = "Java Swing JavaScript editor UI (auto-complete, folding, shortcuts); SmartConnect submits scripts via REST"
EV_ATTACHMENTS = "Attachment system out of scope (no MC-style attachment objects; binary payload + metadata only — scope-vs-mirth.md)"
EV_USER_API = "Mirth Java Server User API (Javadoc); SmartConnect has no Java plugin SPI"
EV_JS_RETURN = "Mirth JavaScript Reader/Writer return-value protocol; SmartConnect inbound + outbound connectors are typed .NET interfaces"
EV_MGMT = "src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/ManagementEndpointExtensions.cs"
EV_EVENTS = "src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/EventsEndpointExtensions.cs"
EV_UI = "src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/wwwroot/smartconnect/index.html"


def swing_edit_view_na(title: str, page: int | None) -> str | None:
    """Java Swing 'Edit Channel/Filter/Transformer/Code-Templates/Alert/GlobalScripts' view chrome (pp 203-321)."""
    if page is None or not (200 <= page <= 325):
        return None
    tl = title.lower()
    patterns = (
        "edit views",
        "edit channel view",
        "summary tab",
        "channel properties",
        "set data types window",
        "select data types",
        "change data type properties",
        "bulk edit mode",
        "set dependencies window",
        "deploy/start dependencies",
        "deploy/start channels",
        "pause/stop/undeploy channels",
        "channel description",
        "source tab",
        "destinations tab",
        "destination table",
        "destination tasks",
        "edit filter / transformer views",
        "edit filter/transformer views",
        "message templates tab",
        "edit data types",
        "specify a template",
        "message trees tab",
        "filter by node name/description",
        "create a rule builder rule or mapper step",
        "create a message builder step",
        "drag-and-drop field values",
        "reference tab",
        "reference list",
        "available variables",
        "create new rules/steps",
        "rule/step table",
        "filter rule properties",
        "transformer step properties",
        "filter tasks",
        "import filter",
        "transformer tasks",
        "import transformer",
        "view generated script",
        "edit global scripts view",
        "edit global scripts",
        "edit alert view",
        "alert error types and regex",
        "alert error categories",
        "alert enabled channels",
        "alert actions",
        "alert variables",
        "edit alert tasks",
        "other tasks",
        "notifications",
    )
    if any(x in tl for x in patterns):
        return EV_ADMIN
    if tl == "navigation":
        return EV_ADMIN
    if tl in ("method 1", "method 1:", "method 2"):
        return EV_ADMIN
    if tl == "protocols" and 315 <= page <= 320:
        return EV_ADMIN
    return None


def mirth_jvm_runtime_na(title: str, page: int | None) -> str | None:
    """Mirth Connect Debugger, Velocity templates, CLI, JS editor (Swing), JS reference tutorial."""
    if page is None:
        return None
    tl = title.lower()
    # JS reference tutorial chapter pp 428-444 (generic JS + E4X + Mirth's JS conventions)
    if 428 <= page <= 444:
        if "message variables" in tl:
            return EV_JS_TUTORIAL
        generic_js = (
            "mirth connect and javascript",
            "about javascript",
            "comments",
            "arrays",
            "operators",
            "arithmetic operators",
            "assignment operators",
            "comparison operators",
            "logical operators",
            "conditional statements",
            "functions",
            "loops and iterations",
            "for loops",
            "for each",
            "while loops",
            "do…while loops",
            "do...while loops",
            "exception handling",
            "using javascript in mirth connect",
            "about e4x",
            "accessing message data with e4x",
            "adding segments to a message",
            "deleting a segment",
            "iterating over message segments",
            "iterating over repeating fields",
            "adding a new repeating field",
            "using java classes",
            "regular expressions",
            "logging with javascript",
            "generating a hash with javascript",
        )
        if any(x in tl for x in generic_js):
            return EV_JS_TUTORIAL
        if tl == "variables" and page <= 430:
            return EV_JS_TUTORIAL
    # Swing JS editor pp 445-449
    if 445 <= page <= 449:
        if "javascript editor" in tl or "remapping editor shortcut" in tl:
            return EV_SWING_JS_EDITOR
    # Attachment functions pp 455-459
    if 455 <= page <= 459:
        if any(x in tl for x in (
            "attachment javascript functions",
            "built-in attachment functions",
            "the attachmentutil class",
            "the attachment object",
        )):
            return EV_ATTACHMENTS
        if tl == "examples" and 458 <= page <= 460:
            return EV_ATTACHMENTS
    # User API (Javadoc) p 459
    if tl == "the user api (javadoc)" or (tl == "javadoc" and 458 <= page <= 460):
        return EV_USER_API
    # Mirth Connect Debugger pp 461-468
    if 461 <= page <= 468:
        if any(x in tl for x in (
            "mirth connect debugger",
            "before you begin",
            "to edit the mcserver.vmoptions",
            "use the debugger",
            "debugger window",
            "debugger menus",
            "coding area",
        )):
            return EV_DEBUGGER
        if tl in ("file menu", "edit menu", "debug menu", "window menu"):
            return EV_DEBUGGER
    # Velocity Variable Replacement pp 469-470
    if 469 <= page <= 470:
        if "velocity variable replacement" in tl:
            return EV_VELOCITY
        if tl in ("basic syntax", "conditional statements", "for loops"):
            return EV_VELOCITY
    # Mirth CLI pp 471-472
    if 471 <= page <= 472:
        if any(x in tl for x in (
            "mirth connect command line interface",
            "running the command line interface",
            "using non-interactive scripting",
        )):
            return EV_MIRTH_CLI
    return None


def mirth_installer_files_na(title: str, page: int | None) -> str | None:
    """Mirth-server configuration files (configuration.properties, dbdrivers.xml, log4j2, etc., pp 478-500)."""
    if page is None or not (476 <= page <= 501):
        return None
    tl = title.lower()
    patterns = (
        "installation directory",
        "application data directory",
        "configuration.properties",
        "extension.properties",
        "keystore.jks",
        "server.id",
        "configuration directory",
        "dbdrivers.xml",
        "log4j2",
        "mirth.properties file",
        "mirth-cli-config.properties",
        "other files and folders",
        "split database connection pools",
        "default supported cipher suites",
        "new default digest algorithm",
        "update the digest algorithm",
    )
    if any(x in tl for x in patterns):
        return EV_MIRTH_FILES
    if tl == "temp" and 479 <= page <= 481:
        return EV_MIRTH_FILES
    return None


def mirth_doc_chapters_na(title: str, page: int | None) -> str | None:
    """FAQ / Channel Best Practices / Security Best Practices / Troubleshooting / Upgrade Guide chapter ranges."""
    if page is None:
        return None
    tl = title.lower()
    # FAQ chapter pp 502-507
    if 502 <= page <= 507:
        return EV_FAQ
    # Channel Development Best Practices and Tips pp 509-518
    if 509 <= page <= 518:
        return EV_BEST_PRACTICES
    # Security Best Practices pp 519-539
    if 519 <= page <= 539:
        return EV_SECURITY_BP
    # Troubleshooting pp 540-552
    if 540 <= page <= 552:
        return EV_TROUBLESHOOT
    # Upgrade Guide + post-upgrade reference material (history) pp 553-602
    if 553 <= page <= 602:
        return EV_UPGRADE
    return None


def js_return_values_na(title: str) -> str | None:
    """Mirth's JavaScript Reader / Writer return-value protocol — N/A in .NET runtime."""
    tl = title.lower()
    if "javascript reader return values" in tl or "javascript writer return values" in tl:
        return EV_JS_RETURN
    return None


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

    # Java Swing edit-view chrome (Edit Channel View, Edit Filter/Transformer Views, Edit Code Templates, Edit Alert, Edit Global Scripts).
    ev = swing_edit_view_na(t, page)
    if ev:
        return ev

    # Mirth-specific JVM runtime surfaces (Debugger, Velocity, CLI, JS editor, JS reference tutorial, attachment functions, User API).
    jvm = mirth_jvm_runtime_na(t, page)
    if jvm:
        return jvm

    # Mirth-server configuration files (configuration.properties, dbdrivers.xml, log4j2, etc.).
    inst = mirth_installer_files_na(t, page)
    if inst:
        return inst

    # FAQ / Channel Best Practices / Security Best Practices / Troubleshooting / Upgrade Guide chapter ranges.
    doc = mirth_doc_chapters_na(t, page)
    if doc:
        return doc

    # JavaScript Reader/Writer return-value protocol.
    js = js_return_values_na(t)
    if js:
        return js

    # Bare "Tasks" / "Navigation" / "Table Columns" rows inside the Swing settings sections (broad page range).
    if page is not None and 170 <= page <= 330:
        if tl in ("tasks", "navigation", "table columns", "settings tasks"):
            return EV_ADMIN

    # Data Pruner Settings Tab UI sub-rows (pp 191-196): Status / Schedule / Tasks (UI, not the actual pruner engine which is Done elsewhere).
    if page is not None and 191 <= page <= 196 and tl in ("status", "schedule"):
        return EV_ADMIN

    # Extensions View settings tab (pp 196-202): no Mirth-style plugin marketplace in SmartConnect.
    if page is not None and 196 <= page <= 202:
        ext_patterns = (
            "extensions view",
            "installed connectors table",
            "installed plugins table",
            "install a new extension",
            "extension tasks",
            "show properties",
        )
        if any(x in tl for x in ext_patterns):
            return EV_ADMIN

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
    # Chapter-heading rows where SmartConnect ships concrete adapters
    "12a2b7f4dfbc": (
        "Source Connectors catalog: HTTP webhook, MLLP, TCP listener, File Reader, Database Reader",
        f"{EV_IN}/Dialysis.SmartConnect.Inbound.AspNetCore; {EV_IN}/Dialysis.SmartConnect.Inbound.Mllp; {EV_IN}/Dialysis.SmartConnect.Inbound.TcpListener; {EV_IN}/Dialysis.SmartConnect.Inbound.FileReader; {EV_IN}/Dialysis.SmartConnect.Inbound.DatabaseReader",
    ),
    "c27980f69bbf": (
        "Destination Connectors catalog: HTTP, File, SMTP, TCP/MLLP, Database, Channel Writer outbound adapters",
        f"{EV_EXT}/HttpOutboundAdapter.cs; {EV_EXT}/FileOutboundAdapter.cs; {EV_EXT}/SmtpOutboundAdapter.cs; {EV_EXT}/TcpOutboundAdapter.cs; {EV_EXT}/DatabaseOutboundAdapter.cs; {EV_EXT}/ChannelWriterOutboundAdapter.cs",
    ),
    "f84c6390b80b": (
        "TCP transmission modes: MLLP / LF-terminated / length-prefixed framing",
        f"{EV_IN}/Dialysis.SmartConnect.Inbound.TcpListener/TcpListenerFrameDecoder.cs; {EV_IN}/Dialysis.SmartConnect.Inbound.Mllp/MllpFrameDecoder.cs",
    ),
    "0f464f24cd13": (
        "Basic TCP transmission mode (LF / length-prefix framing)",
        f"{EV_IN}/Dialysis.SmartConnect.Inbound.TcpListener/TcpListenerFrameDecoder.cs",
    ),
    # Phase 1 — scheduling + iterator + Destination Set Filter
    "0b6bcd03be1a": (
        "Cron schedule (5- or 6-field expression, timezone-aware) via Cronos",
        f"{EV_CORE}/Scheduling/CronSchedule.cs; {EV_CORE}/Scheduling/ScheduleFactory.cs; {EV_ABS}/Scheduling/ScheduleSettings.cs",
    ),
    "56955c34b244": (
        "Time schedule (one or more wall-clock times per day, timezone-aware)",
        f"{EV_CORE}/Scheduling/TimeSchedule.cs; {EV_CORE}/Scheduling/ScheduleFactory.cs",
    ),
    "109fe06b21e3": (
        "Interval schedule (fixed period + optional initial delay)",
        f"{EV_CORE}/Scheduling/IntervalSchedule.cs; {EV_CORE}/Scheduling/ScheduleFactory.cs",
    ),
    "9c0f28631402": (
        "Polling settings: ScheduleSettings (Mode=Interval/Cron/Time) honored by File Reader + Database Reader",
        f"{EV_ABS}/Scheduling/ScheduleSettings.cs; {EV_IN}/Dialysis.SmartConnect.Inbound.FileReader/FileReaderSourceConnector.cs; {EV_IN}/Dialysis.SmartConnect.Inbound.DatabaseReader/DatabaseReaderSourceConnector.cs",
    ),
    "1052c34c408d": (
        "Iterator filter rule: wraps a child route filter and evaluates it per element (HL7/JSON/XML iterable)",
        f"{EV_EXT}/IteratorRouteFilter.cs; {EV_CORE}/Iteration/IterableResolver.cs",
    ),
    "121bd3845f7d": (
        "Iterator transformer step: wraps a child transform stage and runs it per element",
        f"{EV_EXT}/IteratorTransformStage.cs; {EV_CORE}/Iteration/IterableResolver.cs",
    ),
    "7c8e4785a731": (
        "Destination Set Filter transformer step (Jint script with destinationSet.removeAllExcept/remove/removeAll)",
        f"{EV_EXT}/DestinationSetFilterTransformStage.cs; {EV_CORE}/FlowRuntimeEngine.cs",
    ),
    "203b41701989": (
        "Iterator pipeline support: filter rule + transformer step share an IterableResolver",
        f"{EV_EXT}/IteratorRouteFilter.cs; {EV_EXT}/IteratorTransformStage.cs",
    ),
    "90a10bc93515": (
        "Engine support for iterating message parts; the Mirth 'Assign To Iterator' UI task is replaced by the script-form iterator",
        f"{EV_EXT}/IteratorTransformStage.cs",
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
