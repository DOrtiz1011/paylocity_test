# Schema

## Inputs

### Metadata object version (`current.json`, `proposed.json`)

| Property | Type | Notes |
|---|---|---|
| `id` | string | Object id. Current and proposed share the same id |
| `version` | string | Semver for the current version. Placeholder (`DRAFT`) on proposed |
| `owner` | string | Owning team |
| `approvedBy` | string or null | Set when a governance approval has been granted for this version |
| `fields[]` | array | See below |

Field:

| Property | Type | Notes |
|---|---|---|
| `name` | string | Unique within an object |
| `type` | string | e.g. `string`, `decimal`, `date` |
| `tags` | string[] | e.g. `pii` |
| `status` | `active` or `deprecated` | |
| `description` | string | |
| `deprecationNote` | string (optional) | Expected when `status` is `deprecated` |

### Asset (`assets.json` is an array of these)

| Property | Type | Notes |
|---|---|---|
| `id` | string | |
| `type` | `report`, `visualization` or `pipeline` | |
| `owner` | string | |
| `certified` | boolean | |
| `references[]` | `{ objectId, fieldName }` | Assets may reference other objects; only those matching the object being evaluated are relevant |

## Output (see each fixture's `expected.json`)

```jsonc
{
  "decision": "ALLOW | ALLOW_WITH_WARNINGS | BLOCK",
  "recommendedBump": "major | minor | patch",
  "changes": [
    { "field": "gross_pay", "changeType": "type_changed", "breaking": true }
  ],
  "impactedAssets": [
    { "assetId": "rpt_q3_labor_cost", "type": "report", "owner": "team-finance", "field": "gross_pay" }
  ],
  "violations": [
    { "rule": "G2", "field": "gross_pay", "blocking": true, "message": "..." }
  ]
}
```

`changeType` values: `added`, `removed`, `type_changed`, `tag_changed`, `status_changed`, `description_changed`.

An asset that references several affected fields appears once per affected field in `impactedAssets`.
