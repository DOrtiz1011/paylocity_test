using MetadataGate.Core.Models;

namespace MetadataGate.Core.Rules;

/// <summary>
/// G1: A field tagged 'pii' cannot be added to or newly exposed in an object unless the proposed version has approvedBy set.
/// </summary>
public sealed class G1PiiApprovalRule : IGovernanceRule
{
    public string RuleId => GateConstants.RuleIds.G1;

    public IReadOnlyList<RuleViolation> Evaluate(RuleEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // If approval is granted, rule passes
        if (!string.IsNullOrWhiteSpace(context.Proposed.ApprovedBy))
        {
            return Array.Empty<RuleViolation>();
        }

        var currentFields = (context.Current.Fields ?? Array.Empty<FieldDefinition>())
            .ToDictionary(f => f.Name, StringComparer.Ordinal);

        var violations = new List<RuleViolation>();

        foreach (var proposedField in context.Proposed.Fields ?? Array.Empty<FieldDefinition>())
        {
            var isProposedPii = (proposedField.Tags ?? Array.Empty<string>())
                .Any(t => string.Equals(t, GateConstants.Tags.Pii, StringComparison.OrdinalIgnoreCase));

            if (!isProposedPii)
            {
                continue;
            }

            var isNewlyExposed = false;

            if (!currentFields.TryGetValue(proposedField.Name, out var currentField))
            {
                // Field is brand new and has PII
                isNewlyExposed = true;
            }
            else
            {
                // Field existed, check if PII tag is newly added
                var currentHadPii = (currentField.Tags ?? Array.Empty<string>())
                    .Any(t => string.Equals(t, GateConstants.Tags.Pii, StringComparison.OrdinalIgnoreCase));

                if (!currentHadPii)
                {
                    isNewlyExposed = true;
                }
            }

            if (isNewlyExposed)
            {
                violations.Add(new RuleViolation(
                    Rule: RuleId,
                    Field: proposedField.Name,
                    Blocking: true,
                    Message: $"Field '{proposedField.Name}' introduces PII data but the proposed version has no 'approvedBy' set."));
            }
        }

        return violations;
    }
}
