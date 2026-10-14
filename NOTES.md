# Notes & Design Decisions

## 1. Design Decisions & Assumptions

- **Separation of Concerns (Core vs. CLI)**: The business logic lives entirely within `MetadataGate.Core` with zero third-party dependencies. The CLI (`MetadataGate.Cli`) acts purely as an I/O driver. This allows the core engine to be used as a class library, embedded in a CLI, or hosted in an ASP.NET Core Web API without modification.
- **Rule Engine Pattern (`IGovernanceRule`)**: Rather than a monolithic evaluator with nested `if/else` checks, each governance policy (`G1`, `G2`, `G3`) is encapsulated into an isolated rule implementing `IGovernanceRule`. This adheres to the Open/Closed Principle—adding rule `G4` requires zero modifications to existing rules or orchestrators.
- **PII Exposure Semantics (G1)**: The requirement states: *"A field tagged `pii` cannot be added to or newly exposed in an object unless the proposed version has `approvedBy` set."* We classify a field as newly exposed if:
  1. The field is newly added to the object and tagged `pii`, or
  2. The field already existed, but did not have the `pii` tag in the current version.
  Existing fields that already had `pii` in the current version do not trigger G1.
- **Impact Scoping (R3)**: Impacts are only generated for **breaking changes** (`removed`, `type_changed`) where an asset's reference matches both `objectId == current.id` and `fieldName == change.field`. Assets referencing other datasets are ignored. If an asset references multiple broken fields, it appears once per affected field.
- **SemVer Bump Semantics (R2)**:
  - `major`: Any breaking change (`removed`, `type_changed`).
  - `minor`: Non-breaking functionality changes (`added`, `status_changed` to deprecated).
  - `patch`: Documentation/metadata changes (`description_changed`, `tag_changed`) or unchanged metadata.
- **Validation Strictness**: Fields such as `approvedBy` and `deprecationNote` are checked with `!string.IsNullOrWhiteSpace(...)` to prevent whitespace-only bypasses.
- **CLI Exit Codes**: To make the CLI production-friendly in CI/CD pipelines, `BLOCK` returns exit code `2`, whereas `ALLOW` and `ALLOW_WITH_WARNINGS` return `0` (general errors return `1`).

## 2. What I Would Build Next / With More Time

1. **Multi-Hop Dependency & Lineage Graphs**: In real data platforms, pipelines output downstream datasets that feed downstream dashboards. Extending impact analysis through multi-hop DAG traversal would identify indirect breakage.
2. **Configurable Rule Engine (Policy-as-Code)**: Allow governance teams to define custom rules and thresholds via JSON/YAML or OPA (Open Policy Agent) without code deployments.
3. **Smart Rename Detection**: Use heuristic matching (e.g. type matching + Levenshtein distance on names + description similarity) to suggest renames and generate automated SQL/dbt migration aliases.
4. **Type Compatibility Matrix**: Distinguish widening conversions (e.g., `int` → `long` or `float` → `double`) from narrowing/breaking conversions (e.g., `string` → `int`).
5. **REST API Endpoint**: Expose `POST /gate/evaluate` using ASP.NET Core Minimal APIs for seamless GitHub Actions / GitLab CI webhook integrations.

## 3. How AI Was Used

- **Pair Programming & Requirement Verification**: AI was used to cross-reference `take-home-assignment-outline.md`, `schema.md`, and all 4 fixture scenarios to ensure alignment with edge cases.
- **Scaffolding & Edge Case Ideation**: AI assisted in scaffolding boilerplate record definitions and brainstorming edge cases (e.g., whitespace-only approval strings, foreign object references, case-insensitive tag sets).
- **Code Ownership**: All architectural patterns (Rule Engine, Differ, CLI ergonomics, xUnit test fixtures) were reviewed, validated, and tested end-to-end against the provided fixtures.
