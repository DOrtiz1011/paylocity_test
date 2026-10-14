using MetadataGate.Core.Models;
using MetadataGate.Core.Rules;

namespace MetadataGate.Core.Services;

public sealed class MetadataGateEvaluator : IMetadataGateEvaluator
{
    private readonly IMetadataDiffer                _differ;
    private readonly IVersionBumpRecommender        _bumpRecommender;
    private readonly IImpactAnalyzer                _impactAnalyzer;
    private readonly IReadOnlyList<IGovernanceRule> _rules;

    public MetadataGateEvaluator(
        IMetadataDiffer?              differ          = null,
        IVersionBumpRecommender?      bumpRecommender = null,
        IImpactAnalyzer?              impactAnalyzer  = null,
        IEnumerable<IGovernanceRule>? rules           = null)
    {
        _differ          = differ          ?? new MetadataDiffer();
        _bumpRecommender = bumpRecommender ?? new VersionBumpRecommender();
        _impactAnalyzer  = impactAnalyzer  ?? new ImpactAnalyzer();
        _rules           = (rules          ?? new IGovernanceRule[]
        {
            new G1PiiApprovalRule(),
            new G2CertifiedImpactApprovalRule(),
            new G3DeprecationNoteRule()
        }).ToList();
    }

    public EvaluationResult Evaluate(MetadataObject current, MetadataObject proposed, IReadOnlyList<Asset> assets)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(proposed);
        ArgumentNullException.ThrowIfNull(assets);

        // R1: Diff versions
        var changes = _differ.ComputeChanges(current, proposed);

        // R2: Recommend semver bump
        var recommendedBump = _bumpRecommender.RecommendBump(changes);

        // R3: Compute impact of breaking changes
        var impactedAssets = _impactAnalyzer.ComputeImpact(current.Id, changes, assets);

        // R4: Enforce governance rules
        var ruleContext = new RuleEvaluationContext(
            Current: current,
            Proposed: proposed,
            Changes: changes,
            Assets: assets,
            ImpactedAssets: impactedAssets);

        var violations = new List<RuleViolation>();
        foreach (var rule in _rules)
        {
            var ruleViolations = rule.Evaluate(ruleContext);
            if (ruleViolations is { Count: > 0 })
            {
                violations.AddRange(ruleViolations);
            }
        }

        // R5: Return decision
        string decision;
        if (violations.Any(v => v.Blocking))
        {
            decision = GateConstants.Decisions.Block;
        }
        else if (violations.Any(v => !v.Blocking) || changes.Any(c => c.Breaking))
        {
            decision = GateConstants.Decisions.AllowWithWarnings;
        }
        else
        {
            decision = GateConstants.Decisions.Allow;
        }

        return new EvaluationResult(
            Decision: decision,
            RecommendedBump: recommendedBump,
            Changes: changes,
            ImpactedAssets: impactedAssets,
            Violations: violations);
    }
}
