using MetadataGate.Core.Models;

namespace MetadataGate.Core.Services;

public sealed class MetadataDiffer : IMetadataDiffer
{
    public IReadOnlyList<FieldChange> ComputeChanges(MetadataObject current, MetadataObject proposed)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(proposed);

        var currentFields  = (current.Fields  ?? Array.Empty<FieldDefinition>()).ToDictionary(f => f.Name, StringComparer.Ordinal);
        var proposedFields = (proposed.Fields ?? Array.Empty<FieldDefinition>()).ToDictionary(f => f.Name, StringComparer.Ordinal);
        var changes        = new List<FieldChange>();

        // 1. Removed fields (in current, not in proposed)
        foreach (var currentField in current.Fields ?? Array.Empty<FieldDefinition>())
        {
            if (!proposedFields.ContainsKey(currentField.Name))
            {
                changes.Add(new FieldChange(
                    Field: currentField.Name,
                    ChangeType: GateConstants.ChangeTypes.Removed,
                    Breaking: true));
            }
        }

        // 2. Added fields (in proposed, not in current) and Modified fields (in both)
        foreach (var proposedField in proposed.Fields ?? Array.Empty<FieldDefinition>())
        {
            if (!currentFields.TryGetValue(proposedField.Name, out var currentField))
            {
                changes.Add(new FieldChange(
                    Field: proposedField.Name,
                    ChangeType: GateConstants.ChangeTypes.Added,
                    Breaking: false));
                continue;
            }

            // Field exists in both: check attribute changes
            // Type change (breaking)
            if (!string.Equals(currentField.Type, proposedField.Type, StringComparison.OrdinalIgnoreCase))
            {
                changes.Add(new FieldChange(
                    Field: proposedField.Name,
                    ChangeType: GateConstants.ChangeTypes.TypeChanged,
                    Breaking: true));
            }

            // Status change (e.g. active -> deprecated) (non-breaking)
            if (!string.Equals(currentField.Status, proposedField.Status, StringComparison.OrdinalIgnoreCase))
            {
                changes.Add(new FieldChange(
                    Field: proposedField.Name,
                    ChangeType: GateConstants.ChangeTypes.StatusChanged,
                    Breaking: false));
            }

            // Tag change (non-breaking)
            var currentTags  = new HashSet<string>(currentField.Tags  ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var proposedTags = new HashSet<string>(proposedField.Tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            if (!currentTags.SetEquals(proposedTags))
            {
                changes.Add(new FieldChange(
                    Field: proposedField.Name,
                    ChangeType: GateConstants.ChangeTypes.TagChanged,
                    Breaking: false));
            }

            // Description change (non-breaking)
            if (!string.Equals(currentField.Description ?? string.Empty, proposedField.Description ?? string.Empty, StringComparison.Ordinal))
            {
                changes.Add(new FieldChange(
                    Field: proposedField.Name,
                    ChangeType: GateConstants.ChangeTypes.DescriptionChanged,
                    Breaking: false));
            }
        }

        return changes;
    }
}
