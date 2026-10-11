# Metadata Change Gate

A robust, enterprise-ready **Publish Gate** for metadata object versions built in modern C# (.NET).

When publishing a new version of a metadata dataset, this gate evaluates the proposed changes against downstream consumer assets and enforces governance rules before the change goes live:
1. **What breaks?** Identifies affected downstream assets (reports, visualizations, pipelines).
2. **Is it allowed?** Enforces compliance and governance policies (`G1`, `G2`, `G3`).
3. **What version bump is required?** Recommends SemVer bump (`major`, `minor`, `patch`).

---

## Architecture & Project Structure

```
├── src/
│   ├── MetadataGate.Core/           # Core domain library (zero external dependencies)
│   │   ├── Models/                  # Immutable records (MetadataObject, Asset, etc.)
│   │   ├── Rules/                   # Extensible Governance Rule Engine (IGovernanceRule)
│   │   │   ├── G1PiiApprovalRule.cs
│   │   │   ├── G2CertifiedImpactApprovalRule.cs
│   │   │   └── G3DeprecationNoteRule.cs
│   │   └── Services/                # Differ, BumpRecommender, ImpactAnalyzer, Evaluator
│   └── MetadataGate.Cli/            # Production CLI runner
│       └── Program.cs               # Ergonomic CLI argument parsing & formatted JSON output
├── tests/
│   └── MetadataGate.Tests/          # Comprehensive test suite
│       ├── FixtureRegressionTests.cs # Regression tests for all provided fixtures
│       ├── DifferTests.cs           # Unit tests for field diffing logic
│       ├── GovernanceRuleTests.cs   # Isolated rule tests (G1, G2, G3)
│       └── EdgeCaseTests.cs         # Edge case coverage (multi-field impact, casing, whitespace)
└── fixtures/                        # Scenarios with current, proposed, assets, and expected JSON
```

---

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0, 9.0, or 10.0)

---

## Quick Start

### 1. Build the Solution
```bash
dotnet build
```

### 2. Run All Tests
```bash
dotnet test
```
All 27 automated tests (including fixture regression tests and edge cases) will execute.

### 3. Run the CLI

Evaluate any scenario by providing the paths to `--current`, `--proposed`, and `--assets`:

```bash
# Fixture 01: Add non-PII field (Decision: ALLOW, Bump: minor)
dotnet run --project src/MetadataGate.Cli -- \
  --current fixtures/current.json \
  --proposed fixtures/01-add-non-pii-field/proposed.json \
  --assets fixtures/assets.json

# Fixture 02: Remove field used by uncertified viz (Decision: ALLOW_WITH_WARNINGS, Bump: major)
dotnet run --project src/MetadataGate.Cli -- \
  --current fixtures/current.json \
  --proposed fixtures/02-remove-field-used-by-viz/proposed.json \
  --assets fixtures/assets.json

# Fixture 03: Type change affecting certified report (Decision: BLOCK, Bump: major)
dotnet run --project src/MetadataGate.Cli -- \
  --current fixtures/current.json \
  --proposed fixtures/03-type-change-certified-report/proposed.json \
  --assets fixtures/assets.json

# Fixture 04: PII addition without approval & deprecation without note (Decision: BLOCK, Bump: minor)
dotnet run --project src/MetadataGate.Cli -- \
  --current fixtures/current.json \
  --proposed fixtures/04-pii-and-deprecation/proposed.json \
  --assets fixtures/assets.json
```

Positional arguments are also supported:
```bash
dotnet run --project src/MetadataGate.Cli -- fixtures/current.json fixtures/01-add-non-pii-field/proposed.json fixtures/assets.json
```

---

## CLI Options & Exit Codes

```
Options:
  -c, --current    Path to currently published metadata JSON file
  -p, --proposed   Path to proposed metadata JSON file
  -a, --assets     Path to downstream assets JSON file
  -h, --help       Show help message

Exit Codes:
  0 : Decision is ALLOW or ALLOW_WITH_WARNINGS
  1 : Evaluation error or invalid CLI arguments
  2 : Decision is BLOCK (ideal for CI/CD gates and pre-publish pipelines)
```

---

## Output Format

Outputs standard JSON adhering to `schema.md`:

```json
{
  "decision": "BLOCK",
  "recommendedBump": "major",
  "changes": [
    {
      "field": "gross_pay",
      "changeType": "type_changed",
      "breaking": true
    }
  ],
  "impactedAssets": [
    {
      "assetId": "rpt_q3_labor_cost",
      "type": "report",
      "owner": "team-finance",
      "field": "gross_pay"
    }
  ],
  "violations": [
    {
      "rule": "G2",
      "field": "gross_pay",
      "blocking": true,
      "message": "Breaking change on field 'gross_pay' impacts certified downstream asset(s) but has no 'approvedBy' set."
    }
  ]
}
```
