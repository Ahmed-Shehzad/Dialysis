# SmartConnect — user guide traceability matrix

<!-- gen: generated from docs/book/guide-toc.json + traceability-overrides.json; do not hand-edit data rows without updating overrides or re-running extract -->

**Source of truth:** [mirth-connect-user-guide.pdf](../book/mirth-connect-user-guide.pdf) (Git LFS). Each row is anchored to a **PDF outline** entry (`id` = stable hash of title, level, page). Thematic groupings alone are **not** sufficient for requirements coverage.

| PDF id | Title | Page | SmartConnect mapping | Status | Evidence |
|--------|-------|------|----------------------|--------|----------|
| `0fa53d8d19f6` | Contents | 3 | N/A | N/A | PDF front matter; not product behavior |
| `b53f48a4b0a4` | Legal Notice | 19 | N/A | N/A | PDF front matter; not product behavior |
| `e0c81a8ab836` | Introduction to Mirth Connect | 20 | N/A | N/A | Product overview; SmartConnect uses separate technical docs |
| `edcdb2082802` | The NextGen Connected Health Solutions Mission | 20 | N/A | N/A | Training / commercial marketing prose; not SmartConnect implementation backlog |
| `b1df0f9bba3b` | About Mirth Connect | 20 | N/A | N/A | Product overview; SmartConnect uses separate technical docs |
| `15e6ad86afec` | The Healthcare Interoperability Challenge and Solution | 21 | N/A | N/A | Product overview; SmartConnect uses separate technical docs |
| `f7cd3c783cac` | Mirth Connect as Open Source Software | 22 | N/A | N/A | Training / commercial marketing prose; not SmartConnect implementation backlog |
| `bd7cdd708bef` | Getting Started with Mirth Connect | 23 | ASP.NET Core host + SmartConnect wiring | In progress | src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/Program.cs |
| `e338fa38150f` | Mirth Connect System Requirements | 23 | Cross-stack requirements differ from MC installer | N/A | SmartConnect is .NET/K8s-hosted; compare with deployment docs |
| `b81be5d5dc05` | Java Requirements | 24 | N/A | N/A | No JVM channel runtime in SmartConnect host |
| `746ed16e96e7` | Database Requirements | 24 | EF Core persistence for flows + ledger | In progress | src/backend/SmartConnect/Persistence/Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Abstractions/SmartConnectDbContext.cs |
| `27d6f22a63a4` | Install Mirth Connect Using the Mirth Connect Installer | 25 | N/A | N/A | Different distribution model (.NET deploy vs MC installer) |
| `049903eb867c` | Install Mirth Connect Using the Command-Line Installer | 36 | N/A | N/A | Different distribution model (.NET deploy vs MC CLI installer) |
| `a4c05bf787c0` | The Mirth Connect Server Manager | 44 | N/A | N/A | No MC Server Manager UI; operations via K8s + management API |
| `b36678b2c6be` | Service Tab | 44 | N/A | N/A | No MC Server Manager UI; Kubernetes + config + management API (scope-vs-mirth.md) |
| `a08bc1b6f966` | Server Tab | 44 | N/A | N/A | No MC Server Manager UI; Kubernetes + config + management API (scope-vs-mirth.md) |
| `4352e3fdcdcd` | Database Tab | 46 | N/A | N/A | No MC Server Manager UI; Kubernetes + config + management API (scope-vs-mirth.md) |
| `df9dee6092c9` | Info Tab | 47 | N/A | N/A | No MC Server Manager UI; Kubernetes + config + management API (scope-vs-mirth.md) |
| `620627148862` | The Web Dashboard | 47 | Operator shell: flows table, message browser (filter + list + reprocess), pruner readout, API links | In progress | src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/wwwroot/smartconnect/index.html |
| `94b9bfb26a81` | Changing the Database Type | 49 | N/A | N/A | Mirth installer / backup-restore UI N/A; .NET deployment model (scope-vs-mirth.md) |
| `25e220f01b32` | Backup Current Server Configuration | 49 | N/A | N/A | Mirth installer / backup-restore UI N/A; .NET deployment model (scope-vs-mirth.md) |
| `2e3358acf9d7` | Change Database Settings | 50 | N/A | N/A | Mirth installer / backup-restore UI N/A; .NET deployment model (scope-vs-mirth.md) |
| `f5ab12768c17` | Editing the Properties File Directly for Windows, Linux, and Mac | 52 | N/A | N/A | Mirth installer / backup-restore UI N/A; .NET deployment model (scope-vs-mirth.md) |
| `a23cad09bec9` | Restart the Mirth Connect Server | 53 | N/A | N/A | No MC Server Manager UI; Kubernetes + config + management API (scope-vs-mirth.md) |
| `3cc9c1d75a71` | Restore Server Configuration | 53 | N/A | N/A | Mirth installer / backup-restore UI N/A; .NET deployment model (scope-vs-mirth.md) |
| `6faed7fb5f75` | Using Java 9 or greater | 55 | N/A | N/A | No JVM channel runtime in SmartConnect host |
| `f4d7f4f95db0` | Mirth Connect Administrator Overview | 55 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `504a7e83bca6` | Download the Administrator Launcher | 55 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `b837c994bf25` | Launch the Administrator | 56 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `6fd63ec29458` | Log On | 57 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `50e35a801b57` | Logging on for the First Time | 58 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `f08a7e51d0c2` | The Fundamentals of Mirth Connect | 60 | — | In progress | — |
| `cb6cf78e7048` | About Channels and Connectors | 60 | — | In progress | — |
| `8f8485279c1d` | Channel Components | 60 | — | In progress | — |
| `5584b7a1846b` | General Channel Properties | 61 | — | In progress | — |
| `6ff60c0decb8` | Source Connector | 61 | — | In progress | — |
| `40c45dd1279d` | Destination Connectors | 62 | Destination connectors: HTTP, File, SMTP, TCP/MLLP, Database, Channel Writer | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/*.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/SmartConnectServiceCollectionExtensions.cs |
| `5788dfaf6043` | Channel Scripts | 62 | Channel scripts: PreProcessor/PostProcessor (Jint), Deploy/Undeploy on start/stop API | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/Scripts/ChannelScriptExecutor.cs; FlowScriptsDefinition; ManagementEndpointExtensions start/stop; ChannelScriptExecutorTests |
| `b30529234a69` | Connector Components | 63 | — | In progress | — |
| `5e271d1b17c5` | General Connector Properties | 63 | — | In progress | — |
| `e6c5e2baab30` | Connector-Specific Properties | 63 | — | In progress | — |
| `473c249a4370` | Filter | 65 | Channel route filters: JavaScript (Jint), declarative rule-builder | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/JavascriptRouteFilter.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/RuleBuilderRouteFilter.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/SmartConnectServiceCollectionExtensions.cs |
| `b71747f1c8c4` | Transformer | 65 | Transform pipeline: JS, XSLT, JSON path, XML XPath, mapper, message-builder stages | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/JavascriptTransformStage.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/XsltTransformStage.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/Transforms/JsonTransformStage.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/Transforms/XmlTransformStage.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/MapperTransformStage.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/MessageBuilderTransformStage.cs |
| `dc7441c576af` | About Message Data | 65 | IntegrationMessage: payload bytes, format enum, metadata dictionary, correlation id | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationMessage.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/MessageLedgerEntry.cs |
| `41d81271f349` | Message Metadata | 66 | Message metadata: string key-value map on IntegrationMessage during pipeline execution; ledger rows store payload snapshot (metadata not persisted on ledger yet) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationMessage.cs; MessageLedgerEntryEntity (payload only) |
| `644e25fd5ab8` | Message Content | 67 | Message body: payload + PayloadFormat; persisted snapshot on ledger rows | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationMessage.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/MessageLedgerEntry.cs |
| `32cd9bafea30` | Message Attachments | 68 | N/A | N/A | No MC-style attachment objects; binary payload + metadata only (scope-vs-mirth.md) |
| `664b9143f91d` | The Message Processing Lifecycle | 68 | FlowRuntimeEngine: route filters, parallel or sequential outbound routes, response payload + response transforms | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/FlowRuntimeEngineTests.cs |
| `27fd5020ecec` | Source Processing Steps | 69 | — | In progress | — |
| `08ca078d887e` | Destination Processing Steps | 70 | — | In progress | — |
| `66290c37fc29` | Final Processing Steps | 70 | — | In progress | — |
| `906e1dbd0a6a` | Destination Chains | 71 | — | In progress | — |
| `a1192781e4bc` | About Data Types | 72 | — | In progress | — |
| `049e1774caf1` | About Filters | 74 | Route filters registered on IFlowPluginRegistry (JS + rule-builder) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/SmartConnectServiceCollectionExtensions.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs |
| `3439031dbadf` | The "msg" Object | 75 | Filter scripts use payload + metadata on IntegrationMessage (Jint context) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/JavascriptRouteFilter.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationMessage.cs |
| `0043eaf93157` | Filter Rule Types | 75 | Supported filter types: allow-all, JavaScript, rule-builder (JSON rules) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/BuiltInPlugins/AllowAllRouteFilter.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/JavascriptRouteFilter.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/RuleBuilderRouteFilter.cs |
| `055deed2a6dc` | About Transformers | 76 | Transform stages on IFlowPluginRegistry; response transform slot on outbound routes | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationFlowPipelineDefinition.cs |
| `dd16e2703a60` | The "msg" Object | 76 | Transform input: IntegrationMessage payload + metadata | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationMessage.cs |
| `d8fb6242d453` | The "tmp" Object | 76 | N/A | N/A | No separate tmp object; use IntegrationMessage.Metadata or channel scripts (scope-vs-mirth.md) |
| `30de38489db7` | Response Transformers | 76 | Per-route ResponseTransformStages on pipeline definition | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationFlowPipelineDefinition.cs; src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs |
| `803fb9390d22` | Transformer Step Types | 77 | Transform step kinds: javascript, xslt, json, xml, mapper-transform, message-builder | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/SmartConnectServiceCollectionExtensions.cs |
| `120a91251454` | Administrator Launcher Overview | 78 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `1931c0f3e8bd` | Mirth Connect Administrator | 79 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `2d0bec540b7a` | Administrator Layout | 79 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `63e3af7c81fd` | Working With Tables | 80 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `03c7396e5578` | Monitor Views | 84 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `63bb320096ed` | Dashboard View | 84 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `3c8ead8f4688` | Dashboard Table | 86 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `e703605125ea` | Dashboard Table Columns | 86 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `14c6d4a3296b` | View Messages for a Channel | 88 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `37ce1861b0eb` | Show or Hide Channel Groups | 88 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `70b3f6effa66` | Change How Tags Display | 89 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `1cbaf2ccb2eb` | Server Log | 89 | — | In progress | — |
| `6e7763c9e4e7` | Connection Log | 91 | — | In progress | — |
| `9ba7dbb80afe` | Global Maps | 92 | Global and channel maps via IVariableMapStore + REST config-map routes | Done | ConfigurationMapEndpointExtensions; ChannelScriptExecutor channelMap/globalMap |
| `4c6c3ad55310` | Global Maps Table Columns | 92 | — | In progress | — |
| `b5cc8390e49a` | Dashboard Tasks | 93 | — | In progress | — |
| `f6052f76c1fd` | Send Message | 95 | — | In progress | — |
| `e4542d05530f` | Remove All Messages | 96 | — | In progress | — |
| `e37510af8771` | Clear Statistics | 97 | — | In progress | — |
| `99097a44b22e` | Filter By Channel Name or Tag | 98 | — | In progress | — |
| `b765115cf57d` | View All Available Tags/Names | 98 | — | In progress | — |
| `61c13f92df62` | Auto-Complete Tags/Names | 99 | — | In progress | — |
| `790cf22b8dc7` | Filter By Channel Tags | 99 | — | In progress | — |
| `c76458c0dab9` | Filter By Channel | 101 | — | In progress | — |
| `1310a6d32f12` | Filter By Partial Channel Name | 101 | — | In progress | — |
| `c21736a470a6` | Filter By Multiple Criteria | 102 | — | In progress | — |
| `690c7a839977` | Clear Filter Criteria | 102 | — | In progress | — |
| `eb267384985d` | Message Browser View | 103 | Message Browser APIs: list with filters (flowId, correlationId, date range, status), GET entry by id, reprocess, flow statistics | Done | src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/ManagementEndpointExtensions.cs; MessageBrowserApiTests |
| `dd41f11b0f31` | Navigation | 104 | — | In progress | — |
| `7910938ae1a4` | From the Dashboard | 104 | — | In progress | — |
| `137b1bbd1d82` | From the Channels View | 105 | — | In progress | — |
| `d57cc0f98c28` | Metadata Table | 106 | — | In progress | — |
| `e32d40cb052d` | Add a Column to the Metadata Table | 107 | — | In progress | — |
| `58b454bd41c2` | Metadata Table Columns | 108 | — | In progress | — |
| `7ad5ec970f92` | Custom Metadata Columns | 111 | — | In progress | — |
| `e97a4e12318c` | Message Content Tab | 111 | — | In progress | — |
| `fd87fb2f2635` | View Message Content | 112 | — | In progress | — |
| `4c79a22b8705` | Message Content Types | 113 | — | In progress | — |
| `2efc1e95967b` | Formatting Messages | 114 | — | In progress | — |
| `5d229b40aa2d` | Mappings Tab | 115 | — | In progress | — |
| `14fd187586ca` | View Mappings | 116 | — | In progress | — |
| `341366726e2f` | Mappings Table Columns | 116 | — | In progress | — |
| `5a5903d10960` | Errors Tab | 116 | — | In progress | — |
| `ff479e0920d1` | View Message Errors | 117 | — | In progress | — |
| `6c32dd2aa4e1` | Error Content Types | 118 | — | In progress | — |
| `a23e8bb2a424` | Attachments Tab | 118 | — | In progress | — |
| `5888c5a3e6f6` | Attachment Table Columns | 119 | — | In progress | — |
| `b275dfced60b` | View Attachments | 119 | — | In progress | — |
| `2a64eb396355` | Text Attachment Viewer | 120 | — | In progress | — |
| `d7c4994ec221` | Image Attachment Viewer | 121 | — | In progress | — |
| `8780172310f0` | DICOM Attachment Viewer | 122 | — | In progress | — |
| `dba2fb93ad73` | PDF Attachment Viewer | 123 | — | In progress | — |
| `afc8e4c3f060` | Search Messages | 124 | Ledger query filters (flowId, correlation prefix, dates, status) — subset of MC search | Done | ManagementEndpointExtensions GET /admin/messages; IMessageLedgerQuery |
| `736247a4bb0a` | Message Search Options | 125 | — | In progress | — |
| `18d71fe936b8` | Advanced Search Filter | 127 | — | In progress | — |
| `eb6a41544dcf` | Message Browser Tasks | 130 | — | In progress | — |
| `f1ab76555544` | Import Messages | 131 | — | In progress | — |
| `eddf45bc29b4` | Export Results | 133 | — | In progress | — |
| `0858c2e885b0` | Remove Results | 135 | — | In progress | — |
| `341763e159ff` | Reprocess Results | 136 | — | In progress | — |
| `e0f496104a04` | Reprocess Message | 137 | — | In progress | — |
| `27e26a88f711` | Export Attachment | 138 | — | In progress | — |
| `44b685818f8f` | Alerts View | 139 | — | In progress | — |
| `a719fded8b65` | Navigation | 139 | — | In progress | — |
| `fc8d4a7bb89c` | Alerts Table | 140 | — | In progress | — |
| `3308fd0fd198` | Alerts Table Columns | 140 | — | In progress | — |
| `8ebe4b804d88` | Alerts Tasks | 140 | — | In progress | — |
| `ff0230f151f9` | Events View | 141 | GET /admin/events (filter/skip/take), GET /admin/events/{eventId}, export route | Done | src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/EventsEndpointExtensions.cs; IAuditEventStore |
| `167fafb422e2` | Navigation | 142 | — | In progress | — |
| `5eb4a7958208` | Events Table | 142 | — | In progress | — |
| `20e34cf87402` | Metadata Table Columns | 143 | — | In progress | — |
| `e198e9ef5eb4` | PHI Events | 144 | — | In progress | — |
| `cb4b8503ad35` | Event Attributes Table | 145 | — | In progress | — |
| `6d1db4c03a71` | Searching Events | 145 | — | In progress | — |
| `6982d7114011` | Advanced Search Filter | 146 | — | In progress | — |
| `73f805716ab9` | Event Tasks | 147 | — | In progress | — |
| `2e414e6e48d9` | Export All Events | 147 | GET /smartconnect/v1/admin/events/export (JSON dump; capped take) | Done | src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/EventsEndpointExtensions.cs |
| `23d7d23954b8` | Management Views | 148 | — | In progress | — |
| `ab0b0657f405` | Channels View | 148 | — | In progress | — |
| `5fa610fbf3d2` | Navigation | 149 | — | In progress | — |
| `41c983f0c08b` | Channel Table | 150 | — | In progress | — |
| `a4fe7d347dfb` | Channel Table Columns | 151 | — | In progress | — |
| `7b06e3fea95f` | Show or Hide Channel Groups | 151 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `29d12efd261d` | Change How Tags Display | 151 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `e0b27b68d52d` | Filter By Channel Name or Tag | 151 | — | In progress | — |
| `5674b33d7229` | Use Drag and Drop | 152 | — | In progress | — |
| `0fa473a59508` | Get the Channel Name/ID | 152 | — | In progress | — |
| `09def024b34a` | Assign Channels to a Group | 152 | — | In progress | — |
| `de57382c2001` | Import Channels/Groups Using Drag-and-Drop | 153 | — | In progress | — |
| `14cd1a3e9d34` | Channel Tasks | 154 | — | In progress | — |
| `c304f38f491b` | Import Channel | 156 | Flow/channel definition import (subset of Administrator import) | In progress | Management routes (flow import); parity with MC UI import is partial |
| `857ba44219fd` | Debug Channel | 157 | — | In progress | — |
| `89e43ed350c9` | Export Channel | 157 | — | In progress | — |
| `dd60e165bbd5` | Group Tasks | 158 | — | In progress | — |
| `e82ee17dce99` | Edit Group Details | 159 | — | In progress | — |
| `9897d4a86607` | Import Group | 160 | — | In progress | — |
| `65722f321eb3` | Export Group | 160 | — | In progress | — |
| `88db8bc2a8b4` | Users View | 160 | — | In progress | — |
| `880dddf2e789` | Navigation | 161 | — | In progress | — |
| `25d9946b5ffb` | Users Table | 161 | — | In progress | — |
| `c0716e3d6056` | Users Table Columns | 162 | — | In progress | — |
| `ee6b616f2d66` | Users Tasks | 163 | — | In progress | — |
| `92158c65ee21` | Add/Update a User Account | 163 | — | In progress | — |
| `f8ed507bf36c` | Settings View | 166 | — | In progress | — |
| `cb67b19541a6` | Navigation | 166 | — | In progress | — |
| `8ed243f3ce44` | Server Settings Tab | 167 | — | In progress | — |
| `b4435df52a24` | General Settings | 169 | — | In progress | — |
| `8252a73ec326` | Channel Settings | 170 | — | In progress | — |
| `48f754affefe` | Email Settings | 171 | — | In progress | — |
| `610ce7ba36bc` | Notification Settings | 172 | — | In progress | — |
| `507967433858` | Tasks | 173 | — | In progress | — |
| `7cda6572c24e` | Restore Config | 174 | — | In progress | — |
| `0ab20a857be1` | Clear All Statistics | 174 | — | In progress | — |
| `5b1af9c5062e` | Administrator Settings Tab | 174 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `54ebed23b65a` | System Preferences | 175 | — | In progress | — |
| `358db0b36953` | User Preferences | 177 | — | In progress | — |
| `692a2def89d7` | Code Editor Preferences | 178 | — | In progress | — |
| `1d139d57ee8e` | Tasks | 179 | — | In progress | — |
| `d28d92e49252` | Tags Settings Tab | 179 | — | In progress | — |
| `91649618dd2c` | Tags Table | 180 | — | In progress | — |
| `5328555804c9` | Adding a Tag | 181 | — | In progress | — |
| `fd9e9317771e` | Removing a Tag | 181 | — | In progress | — |
| `9f0282e53326` | Channels Table | 181 | — | In progress | — |
| `0045040b889a` | Indeterminate Check Boxes | 182 | — | In progress | — |
| `5f74a6e2db0b` | Configuration Map Settings Tab | 182 | Configuration / global / channel variable maps via REST CRUD | Done | src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/ConfigurationMapEndpointExtensions.cs; IVariableMapStore |
| `863eb71118e8` | Table Columns | 183 | — | In progress | — |
| `da0ed40e9c3c` | Tasks | 183 | — | In progress | — |
| `2915220ce311` | Database Tasks Settings Tab | 184 | — | In progress | — |
| `c60c07155911` | Database Tasks Table Columns | 185 | — | In progress | — |
| `419dad49a5c5` | Affected Channels Table | 185 | — | In progress | — |
| `437130dd4712` | Running a Database Task | 185 | — | In progress | — |
| `7c3b0e9798cc` | Resources Settings Tab | 186 | — | In progress | — |
| `38188bc2fd91` | Resources Table Columns | 187 | — | In progress | — |
| `65546f8a6976` | Tasks | 188 | — | In progress | — |
| `c6a91d33b5f7` | Reload Resource | 188 | — | In progress | — |
| `114fe630f930` | Directory Resource | 188 | — | In progress | — |
| `15e464f091e7` | Using Resources in Channels/Connectors | 190 | — | In progress | — |
| `73f764f17b3a` | Data Pruner Settings Tab | 190 | Background ledger pruner + GET configured interval/retention | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/DataPrunerHostedService.cs; src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/PrunerEndpointExtensions.cs; Api Program.cs AddSmartConnectDataPruner |
| `454a3bba349f` | Status | 191 | — | In progress | — |
| `ce1459f6a563` | Schedule | 192 | — | In progress | — |
| `45e19e372f55` | Prune Settings | 193 | — | In progress | — |
| `204d09872d15` | Archive Settings | 194 | — | In progress | — |
| `fe8575daf1c2` | Tasks | 195 | — | In progress | — |
| `cd574fcd4eeb` | Settings Tasks | 196 | — | In progress | — |
| `6cfd552610ac` | Extensions View | 196 | — | In progress | — |
| `cbc318055076` | Navigation | 197 | — | In progress | — |
| `9fd5fe5dca41` | Installed Connectors Table | 198 | — | In progress | — |
| `c8291e0afbfc` | Installed Connectors Table Columns | 199 | — | In progress | — |
| `25aba066d842` | Installed Plugins Table | 199 | — | In progress | — |
| `22d21e7bc6be` | Installed Plugins Table Columns | 200 | — | In progress | — |
| `2ce4287ef560` | Install a New Extension | 200 | — | In progress | — |
| `a597ea89d0aa` | Extension Tasks | 201 | — | In progress | — |
| `474b188a9bfa` | Show Properties | 202 | — | In progress | — |
| `17476e92cc69` | Edit Views | 203 | — | In progress | — |
| `deec6e3040ac` | Edit Channel View | 203 | — | In progress | — |
| `da2de6bcaf1a` | Navigation | 203 | — | In progress | — |
| `652204eae947` | Summary Tab | 204 | — | In progress | — |
| `e0da3acf9003` | Channel Properties | 205 | — | In progress | — |
| `8f718abc6e1a` | Set Data Types Window | 207 | — | In progress | — |
| `9c8fdd1a42b2` | Select Data Types | 208 | — | In progress | — |
| `be800b5fefa4` | Change Data Type Properties | 209 | — | In progress | — |
| `9f38760fef9c` | Bulk Edit Mode | 210 | — | In progress | — |
| `b61fdc42dced` | Set Dependencies Window | 212 | — | In progress | — |
| `a7ce739b4d7d` | Code Template Libraries | 213 | — | In progress | — |
| `163cabfed67c` | Link Code Template Libraries | 213 | — | In progress | — |
| `81ce8b48b3c0` | Library Resources | 214 | — | In progress | — |
| `dafb0378562f` | Deploy/Start Dependencies | 215 | — | In progress | — |
| `f9c758dcae77` | Deploy/Start Channels | 217 | — | In progress | — |
| `30444d1d023e` | Pause/Stop/Undeploy Channels | 218 | — | In progress | — |
| `454721e1f823` | Attachment Handlers | 219 | — | In progress | — |
| `01cabaf05923` | Extraction | 220 | — | In progress | — |
| `e0d1b0c489f4` | Reattachment | 220 | — | In progress | — |
| `097189137806` | Expanded Replacement Tokens | 220 | — | In progress | — |
| `16d136564ce4` | Attachment MIME Types | 221 | — | In progress | — |
| `4890e76d7fcf` | Attachment Handler Properties | 221 | — | In progress | — |
| `b32b8acf3e75` | Entire Message Attachment Handler Properties | 221 | — | In progress | — |
| `34b2ca8103b1` | Regex Attachment Handler Properties | 222 | — | In progress | — |
| `029122e7bc93` | Regular Expressions Table | 223 | — | In progress | — |
| `d0a9a749d003` | String Replacement Tables | 224 | — | In progress | — |
| `bbaea87685b5` | DICOM Attachment Handler Properties | 224 | — | In progress | — |
| `41352218137a` | JavaScript Attachment Handler Properties | 224 | — | In progress | — |
| `c2cec78b3533` | Scope Variables | 225 | — | In progress | — |
| `9e58f5372f2d` | Extract Attachments | 225 | — | In progress | — |
| `eac4fcd31629` | addAttachment(data, type) | 226 | — | In progress | — |
| `9adfacb7123e` | Custom Attachment Handler Properties | 226 | — | In progress | — |
| `c35c736a202c` | Properties | 227 | — | In progress | — |
| `34fe901b32b2` | Message Storage Settings | 227 | — | In progress | — |
| `bae1ecd854c3` | Message Pruning Settings | 229 | — | In progress | — |
| `d49d8048b497` | Custom Metadata Columns | 230 | — | In progress | — |
| `e8163150f4bb` | Modifying Custom Metadata Columns | 231 | — | In progress | — |
| `f3b484537db6` | Channel Description | 231 | — | In progress | — |
| `b0657b73534e` | Source Tab | 232 | — | In progress | — |
| `2ff06d05acdf` | Choose a Source Connector | 233 | Source connectors: HTTP webhook, MLLP listener, File Reader, TCP Listener, Database Reader (registry + hosted service) | Done | src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Abstractions/ISourceConnector.cs; src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Hosting/SourceConnectorHostedService.cs |
| `05f49cb52519` | Listener Settings | 233 | — | In progress | — |
| `9c0f28631402` | Polling Settings | 234 | — | In progress | — |
| `109fe06b21e3` | Interval Schedule Settings | 235 | — | In progress | — |
| `56955c34b244` | Time Schedule Settings | 236 | — | In progress | — |
| `0b6bcd03be1a` | Cron Schedule Settings | 236 | — | In progress | — |
| `073aa9db064d` | Advanced Settings | 237 | — | In progress | — |
| `96c4d761f215` | Source Settings | 238 | — | In progress | — |
| `cbad119904fa` | HTTP Authentication Settings | 239 | N/A | N/A | MC channel HTTP authentication UI; configure auth at reverse proxy or in host HTTP client (scope-vs-mirth.md) |
| `40357d5af6f6` | Choose an Authentication Type | 240 | N/A | N/A | MC channel HTTP authentication UI; configure auth at reverse proxy or in host HTTP client (scope-vs-mirth.md) |
| `8ca8bf077d45` | Basic HTTP Authentication | 240 | N/A | N/A | MC channel HTTP authentication UI; configure auth at reverse proxy or in host HTTP client (scope-vs-mirth.md) |
| `6a42ce4d791b` | Digest HTTP Authentication | 241 | N/A | N/A | MC channel HTTP authentication UI; configure auth at reverse proxy or in host HTTP client (scope-vs-mirth.md) |
| `e6886c44f30c` | JavaScript HTTP Authentication | 243 | N/A | N/A | MC channel HTTP authentication UI; configure auth at reverse proxy or in host HTTP client (scope-vs-mirth.md) |
| `443e8c6c31e3` | Custom Java Class HTTP Authentication | 244 | N/A | N/A | MC channel HTTP authentication UI; configure auth at reverse proxy or in host HTTP client (scope-vs-mirth.md) |
| `1a89aa729221` | OAuth 2.0 Token Verification | 245 | — | In progress | — |
| `eb51dc5af4aa` | Source Connector Properties | 245 | — | In progress | — |
| `42d9c9b636e0` | Destinations Tab | 246 | — | In progress | — |
| `9ed6ad71ae21` | Destination Table | 247 | — | In progress | — |
| `0110805ec26b` | Destination Tasks | 248 | — | In progress | — |
| `701d10fc4cf4` | Destination Settings | 249 | — | In progress | — |
| `02eb96bc6a08` | Advanced Queue Settings | 251 | — | In progress | — |
| `fbfaaf461730` | Destination Connector Properties | 252 | — | In progress | — |
| `03af45160d82` | Destination Mappings | 253 | — | In progress | — |
| `dc0dc437a07e` | Standard Variables/Templates | 253 | — | In progress | — |
| `9cd7666923ab` | Scripts Tab | 255 | Scripts tab: FlowScriptsDefinition in pipeline JSON (deploy/undeploy/pre/post) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/FlowScriptsDefinition.cs; FlowRuntimeEngine; ManagementEndpointExtensions |
| `7c7e0b552132` | Edit Channel Tasks | 256 | — | In progress | — |
| `4437d3b0eb59` | Edit Filter / Transformer Views | 257 | — | In progress | — |
| `05fadd448ad3` | Navigation | 258 | — | In progress | — |
| `18ece9d87a35` | Message Templates Tab | 260 | — | In progress | — |
| `342393f59ba5` | Edit Data Types | 262 | — | In progress | — |
| `b1d9164d5bde` | Specify a Template | 264 | — | In progress | — |
| `1b4a00ba981d` | Message Trees Tab | 265 | — | In progress | — |
| `b150dd58710b` | Filter By Node Name/Description | 267 | — | In progress | — |
| `c634b1e29a74` | Create a Rule Builder Rule or Mapper Step | 268 | — | In progress | — |
| `32f400d67cf1` | Method 1 | 268 | — | In progress | — |
| `79d20f508317` | Method 2 | 269 | — | In progress | — |
| `b405e9a43037` | Create a Message Builder Step | 270 | — | In progress | — |
| `44c57af0d1a5` | Method 1: | 270 | — | In progress | — |
| `dfda15817082` | Method 2 | 271 | — | In progress | — |
| `5f555807a325` | Drag-and-Drop Field Values | 272 | — | In progress | — |
| `2f5299539d79` | Reference Tab | 273 | — | In progress | — |
| `9cd4435eeac3` | Reference List | 274 | — | In progress | — |
| `e1b1ce2e2fcf` | Available Variables | 275 | — | In progress | — |
| `5b05e951280d` | Create New Rules/Steps | 276 | — | In progress | — |
| `8403b1d5c9ec` | Rule/Step Table | 277 | — | In progress | — |
| `815e62d9f133` | Filter Rule Properties | 278 | — | In progress | — |
| `1e06cb7ebde1` | Rule Builder Filter Rule | 278 | Declarative rule-builder route filter (payloadContains, metadataEquals; JSON parameters) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/RuleBuilderRouteFilter.cs; RuleBuilderRouteFilterTests |
| `2a345bb9af56` | JavaScript Filter Rule | 279 | JavaScript route filter (Jint; channel route filter slot) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/JavascriptRouteFilter.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/JavascriptRouteFilterTests.cs |
| `85f102539778` | External Script Filter Rule | 279 | — | In progress | — |
| `1052c34c408d` | Iterator Filter Rule | 280 | N/A | N/A | No iterator filter; use JavaScript route filter for batch logic |
| `43b7b891c3b1` | Transformer Step Properties | 281 | — | In progress | — |
| `2958d82dc5f6` | Mapper Transformer Step | 281 | Mapper transform stage (alias over JSON path mapper) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/MapperTransformStage.cs; MapperTransformStageTests |
| `7138654d5a99` | Message Builder Transformer Step | 282 | Message Builder transform (UTF-8 prefix/suffix) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/MessageBuilderTransformStage.cs; MessageBuilderTransformStageTests |
| `75e73d330f1a` | JavaScript Transformer Step | 283 | Sandboxed JS transform stage (Jint + timeout) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/JavascriptTransformStage.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/JavascriptTransformStageTests.cs |
| `bbf2966bbe5f` | External Script Transformer Step | 283 | — | In progress | — |
| `5d8d7df14348` | XSLT Transformer Step | 283 | XSLT transform stage (System.Xml.Xsl) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/XsltTransformStage.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/XsltTransformStageTests.cs |
| `7c8e4785a731` | Destination Set Filter Transformer Step | 284 | — | In progress | — |
| `121bd3845f7d` | Iterator Transformer Step | 286 | N/A | N/A | No iterator transform; use JavaScript transform or multiple outbound routes |
| `d9edaa4d4de8` | Response Transformers | 286 | — | In progress | — |
| `6e0704439eda` | Modifying the Response | 287 | — | In progress | — |
| `e6df3c9fba0e` | Common Scenarios | 287 | — | In progress | — |
| `8b8e13cf98bb` | Re-queue a message if the HL7 ACK has an AE code | 287 | — | In progress | — |
| `7d8754af2903` | Force a queuing message to error if the number of send attempts exceeds some threshold | 288 | — | In progress | — |
| `d75a3391beea` | Route the response data to a downstream channel | 288 | — | In progress | — |
| `203b41701989` | Working With Iterators | 288 | — | In progress | — |
| `f49aa4f25d9b` | Creating Iterators From Drag-and-Drop | 289 | — | In progress | — |
| `90a10bc93515` | The Assign To Iterator Task | 291 | — | In progress | — |
| `8a5d6f5134bb` | Remove From Iterators | 294 | — | In progress | — |
| `a8a5552e4d94` | View Generated Script | 294 | — | In progress | — |
| `790c7a7e4ed7` | Filter Tasks | 295 | — | In progress | — |
| `eeef1df1250f` | Import Filter | 296 | — | In progress | — |
| `7f0f51f77a77` | Transformer Tasks | 297 | — | In progress | — |
| `725a20d1c18f` | Import Transformer | 298 | — | In progress | — |
| `76703336aa90` | Edit Global Scripts View | 299 | — | In progress | — |
| `fa56c9df18f0` | Navigation | 299 | — | In progress | — |
| `6f7ef9e54c60` | Edit Global Scripts | 300 | — | In progress | — |
| `ecd6372388ab` | Tasks | 301 | — | In progress | — |
| `7cb9a8890739` | Edit Code Templates View | 301 | — | In progress | — |
| `d1a013d4b76b` | Navigation | 302 | — | In progress | — |
| `bd08744ff76d` | Code Template Library Table | 303 | — | In progress | — |
| `1b419734c6fa` | Edit Library Panel | 304 | — | In progress | — |
| `e56460f67110` | Link Libraries to Channels | 305 | — | In progress | — |
| `af35697d05d5` | Edit Code Template Panel | 305 | — | In progress | — |
| `38b667d9e077` | Code Template Contexts | 307 | — | In progress | — |
| `02b976218fe6` | Use JSDoc in Code Templates | 308 | — | In progress | — |
| `fd99234d2268` | Code Template Tasks | 310 | — | In progress | — |
| `21fe3d03e7ea` | Import Code Templates/Libraries | 311 | — | In progress | — |
| `185f2e590ff8` | Edit Alert View | 313 | — | In progress | — |
| `01102b17f33c` | Navigation | 314 | — | In progress | — |
| `466338502386` | Alert Error Types and Regex | 315 | — | In progress | — |
| `f3a78fd55445` | Alert Error Categories | 316 | — | In progress | — |
| `a87ebd1c499d` | Alert Enabled Channels | 317 | — | In progress | — |
| `f4f375b709a1` | Alert Actions | 318 | — | In progress | — |
| `962f93f812af` | Protocols | 318 | — | In progress | — |
| `95e570028c91` | Alert Variables | 319 | — | In progress | — |
| `893b2bc5f810` | Edit Alert Tasks | 319 | — | In progress | — |
| `e659be27bf19` | Other Tasks | 320 | — | In progress | — |
| `7de79d021822` | Notifications | 321 | — | In progress | — |
| `4957754333ae` | Data Types | 323 | — | In progress | — |
| `1da7292df029` | Inbound Properties | 323 | — | In progress | — |
| `3582017332ba` | Outbound Properties | 323 | — | In progress | — |
| `97f595a57c72` | Delimited Text Data Type | 324 | — | In progress | — |
| `d7cda74ac759` | DICOM Data Type | 327 | — | In progress | — |
| `f2a2c4aa064d` | Example XML snippet: | 327 | — | In progress | — |
| `2d28fa4883d2` | EDI / X12 Data Type | 328 | N/A | N/A | No EDI/X12 parser; payloads pass through as binary/text |
| `3e4d341b19e6` | HL7 v2.x Data Type | 329 | HL7 v2.x parser with path-based segment/field access (subset of MC message tree) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/DataTypes/Hl7V2Parser.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/Hl7V2ParserTests.cs |
| `5ad4fcb40c11` | HL7 v3.x Data Type | 332 | N/A | N/A | No HL7 v3 RIM parser; use XML transform or custom script |
| `39dfc1c57987` | JSON Data Type | 333 | JSON transform stage (path extraction / mappings) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/Transforms/JsonTransformStage.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/JsonTransformStageTests.cs |
| `470fdb353236` | NCPDP Data Type | 334 | — | In progress | — |
| `53b493eef0a0` | Raw Data Type | 335 | — | In progress | — |
| `fcbc2850c3c6` | XML Data Type | 336 | XML transform stage (XPath extraction) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/Transforms/XmlTransformStage.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/XmlTransformStageTests.cs |
| `e17335a4fb83` | Batch Processing | 337 | — | In progress | — |
| `0b734799ad47` | JavaScript Batch Script | 338 | — | In progress | — |
| `12a2b7f4dfbc` | Source Connectors | 340 | — | In progress | — |
| `abea0d6bb89e` | Channel Reader | 340 | N/A | N/A | No separate Channel Reader; use Channel Writer destination to another flow |
| `933ef4ca2258` | Source Map Variables | 341 | — | In progress | — |
| `67d61933dcf2` | DICOM Listener | 341 | N/A | N/A | No DICOM listener in core |
| `a10513ed9df1` | Source Map Variables | 344 | — | In progress | — |
| `ee6dd2c1e477` | Database Reader | 346 | Database reader source connector (watermark-based polling) | Done | src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.DatabaseReader/DatabaseReaderSourceConnector.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/DatabaseReaderParameterTests.cs |
| `bb60791d2158` | Editing Database Drivers | 350 | — | In progress | — |
| `cabdef066e91` | File Reader | 351 | File Reader source connector (polling, after-read actions, quarantine) | Done | src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.FileReader/FileReaderSourceConnector.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/FileReaderSourceConnectorTests.cs |
| `1c0f86389fc6` | Advanced FTP Options | 357 | N/A | N/A | File Reader is local filesystem only; no FTP transport in core |
| `25d2d65030ce` | Advanced SFTP Options | 357 | N/A | N/A | File Reader is local filesystem only; SFTP not in core |
| `98dc69797b66` | Advanced SMB Options | 359 | N/A | N/A | File Reader is local filesystem only; SMB not in core |
| `14ba60978325` | Advanced Amazon S3 Options | 359 | N/A | N/A | File Reader is local filesystem only; S3 not in core |
| `e24813c8155e` | Source Map Variables | 361 | — | In progress | — |
| `001a54fbd3e2` | HTTP Listener | 362 | Inbound HTTP webhook routes | Done | src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.AspNetCore/SmartConnectInboundEndpointExtensions.cs |
| `99f63d49dff3` | JMS Listener | 366 | N/A | N/A | No JMS listener in SmartConnect; use platform messaging or HTTP bridge |
| `ee05a8041a69` | JMS Connection Templates | 369 | N/A | N/A | No JMS in core |
| `d19fabfe08df` | Loading Templates | 369 | N/A | N/A | No JMS templates in core |
| `4a0d97315103` | Creating New Templates | 369 | N/A | N/A | No JMS templates in core |
| `c582b3d4e532` | Updating Templates | 370 | N/A | N/A | No JMS templates in core |
| `26cc04200cb7` | Deleting Templates | 370 | N/A | N/A | No JMS templates in core |
| `6c09f0e130ae` | JavaScript Reader | 371 | N/A | N/A | No MC-style JavaScript reader connector in core |
| `9acc74d3387e` | JavaScript Reader Return Values | 371 | — | In progress | — |
| `de7638b26b86` | TCP Listener | 372 | TCP listener source connector (MLLP / LF / length-prefix framing) | Done | src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.TcpListener/TcpListenerSourceConnector.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/TcpListenerFrameDecoderTests.cs |
| `d2df0999352d` | Source Map Variables | 376 | — | In progress | — |
| `f84c6390b80b` | TCP Transmission Modes | 376 | — | In progress | — |
| `0f464f24cd13` | Basic TCP Transmission Mode | 377 | — | In progress | — |
| `acca89ea2413` | Byte Abbreviations | 377 | — | In progress | — |
| `519ffc586eec` | MLLP Transmission Mode | 378 | MLLP TCP listener framing | Done | src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Mllp/MllpInboundHostedService.cs |
| `0704561e0c73` | Byte Abbreviations | 380 | — | In progress | — |
| `673d3d69227d` | Web Service Listener | 381 | N/A | N/A | No MC-style Web Service listener; use inbound HTTP + contract in app layer |
| `c27980f69bbf` | Destination Connectors | 384 | — | In progress | — |
| `9bb0daf8d64a` | Channel Writer | 384 | Channel writer outbound adapter (in-process flow chaining with depth guard) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/ChannelWriterOutboundAdapter.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/ChannelWriterOutboundAdapterTests.cs |
| `ecd047bb8c78` | Source Map Variables | 386 | — | In progress | — |
| `72ee90d0e889` | DICOM Sender | 386 | N/A | N/A | No DICOM sender in core |
| `1e52b8d7a0d1` | Database Writer | 390 | Database outbound adapter (parameterized, named connection strings, SqlServer + Postgres) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/DatabaseOutboundAdapter.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/DatabaseOutboundAdapterTests.cs |
| `628eff9187e0` | Document Writer | 393 | N/A | N/A | No Document Writer; use File or HTTP outbound with rendered document |
| `a9149b98297c` | File Writer | 395 | File outbound adapter | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/FileOutboundAdapter.cs |
| `b0420a814a90` | Connector Map Variables | 400 | — | In progress | — |
| `1e802e478244` | HTTP Sender | 400 | HTTP outbound adapter | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/HttpOutboundAdapter.cs |
| `1355b11537ce` | Connector Map Variables | 407 | — | In progress | — |
| `31adda5c3122` | JMS Sender | 407 | N/A | N/A | No JMS sender in SmartConnect; use HTTP outbound or native broker client |
| `a38b458b9e80` | JavaScript Writer | 410 | N/A | N/A | No JavaScript Writer connector; use JS transform stage or channel scripts |
| `b78a1ad07466` | JavaScript Writer Return Values | 411 | — | In progress | — |
| `b0e26e4772e5` | SMTP Sender | 411 | SMTP outbound adapter (basic) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/SmtpOutboundAdapter.cs |
| `a74f2026837f` | TCP Sender | 416 | TCP outbound adapter (raw, length-prefixed, MLLP framing; pooled connections) | Done | src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/TcpOutboundAdapter.cs; src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/TcpOutboundAdapterTests.cs |
| `4f3bdc6f1747` | Web Service Sender | 422 | N/A | N/A | No SOAP Web Service sender; use HTTP outbound with SOAP payload |
| `693f111fbb39` | Mirth Connect and JavaScript | 428 | — | In progress | — |
| `90e44e3af4d1` | About JavaScript | 428 | — | In progress | — |
| `6d01c49f1da3` | Variables | 428 | — | In progress | — |
| `65de032656c5` | Comments | 429 | — | In progress | — |
| `f1a5d4a9ba1f` | Arrays | 429 | — | In progress | — |
| `f5a45b8c605d` | Operators | 430 | — | In progress | — |
| `c13364030142` | Arithmetic Operators | 430 | — | In progress | — |
| `107b83613c04` | Assignment Operators | 431 | — | In progress | — |
| `6ceeb6314e87` | Comparison Operators | 431 | — | In progress | — |
| `250e9b59eb95` | Logical Operators | 432 | — | In progress | — |
| `cfdfc6cfa37a` | Conditional Statements | 432 | — | In progress | — |
| `97d055c53958` | Functions | 432 | — | In progress | — |
| `cc8193dcbf70` | Loops and Iterations | 433 | — | In progress | — |
| `12354654582e` | for loops | 433 | — | In progress | — |
| `8f98e579a924` | for each…in loops | 433 | — | In progress | — |
| `2123d39c6241` | while loops | 434 | — | In progress | — |
| `299ff31cb480` | do…while loops | 434 | — | In progress | — |
| `c9d4b380a1ee` | Exception Handling | 435 | — | In progress | — |
| `ef5bacf63c10` | Using JavaScript in Mirth Connect | 435 | — | In progress | — |
| `00ee0ad6d3d7` | About E4X | 435 | — | In progress | — |
| `f77e7c95e156` | Accessing Message Data with E4X | 436 | — | In progress | — |
| `11ec186acf42` | Adding Segments to a Message | 437 | — | In progress | — |
| `25ce372541ba` | Deleting a Segment | 439 | — | In progress | — |
| `af130ee6329e` | Iterating Over Message Segments | 439 | — | In progress | — |
| `d895fcc580ad` | Iterating Over Repeating Fields | 440 | — | In progress | — |
| `9f3ad6ebc16c` | Adding a New Repeating Field | 440 | — | In progress | — |
| `dffca0a5fa52` | Message Variables | 440 | — | In progress | — |
| `22cab570408f` | Built-In Code Templates | 441 | — | In progress | — |
| `f9ebb111c329` | Using Java Classes | 441 | — | In progress | — |
| `991bd87b77cd` | Regular Expressions | 441 | — | In progress | — |
| `367e9f547583` | Logging with JavaScript | 443 | — | In progress | — |
| `3115d770d469` | Generating a Hash with JavaScript | 443 | — | In progress | — |
| `9826cfb0c241` | Using the JavaScript Editor | 445 | — | In progress | — |
| `6dcc8bd816c4` | Using the Context Menu in the JavaScript Editor | 445 | — | In progress | — |
| `19efe521a02b` | Finding/Replacing Code in the JavaScript Editor | 446 | — | In progress | — |
| `7483ca585581` | Folding in the JavaScript Editor | 447 | — | In progress | — |
| `395163e7d5cc` | Using the Auto-Completion Popup in the JavaScript Editor | 448 | — | In progress | — |
| `988caebe1b59` | Remapping Editor Shortcut Keys | 449 | — | In progress | — |
| `9e0a1a681980` | Variable Maps | 449 | — | In progress | — |
| `7005e791f922` | Connector Map | 450 | — | In progress | — |
| `85183e12a370` | Channel Map | 451 | — | In progress | — |
| `95e469fce0de` | Source Map | 451 | — | In progress | — |
| `c8d4405adeff` | Response Map | 452 | — | In progress | — |
| `b839f4a9582a` | Global Channel Map | 452 | — | In progress | — |
| `91920d9ae4e3` | Global Map | 453 | — | In progress | — |
| `5f43f543600d` | Configuration Map | 453 | — | In progress | — |
| `d58569767feb` | The Variable Map Lookup Sequence | 454 | — | In progress | — |
| `f4a5161dbf5a` | Attachment JavaScript Functions | 455 | — | In progress | — |
| `291676e917c2` | Built-In Attachment Functions | 455 | — | In progress | — |
| `26a30223c229` | The AttachmentUtil Class | 457 | — | In progress | — |
| `e772bb6f0df4` | The Attachment Object | 458 | — | In progress | — |
| `d811d53890ac` | Examples | 459 | — | In progress | — |
| `f935cb95d8fa` | The User API (Javadoc) | 459 | — | In progress | — |
| `c260d67414a3` | Mirth Connect Debugger | 461 | — | In progress | — |
| `c1b08a11e681` | Before You Begin | 462 | — | In progress | — |
| `ade9e3a1104d` | To Edit the mcserver.vmoptions file | 462 | — | In progress | — |
| `2dd16b9ab7e4` | Use the Debugger | 463 | — | In progress | — |
| `27151831795d` | Debugger Window | 464 | — | In progress | — |
| `61dbac19ee37` | Debugger Menus | 465 | — | In progress | — |
| `454571958af0` | File Menu | 465 | — | In progress | — |
| `a050f78878a5` | Edit Menu | 466 | — | In progress | — |
| `58010e400b1f` | Debug Menu | 467 | — | In progress | — |
| `924465691f98` | Window Menu | 467 | — | In progress | — |
| `98f84b9ca18b` | Coding Area | 468 | — | In progress | — |
| `fb2bd68085ba` | Velocity Variable Replacement | 469 | — | In progress | — |
| `dbd3139b0d38` | Basic Syntax | 469 | — | In progress | — |
| `0da94159ff5b` | Conditional Statements | 470 | — | In progress | — |
| `35c5f20bfd21` | For Loops | 470 | — | In progress | — |
| `1e8466dc5a69` | Mirth Connect Command Line Interface | 471 | — | In progress | — |
| `77b068248a31` | Running the Command Line Interface | 471 | — | In progress | — |
| `1e4c3c953b6d` | Using Non-interactive Scripting | 472 | — | In progress | — |
| `1048ab38243e` | Mirth Connect REST API | 473 | Management HTTP API (flows, lifecycle, ledger, maps, events, pruner); optional JWT when configured | Done | src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/ManagementEndpointExtensions.cs; ManagementSecurityExtensions.cs; Api Program.cs AddSmartConnectManagementJwt / UseAuthentication |
| `a5e288ce0b6e` | Authentication | 476 | N/A | N/A | Release-notes topic; not SmartConnect feature matrix |
| `f6e04f09424c` | Installation Directory | 478 | — | In progress | — |
| `849efb2b04ed` | Application Data Directory | 478 | — | In progress | — |
| `8754f4f0dcae` | configuration.properties | 478 | — | In progress | — |
| `be2e793b8e43` | extension.properties | 479 | — | In progress | — |
| `5e4fc55e03b5` | keystore.jks | 479 | — | In progress | — |
| `f583b3155102` | server.id | 480 | — | In progress | — |
| `0d5235049dc8` | temp | 480 | — | In progress | — |
| `ac93193a3787` | Configuration Directory | 480 | — | In progress | — |
| `e2620aacea13` | dbdrivers.xml File | 480 | — | In progress | — |
| `2e6fa1798fbc` | Adding a new entry to dbdrivers.xml | 481 | — | In progress | — |
| `23bc034a650b` | log4j2.properties File | 482 | — | In progress | — |
| `9f1cee054676` | log4j2-cli.properties File | 484 | — | In progress | — |
| `bd1a0295bd9c` | mirth.properties File | 485 | — | In progress | — |
| `4ce218df8f34` | Split Database Connection Pools | 497 | — | In progress | — |
| `ea29d20b9bef` | Default Supported Cipher Suites | 498 | — | In progress | — |
| `799c01292d28` | New Default Digest Algorithm in Mirth Connect 4.4 | 499 | — | In progress | — |
| `e47daed2d9eb` | Update the Digest Algorithm | 499 | — | In progress | — |
| `8c47d8633e6b` | mirth-cli-config.properties File | 500 | — | In progress | — |
| `bf197be3beea` | Other Files and Folders | 500 | — | In progress | — |
| `6170f9dd3db6` | Frequently Asked Questions | 502 | — | In progress | — |
| `83f69eeff687` | Is Mirth® Connect the same as "Mirth"? | 502 | — | In progress | — |
| `2b418ce59ec1` | Who develops Mirth® Connect? | 502 | — | In progress | — |
| `87cc84363b05` | What is the Mirth® Connect license, and how much does it cost? | 502 | — | In progress | — |
| `6b23ae622805` | How can Mirth® Connect be free and open-source? | 502 | — | In progress | — |
| `e74343451827` | Is there a difference between the free, open-source Mirth® Connect download and the supported version of Mirth® Connect? | 503 | — | In progress | — |
| `a8ced0d763d9` | How does Mirth® Connect compare to commercial integration engines? | 503 | — | In progress | — |
| `cc21702b2373` | How many production installations of Mirth® Connect are there? | 503 | — | In progress | — |
| `e10cfff4e5ad` | What can I expect next from Mirth® Connect? | 503 | — | In progress | — |
| `491b9237878d` | How fast does Mirth® Connect operate? | 503 | — | In progress | — |
| `faa04df6d9c2` | Is Mirth® Connect hard to install and configure? | 504 | — | In progress | — |
| `3f2ed5558177` | Do I need Mirth Appliance to run Mirth® Connect? | 504 | — | In progress | — |
| `bdc107b18d23` | As a member of the Mirth Solutions community, how can I get more help? | 504 | — | In progress | — |
| `ca9cbec5f443` | How do I become a Mirth® Connect expert? | 504 | — | In progress | — |
| `9510524b9c84` | What are the system requirements for Mirth® Connect? | 504 | — | In progress | — |
| `67e5e1804d95` | Which databases does Mirth® Connect support for its data store? | 505 | — | In progress | — |
| `54471df947ba` | Does Mirth® Connect use the Mule ESB? | 505 | — | In progress | — |
| `bd59b5fb6202` | Do I need an application server to run Mirth® Connect? | 505 | — | In progress | — |
| `76432cb8a21c` | Can Mirth® Connect send data to __ or transform data from __ to __? | 505 | — | In progress | — |
| `56cdafbfd0fb` | What message standards does Mirth® Connect support? | 505 | — | In progress | — |
| `664dfebc2935` | What transfer protocols does Mirth® Connect support? | 506 | — | In progress | — |
| `8f4ef3c94e5d` | How do I transform a data segment? | 507 | — | In progress | — |
| `40a482aa9156` | How can I increase the text size when using the Mirth® Connect Administrator on a high DPI monitor and the text is small and difficult to read? | 507 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `9774ce82a593` | Channel Development Best Practices and Tips | 509 | — | In progress | — |
| `73c83a8392d8` | Channel Performance | 509 | — | In progress | — |
| `365672498056` | Adjust the Message Storage slider so that the channel only retains the message data that you will actually need. | 510 | — | In progress | — |
| `0d0bd1e3ef0d` | If large messages are expected, use an Attachment Handler to improve throughput and reduce memory and disk usage. | 510 | — | In progress | — |
| `26ee0f069b26` | Enable source queuing if an auto-generated acknowledgement is sufficient for the upstream system. | 511 | — | In progress | — |
| `f3f7a10928e0` | Enable destination queuing if you do not need to respond to the originating system from your destination. | 511 | — | In progress | — |
| `8a895429d36f` | Increase the Max Processing Threads value if message order is not important and you may receive a large number of messages in a short time | 511 | — | In progress | — |
| `e9e718eb9ba0` | Increase the Queue Threads value if the downstream system can accept multiple concurrent connections. | 512 | — | In progress | — |
| `3801c4e05c4d` | Clear the "Wait for previous destination" checkbox, unless you need destinations to process messages one after another. | 512 | — | In progress | — |
| `b796346b0b79` | Use the "Destination Set Filter" feature in your source transformer when you need to route messages to only one (or a subset) of destinations in your channel. | 512 | — | In progress | — |
| `c6e23cd56f51` | Channel Configuration | 513 | — | In progress | — |
| `01e5fd3ba54f` | Use tags to categorize your channels. | 514 | — | In progress | — |
| `aba1897961b6` | Place any reusable JavaScript code into Code Template functions and organize them into Code Template Libraries. | 514 | — | In progress | — |
| `d88b8fd04af8` | Use Custom Meta Data Columns to increase the performance of searching the message history on a particular message field. | 514 | — | In progress | — |
| `02239180acb6` | Use Deploy/Start Dependencies if one channel depends on another to operate correctly. | 515 | — | In progress | — |
| `ed2e97661a35` | Use multiple resource folders for your Java libraries if you need to limit library usage to specific channels or if you have many Java libraries. | 516 | — | In progress | — |
| `a2c1fd3d9e92` | Message Integrity | 516 | — | In progress | — |
| `3ed955eacb8c` | Use hashing to ensure message integrity | 517 | — | In progress | — |
| `d260837c46cf` | Other Tips | 517 | — | In progress | — |
| `861d177986f3` | Do not reference Mirth Connect internal Java classes in your JavaScript code. | 518 | — | In progress | — |
| `05d0b5a15f64` | Security Best Practices | 519 | — | In progress | — |
| `429075ea7c11` | Secure Installation and Deployment | 519 | — | In progress | — |
| `daa7ca6cee48` | Installation Directory | 519 | — | In progress | — |
| `985bfc3e8674` | Networking | 520 | — | In progress | — |
| `a928c555c02e` | Web Server Certificate | 521 | — | In progress | — |
| `5a7bdc521719` | Connect to Database using SSL/TLS | 522 | — | In progress | — |
| `f68531a49349` | Importing the Database Server Certificate | 522 | — | In progress | — |
| `bfbf88582cdc` | PostgreSQL | 523 | — | In progress | — |
| `aadb36f016e1` | MySQL | 523 | — | In progress | — |
| `a7f0ec2ba2ef` | Oracle | 524 | — | In progress | — |
| `fc75b93eaaeb` | SQL Server | 524 | — | In progress | — |
| `d96517c6f899` | Secure Configuration | 525 | — | In progress | — |
| `b18ea5f70802` | Encryption Settings | 525 | — | In progress | — |
| `ab634c646084` | Encrypt Database Password | 527 | N/A | N/A | MC server/keystore password UI; use Kubernetes secrets / ASP.NET configuration (scope-vs-mirth.md) |
| `8b126975e608` | Plain HTTP Main Web Server | 527 | — | In progress | — |
| `e014126fb6b9` | Default TLS/SSL Settings | 527 | — | In progress | — |
| `c2547e34b82f` | Default Supported Cipher Suites | 528 | — | In progress | — |
| `4e8e77e561a7` | Cipher Suites Removed From Earlier Versions | 529 | — | In progress | — |
| `a251877a724e` | New Protocol/Cipher Suite Support in Java 11 | 531 | — | In progress | — |
| `9251fe63c07d` | Password Requirements | 532 | N/A | N/A | MC server/keystore password UI; use Kubernetes secrets / ASP.NET configuration (scope-vs-mirth.md) |
| `a8211fb9cf73` | SSL Manager Extension | 533 | — | In progress | — |
| `09d6533cdcc8` | Source Connector Settings | 533 | — | In progress | — |
| `e9c1fee6c59a` | Destination Connector Settings | 534 | — | In progress | — |
| `99141bc26b75` | Advanced Alerting - SSL Manager Trigger | 535 | — | In progress | — |
| `7e39e01376f3` | Operational Procedures | 536 | — | In progress | — |
| `5fad94d14978` | Users/Permissions | 536 | — | In progress | — |
| `16ae05e6d92a` | Auditing | 537 | — | In progress | — |
| `3395e24827ab` | Environmental Upkeep | 538 | — | In progress | — |
| `91a18e0081f6` | Mirth Connect Software | 538 | — | In progress | — |
| `11c3bb8b4a14` | Operating System | 539 | — | In progress | — |
| `15501dacb612` | Java | 539 | — | In progress | — |
| `9a72c9e0684c` | Troubleshooting | 540 | — | In progress | — |
| `d3cbd329c8ae` | Logs | 540 | — | In progress | — |
| `4217df4216a3` | Configuration | 540 | — | In progress | — |
| `ef4f04f2c292` | Mirth Connect Engine fails to start up | 540 | — | In progress | — |
| `f0001315e10f` | Unable to Launch Mirth Connect Administrator | 542 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `544943379c0b` | Clearing your Java Cache | 543 | — | In progress | — |
| `102bf80a894e` | Using the Java Control Panel | 543 | — | In progress | — |
| `458aa783b2ec` | Using the Command Line | 545 | — | In progress | — |
| `eb8b1315acef` | Opening the Java Client Console | 546 | — | In progress | — |
| `24275f64d4af` | Out of Memory Errors | 549 | — | In progress | — |
| `f24ca75860a9` | Manually reset a password in the database | 551 | N/A | N/A | MC server/keystore password UI; use Kubernetes secrets / ASP.NET configuration (scope-vs-mirth.md) |
| `db8e077c1348` | Upgrade Guide | 553 | — | In progress | — |
| `2813d5cc3e3f` | Before You Upgrade | 553 | — | In progress | — |
| `b42b0b77ee47` | Upgrade Mirth Connect | 554 | — | In progress | — |
| `0e22fb8644b9` | After the Upgrade | 554 | — | In progress | — |
| `4a16a883be3a` | Version-Specific Upgrade Instructions | 555 | — | In progress | — |
| `22e00d0a5268` | 4.5.0 Upgrade Notes | 555 | — | In progress | — |
| `bd6fd013e268` | Updated DUO Authentication to use the Universal Prompt | 555 | — | In progress | — |
| `b266af7e2a18` | Removed Libraries | 555 | — | In progress | — |
| `65e5f06cdf17` | Change of Functionality of "Generate Envelope" in the Web Service Sender | 556 | — | In progress | — |
| `f6e26e7a286d` | Health Data Hub Plugin | 557 | — | In progress | — |
| `1d1889921d4d` | 4.4.2 Upgrade Notes | 557 | — | In progress | — |
| `c1c1010a902d` | Added a Readme File to Show Valid XML Type Message Export Information | 557 | — | In progress | — |
| `024845b529ab` | 4.4.1 Upgrade Notes | 557 | — | In progress | — |
| `3634a58a6a13` | XStream Now Uses an Allowlist Instead of a Denylist | 557 | — | In progress | — |
| `a7320a63873f` | 4.4.0 Upgrade Notes | 558 | — | In progress | — |
| `429827297f27` | Added New Functionality to the Mirth® Connect Setup Wizard (Installation Process) | 558 | — | In progress | — |
| `5d88c00ecea0` | Default Digest Algorithm Changed | 558 | — | In progress | — |
| `65780dac54aa` | Updating the Digest Algorithm | 559 | — | In progress | — |
| `4443cb5f49e0` | 4.3.0 Upgrade Notes | 559 | — | In progress | — |
| `19c10c4f0022` | New Functionality for the Mirth® Connect Setup Wizard | 559 | — | In progress | — |
| `4d673e2820e8` | Resource Classloaders Load Classes Child-First By Default | 560 | — | In progress | — |
| `d99c6c28b00d` | Updated Deprecated Docker Base Images | 560 | — | In progress | — |
| `416df2618ad5` | Updated Encryption Settings | 560 | — | In progress | — |
| `808adc18011e` | Disabled TLS Cipher Suites | 561 | — | In progress | — |
| `869e631d1fc2` | Removed the View User Guide Option | 562 | — | In progress | — |
| `06bbb66b9019` | Administrator Launcher | 562 | N/A | N/A | No Java Swing Administrator; REST management API + static operator shell (scope-vs-mirth.md) |
| `471d4d898c29` | 4.2.0 Upgrade Notes | 563 | — | In progress | — |
| `fcc2ab28c8f4` | Removed the Remove All Events Button | 563 | — | In progress | — |
| `3ab717ec029e` | Changed the patient_id Query Parameter Naming Scheme for the Remove Message API Request | 563 | — | In progress | — |
| `b005e18a50f5` | Renamed the User Authorization Extension to Role-Based Access Control | 563 | N/A | N/A | MC commercial RBAC extension; use platform IdP + optional JWT (ManagementSecurityExtensions.cs; scope-vs-mirth.md) |
| `22339b2c9f89` | Moving Java Home Preference Checks | 563 | — | In progress | — |
| `2d09ea0ceab9` | 4.1.0 Upgrade Notes | 564 | — | In progress | — |
| `4b651810fd69` | Warning: Open issue with SMB File Reader/Writer | 564 | — | In progress | — |
| `5171600cef33` | Update log4j Library | 564 | — | In progress | — |
| `3b20cb81bd4c` | 4.0.0 Upgrade Notes | 564 | — | In progress | — |
| `526a709011db` | Database Reader XML Casing | 564 | — | In progress | — |
| `f655f3e644c3` | TLS Protocols and Cipher Suites | 565 | — | In progress | — |
| `47942deb5773` | TLS Protocols | 565 | — | In progress | — |
| `0c6bfe57523b` | Cipher Suites | 565 | — | In progress | — |
| `fcfd93ab30a7` | Impact | 566 | — | In progress | — |
| `2b998b66a8f3` | Resolution | 566 | — | In progress | — |
| `66f9803491f3` | SSL Manager Options | 566 | — | In progress | — |
| `fbc9f571cccd` | Server Wide Options | 566 | — | In progress | — |
| `8b3d8434ffe7` | HTTP User Agent | 566 | — | In progress | — |
| `f0dbd8864611` | HTTP Server Header | 567 | — | In progress | — |
| `e5bb08dc82c9` | FHIR Extension | 567 | — | In progress | — |
| `f36c092134d8` | 3.12.0 Upgrade Notes | 567 | — | In progress | — |
| `1d21d604eeb4` | Preventing XML External Entity Vulnerabilities | 567 | — | In progress | — |
| `497f4a324af6` | How this can affect you | 567 | — | In progress | — |
| `38e025a35a2c` | PDF Generation Updates | 567 | — | In progress | — |
| `56b5f595e9d8` | Using PDFBox Classes | 568 | — | In progress | — |
| `155ddf3e34f9` | PDF Generation and Images | 568 | — | In progress | — |
| `8aed8bc5f47d` | 3.11.0 Upgrade Notes | 568 | — | In progress | — |
| `fb4c9a119839` | Database Connection Retries on Server Startup | 568 | — | In progress | — |
| `63ca8bcb3706` | Interoperability Connector Suite | 569 | — | In progress | — |
| `2fde9791dd9f` | Logging Raw SOAP Payloads and WS-Security Details | 569 | — | In progress | — |
| `c6a6bb7e2d3b` | Toggling Automatic Conversion to JSON | 569 | — | In progress | — |
| `5ace9dd04500` | 3.10.0 Upgrade Notes | 570 | — | In progress | — |
| `279545f2881f` | Advanced Clustering Sync Intervals | 570 | N/A | N/A | MC clustering sync UI; not applicable (scope-vs-mirth.md) |
| `8c9aba4d25af` | 3.9.0 Upgrade Notes | 571 | — | In progress | — |
| `742b9ccf3595` | SMB Versions in a File Reader/Writer | 571 | — | In progress | — |
| `70e2e929c0f9` | DICOM Sender Storage Commitment | 571 | — | In progress | — |
| `f27e70d5854f` | 3.8.0 Upgrade Notes | 571 | — | In progress | — |
| `23976638e4c8` | Connecting via a Database Reader/Writer or in JavaScript | 571 | — | In progress | — |
| `3c45d5cbf3e8` | Using an Old Version of MySQL for Your Connect Backend Database | 572 | — | In progress | — |
| `13aa0812a805` | 3.7.0 Upgrade Notes | 572 | — | In progress | — |
| `3404e9056439` | Commercial License Key | 572 | — | In progress | — |
| `469e8a922ec0` | Using Mirth Appliance | 572 | — | In progress | — |
| `cb47d401996a` | Standalone Installation | 573 | — | In progress | — |
| `388f8023bf37` | Database Connection Pools | 573 | — | In progress | — |
| `3b3821c2b6d7` | Option 1: Use two pools, one read/write and one read-only | 574 | — | In progress | — |
| `e23eec479d93` | Option 2: Use one connection pool for everything | 574 | — | In progress | — |
| `cb66d8d8674e` | Keystore Passwords | 575 | N/A | N/A | MC server/keystore password UI; use Kubernetes secrets / ASP.NET configuration (scope-vs-mirth.md) |
| `3b6861a67fca` | 3.5.0 Upgrade Notes | 575 | — | In progress | — |
| `7b35c51bae06` | 3.4.0 Upgrade Notes | 576 | — | In progress | — |
| `1173bb9b2cfa` | 3.2.0 Upgrade Notes | 577 | — | In progress | — |
| `bd010aed7117` | 3.1.1 Upgrade Notes | 578 | — | In progress | — |
| `f0b5911c310a` | 3.1.0 Upgrade Notes | 578 | — | In progress | — |
| `0fd1f78d0f0a` | 3.0.2 Upgrade Notes | 579 | — | In progress | — |
| `299a0154645c` | Channel Updates | 579 | — | In progress | — |
| `678388f330ad` | 3.0.0 Upgrade Notes | 579 | — | In progress | — |
| `878d947c78c3` | Message Migration | 579 | — | In progress | — |
| `6b480dc9d2bb` | For Non-Polling Channels | 580 | — | In progress | — |
| `51d64b8819d3` | For Polling Channels | 580 | — | In progress | — |
| `ca2619506849` | Channel Updates | 580 | — | In progress | — |
| `799a49bda76d` | Commercial Support/Extensions | 582 | N/A | N/A | Commercial support/extensions marketing; not SmartConnect core backlog |
| `8c24dedf8572` | Advanced Alerting | 583 | N/A | N/A | No MC Advanced Alerting; use platform metrics/alerting (scope-vs-mirth.md) |
| `7453863f9f44` | Advanced Clustering | 584 | N/A | N/A | MC cluster control plane; scaling/HA is Kubernetes / host concern (scope-vs-mirth.md) |
| `d19f2efda9df` | ASTM E1381 Transmission Mode | 585 | N/A | N/A | ASTM E1381 framing not in core; use TCP listener with custom delimiter or host adapter |
| `8bdffc727247` | ASTM E1394 Data Type | 586 | N/A | N/A | ASTM E1394 datatype plugin not in core; use raw TCP or custom transform |
| `c7ca9d757744` | Channel History | 587 | N/A | N/A | No Administrator channel history view; ledger + audit APIs (scope-vs-mirth.md) |
| `a1cb41039be9` | Cures Certification Support | 588 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `94c3be236d8a` | Email Reader | 588 | N/A | N/A | No Email Reader; commercial MC extension |
| `4d7068ec34d9` | Enhancement Bundle | 589 | N/A | N/A | MC Enhancement Bundle; not applicable to SmartConnect core distribution |
| `5aceef4e86ba` | FHIR Connector | 590 | N/A | N/A | No FHIR connector in core; use HTTP + FHIR library in host |
| `0d35ad441457` | Interoperability Connector Suite | 592 | N/A | N/A | Vendor interoperability bundle; explicit HTTP/TCP/DB adapters in core instead |
| `2adddd589355` | LDAP Authorization | 594 | N/A | N/A | LDAP authorization: use platform directory integration |
| `968b1ad9d9ed` | Message Generator | 595 | N/A | N/A | No MC Message Generator connector in core |
| `5711b375af90` | Multi-Factor Authentication | 596 | N/A | N/A | MFA: delegated to platform IdP / ASP.NET Core auth |
| `ced80a6f5489` | Serial Connector | 597 | N/A | N/A | No serial connector in core |
| `45bddecf8523` | SSL Manager | 598 | N/A | N/A | SSL/TLS: configure at reverse proxy or Kestrel; no MC SSL Manager UI |
| `6b42a915721a` | Role-Based Access Control | 599 | N/A | N/A | MC commercial RBAC extension; use platform IdP + optional JWT (ManagementSecurityExtensions.cs; scope-vs-mirth.md) |
| `ccce3ca312f3` | NextGen Results CDR Connector | 600 | N/A | N/A | Vendor NextGen Results CDR connector; not in core |
| `38b645cc5b7b` | Training | 602 | N/A | N/A | Training / commercial marketing prose; not SmartConnect implementation backlog |
| `c1c2e605a9ae` | Cures Certification | 603 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `8117ba92ed20` | Summary | 604 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `94c7b0366be0` | §170.315(b)(10) Electronic Health Information (EHI) Export | 605 | N/A | N/A | EHI export certification: not implemented in SmartConnect; product-specific |
| `fa0bb554c046` | Required Extensions: | 605 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `923f471a5bdf` | Features that Support the Certification | 605 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `fd06eab9ce92` | Required Actions | 606 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `9ff3f6fa99d9` | Single Patient Export | 606 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `2f3a0d7c63b5` | Multi-Patient Export | 606 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `c5de3c63318a` | Viewing Exported Attachments | 606 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `865840ed47b0` | §170.315(d)(1) Authentication, Access Control, Authorization | 607 | N/A | N/A | Authentication certification: use ASP.NET Core / platform IdP |
| `2fee2b113006` | Required Extensions | 607 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `44e72512ab64` | Features that Support the Certification | 607 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `8db0a7c43270` | Required Actions | 607 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `5ad5b9956070` | §170.315(d)(2) Auditable Events and Tamper-Resistance, §170.315(d)(3) Audit Report(s), and §170.315(d)(10) Auditing Actions on Health Information | 607 | N/A | N/A | Audit event store (EfAuditEventStore) + APIs; not ONC tamper-evident bundle |
| `39ee26d5031b` | Required Extensions | 608 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `231eb3985709` | Features that Support the Certification | 608 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `2c057f56be50` | Required Actions | 609 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `9e2900e11e26` | §170.315(d)(5) Automatic Access Time-out | 609 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `e7809839e541` | Required Extensions | 609 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `21450768fd6a` | Features that Support the Certification | 609 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `2c057f56be50` | Required Actions | 609 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `ba3aa6ccc73f` | §170.315(d)(6) Emergency Access | 609 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `8e206c9f802c` | Required Extensions | 610 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `95b4e01c9e26` | Features that Support the Certification | 610 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `290159c8194e` | Required Actions | 610 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `1d51124eac25` | §170.315(d)(7) End-User Device Encryption | 616 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `e15e39991af5` | Required Extensions | 616 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `efc2de89d9ef` | Features that Support the Certification | 616 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `88a2feb6de9b` | Required Actions | 616 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `b94db87ac8ae` | §170.315(d)(8) Integrity | 616 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `e15e39991af5` | Required Extensions | 616 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `efc2de89d9ef` | Features that Support the Certification | 616 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `e6ad57dc1169` | Required Actions | 617 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `afb899319a24` | §170.315(d)(9) Trusted Connection | 617 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `d871138dbe01` | Required Extensions | 617 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `dc33e7401238` | Features that Support the Certification | 617 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `e6ad57dc1169` | Required Actions | 617 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `84a8ab57ffa2` | §170.315(d)(12) Encrypt Authentication Credentials | 617 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `659b4f586fdc` | Required Extensions | 618 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `418d1b69f183` | Features that Support the Certification | 618 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `7b0197c613b2` | Required Actions | 618 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `249db375a76d` | §170.315(d)(13) Multi-Factor Authentication | 618 | N/A | N/A | ONC MFA criterion: host product responsibility |
| `659b4f586fdc` | Required Extensions | 618 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `418d1b69f183` | Features that Support the Certification | 618 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `7b0197c613b2` | Required Actions | 618 | N/A | N/A | ONC / certification appendix: not a SmartConnect deliverable; host product scope (docs/smartconnect/scope-vs-mirth.md) |
| `ad5275427813` | §170.315(g)(4) Quality Management System | 619 | N/A | N/A | ONC QMS: out of scope for SmartConnect core; host application lifecycle |
| `0985d957e195` | §170.315(g)(5) Accessibility-Centered Design | 619 | N/A | N/A | Accessibility-centered design: delegated to host UI standards |
