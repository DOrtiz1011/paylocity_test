using MetadataGate.Core.Models;

namespace MetadataGate.Core.Services;

public interface IMetadataGateEvaluator
{
    EvaluationResult Evaluate(
        MetadataObject current,
        MetadataObject proposed,
        IReadOnlyList<Asset> assets);
}
