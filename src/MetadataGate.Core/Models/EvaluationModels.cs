using System.Text.Json.Serialization;

namespace MetadataGate.Core.Models;

public static class GateConstants
{
    public static class Decisions
    {
        public const string Allow = "ALLOW";
        public const string AllowWithWarnings = "ALLOW_WITH_WARNINGS";
        public const string Block = "BLOCK";
    }

    public static class VersionBumps
    {
        public const string Major = "major";
        public const string Minor = "minor";
        public const string Patch = "patch";
    }

    public static class ChangeTypes
    {
        public const string Added = "added";
        public const string Removed = "removed";
        public const string TypeChanged = "type_changed";
        public const string TagChanged = "tag_changed";
        public const string StatusChanged = "status_changed";
        public const string DescriptionChanged = "description_changed";
    }

    public static class RuleIds
    {
        public const string G1 = "G1";
        public const string G2 = "G2";
        public const string G3 = "G3";
    }

    public static class Tags
    {
        public const string Pii = "pii";
    }

    public static class Statuses
    {
        public const string Active = "active";
        public const string Deprecated = "deprecated";
    }
}

public sealed record FieldChange(
    string Field,
    string ChangeType,
    bool Breaking);

public sealed record ImpactedAsset(
    string AssetId,
    string Type,
    string Owner,
    string Field);

public sealed record RuleViolation(
    string Rule,
    string Field,
    bool Blocking,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message = null);

public sealed record EvaluationResult(
    string Decision,
    string RecommendedBump,
    IReadOnlyList<FieldChange> Changes,
    IReadOnlyList<ImpactedAsset> ImpactedAssets,
    IReadOnlyList<RuleViolation> Violations);
