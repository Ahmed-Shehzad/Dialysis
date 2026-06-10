# Security Policy

Dialysis is a clinical platform handling PHI (HIPAA) and EU personal data (GDPR/BDSG).
Security reports are taken seriously and handled with priority.

## Reporting a vulnerability

**Do not open a public issue for security findings.**

- Use GitHub's **private vulnerability reporting** ("Report a vulnerability" under the
  repository's Security tab), or
- Email the maintainer (see the repository owner's profile) with `[SECURITY]` in the subject.

Include: affected component (module/BFF/gateway/SPA/workflow), reproduction steps or PoC,
impact assessment (especially any PHI exposure path), and suggested remediation if known.

You can expect an acknowledgement within **72 hours** and a triage verdict within **7 days**.
Please allow a coordinated disclosure window of **90 days** (shorter by agreement once a fix
ships) before publishing details.

## Scope

In scope: everything in this repository — module APIs, BFFs, the YARP gateway, the seven SPAs,
Keycloak realm configuration, deployment artifacts (`deploy/`), CI workflows, and the build
tooling. Especially interesting: authentication/session handling (BFF cookies, token refresh,
`kc_idp_hint` flows), cross-module authorization (permission catalogs, patient-claim filtering),
PHI egress paths (FHIR endpoints, exports, logs), and the GDPR data-subject-rights surfaces.

Out of scope: vulnerabilities requiring a compromised host, social engineering, and findings in
third-party dependencies with no exploitable path in this codebase (report those upstream — our
weekly `security.yml` scans and Dependabot track them).

## Existing controls (for researchers' orientation)

- CI: dependency audits (NuGet + npm), Trivy filesystem scan, OWASP ZAP baseline against the
  live stack, GitGuardian secret scanning (`.github/workflows/security.yml`).
- Runtime: per-context BFF session isolation, JWT-only module APIs, fail-fast guards for
  missing CORS origins and placeholder OIDC client secrets outside Development, HIPAA audit
  pipeline (`[PhiAccess]` → FHIR `AuditEvent`), column-level PHI encryption (`IPhiProtector`).

## Supported versions

The `main` branch and the latest tagged release receive security fixes. Older tags are not
patched — upgrade forward.
