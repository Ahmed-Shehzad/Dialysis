# Rules Alignment

This document maps Cursor rules, .editorconfig, and build enforcement so that all rules are applied correctly.

## Build Enforcement

| Setting | Value | Purpose |
|---------|-------|---------|
| `EnableNETAnalyzers` | true | Run .NET analyzers during build |
| `EnforceCodeStyleInBuild` | true | Treat code style violations as build errors/warnings |
| `AnalysisLevel` | latest-recommended | Use latest recommended analyzer rules |
| `SonarAnalyzer.CSharp` | PackageReference | Enforces Sonar S#### rules |

## .editorconfig ↔ Cursor Rules

### Sonar Rules (S####) – require `SonarAnalyzer.CSharp` package

| Rule | .editorconfig | Cursor Rule | Severity |
|------|---------------|-------------|----------|
| S107 | ✓ | parameter-count-s107, sonar-csharp | warning |
| S1135 | ✓ | sonar-csharp | warning (suggestion in tests) |
| S125 | ✓ | sonar-csharp | suggestion (none in tests) |
| S1144 | ✓ | sonar-csharp | suggestion (none in tests) |
| S3776 | ✓ | cognitive-complexity-s3776, sonar-csharp | warning (suggestion in tests) |

### IDE / Built-in (IDE####, CS####)

Configured in `.editorconfig`; no extra package needed. Includes: IDE0005, IDE0051, IDE0052, IDE0058, IDE0059, IDE0060, IDE0011, IDE0055, CS0168, CS0219, CS0414.

### Naming Rules

- **Async suffix** (dotnet_naming_rule): Async methods must end with `Async` – error (all files).

### Formatting (dotnet format)

Run `dotnet format` to enforce: trim_trailing_whitespace, insert_final_newline, charset (utf-8), indent_style, and remove unnecessary usings (IDE0005).

## Cursor Rules (AI Guidance)

Cursor rules in `.cursor/rules/` provide AI guidance. They document standards that the agent should follow. Severity in .editorconfig controls compiler/IDE behavior; Cursor rules ensure the AI generates compliant code.

- **sonar-csharp** – S107, S1135, S125, S1144, S3776, S4487, S2737, S127, S1192, S4144, S3168, S6967
- **sonar-security** – S2068, S4818, S3649, S2077, S5443, S5542, S5527, S2245, S5547, S5146, S4790
- **parameter-count-s107** – S107 (max 7 parameters)
- **cognitive-complexity-s3776** – S3776 (complexity threshold)

## Test Projects

Test code (`**/*Tests*/**/*.cs`) uses relaxed severity for some Sonar rules to reduce noise:

- S1135: suggestion
- S125: none
- S107: suggestion
- S3776: suggestion
- S1144: none

Async naming rule still applies (error).
