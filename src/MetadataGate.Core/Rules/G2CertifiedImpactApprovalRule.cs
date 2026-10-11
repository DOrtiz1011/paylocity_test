using MetadataGate.Core.Models;

namespace MetadataGate.Core.Rules;

/// <summary>
/// G2: A breaking change that impacts any certified asset requires approvedBy.
/// </summary>
public sealed class G2CertifiedImpactApprovalRule : IGovernanceRule
{
    public string RuleId => GateConstants.RuleIds.G2;

    public IReadOnlyList<RuleViolation> Evaluate(RuleEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If approval is granted, rule passes
        if (!string.IsNullOrWhiteSpace(context.Proposed.ApprovedBy))
        {
            return Array.Empty<RuleViolation>();
        }

        var certifiedAssetIds = (context.Assets ?? Array.Empty<Asset>())
            .Where(a => a.Certified)
            .Select(a => a.Id)
            .ToHashSet(StringComparer.Ordinal);

        var breakingFields = (context.Changes ?? Array.Empty<FieldChange>())
            .Where(c => c.Breaking)
            .Select(c => c.Field)
            .Distinct(StringComparer.Ordinal);

        var violations = new List<RuleViolation>();

        foreach (var field in breakingFields)
        {
            var impactsCertifiedAsset = (context.ImpactedAssets ?? Array.Empty<ImpactedAsset>())
                .Any(ia => string.Equals(ia.Field, field, StringComparison.Ordinal)
                           && certifiedAssetIds.Contains(ia.AssetId));

            if (impactsCertifiedAsset)
            {
                violations.Add(new RuleViolation(
                    Rule: RuleId,
                    Field: field,
                    Blocking: true,
                    Message: $"Breaking change on field '{field}' impacts certified downstream asset(s) but has no 'approvedBy' set."));
            }
        }

        return violations;
    }
}
