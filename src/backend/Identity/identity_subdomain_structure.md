# Identity — Subdomain structure (Large-Scale Structure)

This document records Identity's **Large-Scale Structure pattern** per Eric Evans, *Domain-Driven Design* (2003), pp. 307–337.

## No large-scale structure

Identity is a **Generic Subdomain** (Evans, p. 281). Evans' explicit guidance for generic subdomains is to keep them thin and avoid deep modeling effort. Identity has exactly one operational slice (`Provisioning`) and one auth gateway (`Bff`), both standing on a standard OIDC broker (Keycloak in dev).

There is therefore no chosen Large-Scale Structure pattern for this module. The minimal structure is:

```
┌────────────────────────────────────────────┐
│ BFF (browser OIDC entry)  Identity.Bff     │
└────────────────────────────────────────────┘
                  ↓ (OIDC code-flow + JWT cookie)
┌────────────────────────────────────────────┐
│ Provisioning API          Identity.Api     │
│ Local user provisioning + role assignment. │
└────────────────────────────────────────────┘
                  ↓ (publishes events)
┌────────────────────────────────────────────┐
│ Integration-event contracts                │
│ Identity.Contracts/Integration             │
└────────────────────────────────────────────┘
```

## Why no Responsibility Layers / Metaphor / Knowledge Level?

- Responsibility Layers: there are no strata to layer.
- System Metaphor: would imply richer modeling than the subdomain warrants.
- Knowledge Level: there are no rules about rules; role names are opaque strings owned by the IdP.
- Pluggable Component Framework: the only "pluggable" element is the IdP itself, swapped via configuration; that is a build-time decision, not a runtime framework.

## Replaceability

The whole module is intentionally replaceable. The published-language contract is OIDC (claims `sub`, `email`, `roles`, `his_patient_id`, etc.) plus the integration events. Any IdP that can issue equivalent claims is a drop-in.
