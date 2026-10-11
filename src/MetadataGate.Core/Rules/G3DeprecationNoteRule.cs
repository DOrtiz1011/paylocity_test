using MetadataGate.Core.Models;

namespace MetadataGate.Core.Rules;

/// <summary>
/// G3: A deprecated field must include a deprecationNote.
/// </summary>
public sealed class G3DeprecationNoteRule : IGovernanceRule
{
    public string RuleId => GateConstants.RuleIds.G3;

    public IReadOnlyList<RuleViolation> Evaluate(RuleEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var violations = new List<RuleViolation>();

        foreach (var field in context.Proposed.Fields ?? Array.Empty<FieldDefinition>())
        {
            var isDeprecated = string.Equals(field.Status, GateConstants.Statuses.Deprecated, StringComparison.OrdinalIgnoreCase);

            if (isDeprecated && string.IsNullOrWhiteSpace(field.DeprecationNote))
            {
                violations.Add(new RuleViolation(
                    Rule: RuleId,
                    Field: field.Name,
                    Blocking: false,
                    Message: $"Field '{field.Name}' is marked as deprecated but is missing a 'deprecationNote'."));
            }
        }

        return violations;
    }
}
