# Take-Home Assignment: Metadata Change Gate

**Suggested time:** ~3–4 hours | **AI tools:** Allowed and encouraged

## 1. Context

Our team builds platforms that let people author things like visualizations, reports, and pipelines on top of a shared **metadata layer**. A metadata object (for example, a dataset) describes its fields, types, ownership, and governance tags. Authors build **assets** that reference those fields, and trust that everything they need is defined in the metadata.

Metadata objects change over time. When someone publishes a new version of a dataset, we need to answer two questions **before** the change goes live:

1. **What breaks?** Which downstream assets depend on something that was removed or changed?
2. **Is it allowed?** Does the change comply with governance rules?

Your task is to build the small piece of the system that answers those questions.

## 2. The Problem

Build a **publish gate** for metadata object versions.

Given the currently published version of a metadata object, a proposed new version, and the set of assets that reference the object, the gate must:

### Requirements

**R1. Diff the versions.**
Compute field-level changes: added, removed, type changed, renamed (a rename may be treated as remove + add; do not attempt rename detection), tag changed, and `status` changed (`active` → `deprecated`).

**R2. Classify each change and recommend a version bump.**
- Breaking: field removed, field type changed
- Non-breaking: field added, description or tag-only change, field marked deprecated
- Recommend `major` / `minor` / `patch` using semver semantics. Document your classification choices.

**R3. Compute impact.**
For each breaking change, list the assets that reference the affected field (asset id, type, owner). Assets reference fields by `objectId` + `fieldName`.

**R4. Enforce governance rules.** Evaluate these rules and return violations:
- **G1:** A field tagged `pii` cannot be added to or newly exposed in an object unless the proposed version has `approvedBy` set.
- **G2:** A breaking change that impacts any `certified` asset requires `approvedBy`.
- **G3:** A deprecated field must include a `deprecationNote`.

**R5. Return a decision.**
Output a result of `ALLOW`, `ALLOW_WITH_WARNINGS`, or `BLOCK`, along with the diff, the bump recommendation, the impacted assets, and any violations. Blocking violations (G1, G2) → `BLOCK`. G3 and non-blocking breaking changes → `ALLOW_WITH_WARNINGS`.

### Interface

Pick **one**: a small HTTP API (`POST /gate/evaluate`), a CLI that reads JSON files, or a library with a clear entry function. Use any language and any stack you are comfortable with. Persistence is **not** required; in-memory or JSON-file input is fine.

### Out of scope

UI, authentication, a database, rename detection, lineage beyond one hop, multi-object dependencies, deployment.

## 3. Provided Materials

- `schema.md` describing the input shapes below
- `fixtures/` containing sample scenarios, each with inputs and the expected result

```jsonc
// metadata object version
{
  "id": "ds_payroll_summary",
  "version": "1.3.0",
  "owner": "team-payroll",
  "approvedBy": null,
  "fields": [
    { "name": "employee_id", "type": "string", "tags": ["pii"], "status": "active", "description": "..." },
    { "name": "gross_pay",   "type": "decimal", "tags": [], "status": "active", "description": "..." }
  ]
}

// asset
{
  "id": "rpt_q3_labor_cost",
  "type": "report",            // report | visualization | pipeline
  "owner": "team-finance",
  "certified": true,
  "references": [{ "objectId": "ds_payroll_summary", "fieldName": "gross_pay" }]
}
```

## 4. Deliverables

1. Working code with a README: how to run it, how to run tests
2. Tests covering the provided fixtures plus a few of your own edge cases
3. A short `NOTES.md` (max 1 page):
   - Design decisions and assumptions
   - What you would build next or change with more time
   - How you used AI, if you did

## 5. Guidance

- Aim for roughly 3–4 hours. A clean, tested core is preferred over a sprawling solution.
- AI assistants are welcome. You own everything you submit and should be able to explain any part of it.
- Where the spec is ambiguous, make a reasonable choice and write it down in `NOTES.md`.

## 6. Optional Stretch

Not required.

- Warn on assets referencing deprecated fields
- Machine-readable and human-readable output formats
- Configurable rules via a JSON/YAML file
