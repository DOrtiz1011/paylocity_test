# Fixtures

All scenarios evaluate a proposed change to `ds_payroll_summary`.

- `current.json` - the currently published version (shared by all scenarios)
- `assets.json` - all known assets (shared by all scenarios). Note that some assets reference other objects.
- `NN-name/proposed.json` - the proposed new version for that scenario. Its `version` is a placeholder; your gate recommends the bump.
- `NN-name/expected.json` - the expected gate result for that scenario

Expected results list `changes`, `impactedAssets` and `violations` in no particular order. Violation messages are free-form and not part of the comparison.
