# Mirth Channel Samples for Dialysis PDMS

This folder contains reusable scripts and configuration for Mirth Connect channels that route HL7 messages to the Dialysis PDMS.

## Import Steps

1. Create a new channel in Mirth Connect (Channels → New Channel).
2. Configure the **Source Connector** (LLP Listener) – port, e.g. 6661.
3. Add a **Destination** connector (HTTP Sender).
4. In the destination transformer, paste the script from `transformer-routing.js`.
5. Configure channel variables: `pdmsBaseUrl`, `patientBaseUrl`, etc. (or use gateway URL for all).
6. Save and deploy.

To **import** an existing Mirth channel XML: Mirth Connect → Channels → Import Channel → select `.xml` file.

## Files

| File | Description |
|------|-------------|
| `transformer-routing.js` | JavaScript transformer: parses MSH-9, builds JSON body, sets target URL |
| `channel-variables.json` | Suggested channel variables (PDMS base URLs, tenant) |

## Gateway URL

When using Dialysis.Gateway, set `pdmsBaseUrl` to `http://localhost:5001` so all messages route through a single host.
