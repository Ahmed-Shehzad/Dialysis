# SmartConnect response transforms — operator guide

**Slice F** of the SmartConnect ↔ Mirth alignment plan. Covers the **Response
Transformers** section of the Mirth Connect User Guide (pp. 286–288) and gives
operators canonical recipes for the three patterns the guide names. The
underlying runtime has supported response transforms since the original flow
engine landed; this doc just makes the patterns explicit so flow authors
don't have to reinvent them.

## Where response transforms run

A response transform runs **after** an outbound route's `SendAsync` returns
**successfully** and the response carries a non-empty payload. The
[`FlowRuntimeEngine`](../../src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs)
flow is:

1. Source receives the message → ledger row `Received`.
2. Pre-processor + global filters + transforms run → ledger rows for each stage.
3. For each outbound route (parallel or sequential):
   1. Route filter evaluates → `OutboundFiltered` if rejected.
   2. Route-level transforms run → `OutboundTransformed`.
   3. `IOutboundAdapter.SendAsync` returns `OutboundSendResult`.
   4. **If the route enabled `ConnectorProperties.CaptureResponseBody` and the
      `ResponsePayload` is non-empty**, response transforms run, in order, with
      the response bytes as their input payload → `OutboundSent`.
4. The first response payload (raw or transformed) is propagated back to the
   caller's `FlowDispatchResult.ResponsePayload`.

