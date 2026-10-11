using MetadataGate.Core.Models;

namespace MetadataGate.Core.Services;

public interface IImpactAnalyzer
{
    IReadOnlyList<ImpactedAsset> ComputeImpact(
        string objectId,
        IReadOnlyList<FieldChange> changes,
        IReadOnlyList<Asset> assets);
}
