# Cursor Rules

These rules provide persistent AI guidance for the Dialysis PDMS project. They are sourced from the `rules/` directory and applied by the Cursor agent.

For the mapping between Cursor rules, `.editorconfig`, and build enforcement (EnforceCodeStyleInBuild, SonarAnalyzer), see [docs/RULES-ALIGNMENT.md](../docs/RULES-ALIGNMENT.md).

## Rule Categories

### Always-Apply Rules
- **project-goal** – Learning platform: healthcare systems, dialysis, FHIR, .NET. Theory first, then learn by doing
- **c5-compliance** – C5 (BSI Cloud Computing Compliance); mandatory for all changes
- **code-quality** – Behavioral guidelines (verify info, no apologies, file-by-file, etc.)
- **general-coding-rules** – Variable names, performance, security, error handling, testing
- **learn-by-doing-workflow** – Plan (workflows/Mermaid) → Implement (WIKI docs) → Explain (architecture updates)

### C# / Sonar (globs: `**/*.cs`)
- **sonar-csharp** – SonarQube C# code quality (unused code, exceptions, async, disposal)
- **cognitive-complexity-s3776** – Method complexity threshold, extraction guidance
- **parameter-count-s107** – Maximum 7 parameters per method; use records/DTOs or split
- **sonar-security** – Secrets, SQL injection, path traversal, deserialization, crypto

### Architecture (globs vary)
- **api-versioning** – URL path versioning `api/v1/...`
- **data-persistence** – Redis + EF Core + PostgreSQL, cache-aside
- **intercessor** – CQRS, commands, queries, events
- **multi-tenancy** – X-Tenant-Id, per-tenant DBs
- **verifier** – FluentValidation for commands/queries
- **mirth-integration** – Mirth = integration engine; PDMS = domain + FHIR

### Code Style & Clean Code
- **clean-code** – Constants, meaningful names, DRY, single responsibility
- **naming-conventions** – Descriptive names
- **general-code-style-and-readability** – Readability, refactoring
- **function-length-and-responsibility** – Single responsibility
- **dry-principle** – Don't repeat yourself
- **conditional-encapsulation** – Extract nested conditionals
- **comment-usage** – Meaningful comments
- **code-writing-standards** – Language conventions

### Documentation & Git
- **docs-how-to** – How-To documentation for non-technical users
- **conventional-commits** – Conventional Commit Messages specification
