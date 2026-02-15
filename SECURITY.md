# Security

## Reporting Vulnerabilities

If you discover a security vulnerability, please report it responsibly:

1. **Do not** open a public GitHub issue.
2. Email security concerns to your organization's security contact.
3. Include a detailed description, steps to reproduce, and potential impact.

## Security Practices

Dialysis PDMS follows C5 (BSI Cloud Computing Compliance Criteria Catalogue). See [docs/C5-COMPLIANCE.md](docs/C5-COMPLIANCE.md) for alignment details.

### Configuration

- **Secrets**: Use Azure Key Vault, Kubernetes Secrets, or environment variables. Never commit connection strings or API keys to the repository.
- **Connection strings**: `ServiceBus:ConnectionString`, database connection strings, and OIDC credentials are read from configuration at runtime.
- **Health endpoints**: `/health` is unauthenticated for load balancer probes; do not expose sensitive data there.

### Authentication & Authorization

- JWT Bearer with OIDC (Azure AD, Keycloak, Auth0).
- Scope-based policies: `dialysis.read`, `dialysis.write`, `dialysis.admin`.
- See [docs/PRODUCTION-CONFIG.md](docs/PRODUCTION-CONFIG.md) for IdP setup.

### Code Review Checklist

- [x] No hardcoded credentials or API keys – config/env vars; optional Azure Key Vault via `Dialysis.Configuration.AddKeyVaultIfConfigured()`
- [x] Sensitive config validated in production (fail fast if missing) – `ValidateProductionConfig()` enforces Auth in Production
- [x] Audit logging for sensitive operations – `Dialysis.AuditConsent` records `AuditEvent` per tenant
- [x] Tenant isolation enforced (`X-Tenant-Id`) – `TenantResolutionMiddleware` in Alerting, AuditConsent, DeviceIngestion, HisIntegration; per-tenant PostgreSQL and Redis keys
