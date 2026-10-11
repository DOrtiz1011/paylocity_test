using MetadataGate.Core.Models;

namespace MetadataGate.Core.Services;

public interface IVersionBumpRecommender
{
    string RecommendBump(IReadOnlyList<FieldChange> changes);
}
