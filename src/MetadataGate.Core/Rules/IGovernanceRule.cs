using MetadataGate.Core.Models;

namespace MetadataGate.Core.Rules;

public sealed record RuleEvaluationContext(
    MetadataObject Current,
    MetadataObject Proposed,
    IReadOnlyList<FieldChange> Changes,
    IReadOnlyList<Asset> Assets,
    IReadOnlyList<ImpactedAsset> ImpactedAssets);

public interface IGovernanceRule
{
    string RuleId { get; }
    IReadOnlyList<RuleViolation> Evaluate(RuleEvaluationContext context);
}
