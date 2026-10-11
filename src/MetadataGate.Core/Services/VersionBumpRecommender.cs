using MetadataGate.Core.Models;

namespace MetadataGate.Core.Services;

public sealed class VersionBumpRecommender : IVersionBumpRecommender
{
    public string RecommendBump(IReadOnlyList<FieldChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        // 1. Any breaking change (removed, type_changed) triggers a major bump
        if (changes.Any(c => c.Breaking))
        {
            return GateConstants.VersionBumps.Major;
        }

        // 2. Non-breaking functionality changes (field added or field deprecated) trigger a minor bump
        if (changes.Any(c => c.ChangeType is GateConstants.ChangeTypes.Added or GateConstants.ChangeTypes.StatusChanged))
        {
            return GateConstants.VersionBumps.Minor;
        }

        // 3. Metadata/documentation adjustments (description, tags) trigger a patch bump
        return GateConstants.VersionBumps.Patch;
    }
}
