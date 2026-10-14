using MetadataGate.Core.Models;

namespace MetadataGate.Core.Services;

public sealed class ImpactAnalyzer : IImpactAnalyzer
{
    public IReadOnlyList<ImpactedAsset> ComputeImpact(
        string                     objectId,
        IReadOnlyList<FieldChange> changes,
        IReadOnlyList<Asset>       assets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(assets);

        var impacted        = new List<ImpactedAsset>();
        var breakingChanges = changes.Where(c => c.Breaking).ToList();

        if (breakingChanges.Count == 0 || assets.Count == 0)
        {
            return impacted;
        }

        foreach (var change in breakingChanges)
        {
            foreach (var asset in assets)
            {
                var hasMatchingReference = (asset.References ?? Array.Empty<AssetReference>())
                    .Any(r =>    string.Equals(r.ObjectId,  objectId,     StringComparison.Ordinal)
                              && string.Equals(r.FieldName, change.Field, StringComparison.Ordinal));

                if (hasMatchingReference)
                {
                    impacted.Add(new ImpactedAsset(
                        AssetId: asset.Id,
                        Type: asset.Type,
                        Owner: asset.Owner,
                        Field: change.Field));
                }
            }
        }

        return impacted;
    }
}
