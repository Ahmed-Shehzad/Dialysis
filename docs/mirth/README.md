# Mirth Connect Integration

This guide explains how to integrate Mirth Connect with the Dialysis PDMS to route HL7 v2 messages (ORU, ADT) into the REST API.

---

## Overview

```
[HL7 Source]  →  Mirth Channel  →  HTTP Sender  →  PDMS POST /api/v1/hl7/stream
```

The PDMS accepts raw HL7 v2 messages at `POST /api/v1/hl7/stream` with a JSON body. Supported message types: **ORU^R01** (vitals/lab), **ADT^A04** (register patient), **ADT^A08** (update patient).

```json
{ "rawMessage": "MSH|^~\\&|SENDER|FACILITY|..." }
```

Mirth receives HL7 from devices, LIS, or ADT, and forwards it to the PDMS via HTTP.

---

## Prerequisites

- Mirth Connect 4.x or 3.x
- PDMS Gateway URL (e.g. `http://localhost:5000` or your deployed URL)
- Tenant ID for multi-tenancy (default: `default`)

---

## Channel Configuration

### 1. Create a New Channel

1. In Mirth Connect Admin, go to **Channels** → **Create Channel**
2. **Name**: `HL7 to Dialysis PDMS`
3. **Channel ID**: (auto-generated or custom, e.g. `hl7-to-pdms`)

### 2. Source Connector – HL7 Listener

1. **Connector Type**: **TCP Listener**
2. **Settings**:
   - **Port**: e.g. `6661` (or your facility’s HL7 port)
   - **Accept HL7 Encoding**: Yes
3. This receives HL7 ORU/ADT from devices, LIS, or upstream systems.

### 3. Destination Connector – HTTP Sender (PDMS)

1. **Connector Type**: **HTTP Sender**
2. **URL**: `{PDMS_BASE_URL}/api/v1/hl7/stream`
   - Example: `http://localhost:5000/api/v1/hl7/stream`
   - Production: `https://pdms.your-facility.com/api/v1/hl7/stream`
3. **Method**: `POST`
4. **Content Type**: `application/json`
5. **Headers**:
   - `X-Tenant-Id`: `default` (or your tenant ID)

### 4. Transformer – Build JSON Payload

In the **Destination** transformer for the HTTP connector:

**Step 1 – Get raw HL7**
```javascript
// Raw HL7 message from the source
var raw = messageObject.getRawData();
```

**Step 2 – Build JSON for PDMS**
```javascript
// PDMS expects: { "rawMessage": "MSH|^~\&|..." }
var payload = {
  rawMessage: raw
};
channelMap.put('requestBody', JSON.stringify(payload));
```

**Step 3 – Use in HTTP connector**

- In the HTTP Sender **Body** setting, select **Map**
- Map variable: `requestBody`

Or use **Template** and reference:
```
${requestBody}
```

---

## Complete Destination Transformer Script

Paste this into the destination’s **Transform Outbound Message** step:

```javascript
// Get raw HL7
var raw = messageObject.getRawData();

// PDMS API expects JSON: { "rawMessage": "MSH|..." }
var payload = {
  rawMessage: raw
};

// Store for HTTP body
channelMap.put('requestBody', JSON.stringify(payload));

// Return the JSON string for the HTTP body
return JSON.stringify(payload);
```

In the HTTP Sender:
- **Template**: Use `${message}` if the transformer returns the final payload, or reference `requestBody` via channelMap as per your Mirth version.
- If using **Map** for the body, set it to `requestBody` from the channel map.

---

## HTTP Sender Settings Summary

| Setting      | Value                              |
|-------------|-------------------------------------|
| URL         | `http://localhost:5000/api/v1/hl7/stream` |
| Method      | POST                               |
| Content Type| application/json                   |
| Headers     | X-Tenant-Id: default               |
| Body        | Map: requestBody (or Template with ${requestBody}) |

---

## Example HL7 ORU (Vitals)

```
MSH|^~\&|VITALS|FACILITY|PDMS|FACILITY|20250217120000||ORU^R01|MSG001|P|2.5
PID|||PAT123^^^FACILITY^MR||Doe^John^^^Mr||19900115|M
OBR|1|||85354-9^Blood pressure^LN|||||||||||||||F
OBX|1|NM|85354-9^Blood pressure^LN||95|mmHg^^UCUM|||||F
```

This creates a blood pressure observation (LOINC 85354-9) for patient `PAT123`.

---

## Multi-Tenancy

Always send the `X-Tenant-Id` header from Mirth so the PDMS can isolate data per tenant.

Example for multiple facilities:
- Facility A: `X-Tenant-Id: facility-a`
- Facility B: `X-Tenant-Id: facility-b`

You can set this per channel or per destination using channel variables (e.g. from HL7 MSH-3/MSH-4).

---

## Error Handling

- **2xx**: PDMS accepted the message.
- **400**: Bad request (e.g. empty or invalid `rawMessage`).
- **5xx**: Server error; retry using Mirth’s built-in retry.

Recommended: enable **Respond from destination** in Mirth and check HTTP status; configure retries and a dead letter queue for 5xx. Set **Retry count: 3**, **Retry interval: 10000 ms** in the HTTP Sender. Enable destination queue for DLQ on max retries. The PDMS does not support idempotency keys; deduplicate by MSH-10 in Mirth if needed.

---

## Example Files

- **example-channel-hl7-to-pdms.xml** – Reference config showing key settings and transformer script. Create the channel manually per the steps above; Mirth’s export format varies by version.

## Docker Compose (Optional)

To run Mirth alongside the PDMS locally:

```yaml
services:
  mirth:
    image: nextgenhealthcare/connect:latest
    ports:
      - "8080:8080"   # Mirth Admin
      - "6661:6661"   # HL7 listener
    environment:
      - MIRTH_ADMIN_USER=admin
      - MIRTH_ADMIN_PASSWORD=mirth
  pdms:
    # Your PDMS Gateway service
    ...
```

Use Mirth Admin at `http://localhost:8080` to create the channel and test.

---

## References

- [ECOSYSTEM-PLAN.md](../ECOSYSTEM-PLAN.md) – Phase 1.1.3 (Mirth config)
- [GETTING-STARTED.md](../GETTING-STARTED.md) – PDMS setup
- Mirth Connect: [HTTP Connector docs](https://github.com/nextgenhealthcare/connect)