> ⚠️ Response transforms only fire when `ConnectorProperties.CaptureResponseBody`
> is `true` (slice B, PR #59). Without that, the adapter returns `null` for
> `ResponsePayload` and the flow runtime skips the response-transform stage.

## Pattern 1 — Parse ACK and decide success / failure

The partner returns an HL7 v2 ACK; we want to mark our ledger row as failed if
`MSA-1` is `AE` or `AR`, even though the HTTP layer reported `200 OK`.

**Pipeline definition (excerpt)**

```json
{
  "OutboundRoutes": [{
    "OutboundAdapterKind": "http",
    "OutboundParametersJson": {
      "Url": "https://partner.example/api/lab-order",
      "ConnectorProperties": { "CaptureResponseBody": true }
    },
    "ResponseTransformStages": [{
      "Kind": "javascript",
      "ParametersJson": "{\"script\":\"const ack = parseHL7v2(msg.payloadString); const code = ack['MSA-1']; if (code === 'AE' || code === 'AR') { msg.metadata['smartconnect.ack.error'] = code; } return msg;\"}"
    }]
  }]
}
```

The transform writes `smartconnect.ack.error` to the message metadata when the
partner negatives the ACK. Slice C (PR #60) persists that metadata on the
ledger row, so the operator dashboard can surface failed acknowledgements as
a chip on the **Messages** tab.

## Pattern 2 — Force a queueing message to error if attempts exceed a threshold

When the partner keeps replying `AE` (transient application error), the flow
runtime's outbound retry (slice B's `MaxRetries`) eventually gives up. The
response transform is the right place to **escalate** that final failure into
the alert pipeline rather than silently dead-lettering.

**Pipeline definition (excerpt)**

```json
{
  "OutboundRoutes": [{
    "OutboundAdapterKind": "http",
    "OutboundParametersJson": {
      "Url": "https://partner.example/api/lab-order",
      "ConnectorProperties": {
        "CaptureResponseBody": true,
        "MaxRetries": 5,
        "RetryDelayMs": 1000
      }
    },
    "ResponseTransformStages": [{
      "Kind": "javascript",
      "ParametersJson": "{\"script\":\"const ack = parseHL7v2(msg.payloadString); if (ack['MSA-1'] === 'AE') { msg.metadata['smartconnect.escalate'] = 'ack-error-after-retries'; } return msg;\"}"
    }]
  }]
}
```

The matching alert rule (configured in the operator-shell **Alerts** tab) fires
on the `smartconnect.escalate` metadata key and routes to email / webhook /
channel-redispatch. The runtime exhausts `MaxRetries` before the response
transform runs on the final attempt's response payload, so the escalation
metadata only ever lands once per terminal failure.

## Pattern 3 — Route the response data to a downstream channel

The lab partner's ACK carries the assigned accession number; we want to feed
that into a separate "lab-followup" channel so the EHR can poll for results
keyed by accession.

**Pipeline definition (excerpt)**

```json
{
  "OutboundRoutes": [{
    "OutboundAdapterKind": "http",
    "OutboundParametersJson": {
      "Url": "https://partner.example/api/lab-order",
      "ConnectorProperties": { "CaptureResponseBody": true }
    },
    "ResponseTransformStages": [{
      "Kind": "javascript",
      "ParametersJson": "{\"script\":\"const ack = parseHL7v2(msg.payloadString); msg.metadata['lab.accession'] = ack['MSA-3'] || ''; return msg;\"}"
    }]
  }, {
    "OutboundAdapterKind": "channel-writer",
    "OutboundParametersJson": {
      "TargetFlowId": "f0000000-0000-0000-0000-0000000feed1",
      "MetadataPropagation": 0
    }
  }]
}
```

The first route POSTs the lab order and parses the ACK into metadata. The
second route is a `channel-writer` that hands the (now metadata-enriched)
message to the downstream `lab-followup` flow, where it lands as an inbound
message keyed by `lab.accession`.

> The `channel-writer` adapter (`Dialysis.SmartConnect.Core/ExtendedPlugins/ChannelWriterOutboundAdapter.cs`)
> increments a per-message recursion depth counter to prevent infinite loops
> if a downstream flow tries to fan back to the source.

## Common pitfalls

- **Forgetting `CaptureResponseBody`** — without it, `ResponsePayload` is
  always `null` and response transforms silently skip. The
  `HttpOutboundAdapterConnectorPropertiesTests.Adapter_Returns_Empty_Response_Payload_When_Capture_Disabled_Async`
  test pins this contract.
- **Modifying the request payload from inside a response transform** — the
  response transform sees a *clone* of the original message with the response
  bytes as `Payload`. Writes to `msg.payload` only affect the response leg;
  the request payload stored in the ledger is untouched.
- **Exception in a response transform** — bubbles up and marks the route as
  `OutboundError` in the ledger. The alert engine fires on that status the
  same as any other downstream failure.

## Cross-references

| Concern | Code path |
|---|---|
| Response transform dispatch | [`FlowRuntimeEngine.cs:287`](../../src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs#L287) |
| Pipeline shape | [`IntegrationFlowPipelineDefinition.cs:65`](../../src/backend/SmartConnect/Dialysis.SmartConnect.Core.Abstraction/IntegrationFlowPipelineDefinition.cs#L65) |
| `CaptureResponseBody` toggle | [`HttpOutboundParameters.cs`](../../src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/HttpOutboundParameters.cs) (slice B) |
| Metadata persistence on the ledger | [`EfMessageLedger.cs`](../../src/backend/SmartConnect/Persistence/Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Abstractions/EfMessageLedger.cs) (slice C) |
| Channel-Writer outbound | [`ChannelWriterOutboundAdapter.cs`](../../src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/ChannelWriterOutboundAdapter.cs) |

## Traceability

Closes the six "In progress" Mirth UG rows in
[`guide-traceability.md`](guide-traceability.md):

| Row id | Mirth section | Coverage |
|---|---|---|
| `d9edaa4d4de8` | Response Transformers | this doc + `FlowRuntimeEngine.cs:287` |
| `6e0704439eda` | Modifying the Response | this doc, Pattern 1 |
| `e6df3c9fba0e` | Common Scenarios | this doc — § Common pitfalls |
| `8b8e13cf98bb` | Re-queue a message if the HL7 ACK has an AE code | this doc, Pattern 1 |
| `7d8754af2903` | Force a queuing message to error if the number of send attempts exceeds some threshold | this doc, Pattern 2 |
| `d75a3391beea` | Route the response data to a downstream channel | this doc, Pattern 3 |
