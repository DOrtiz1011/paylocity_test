using MetadataGate.Core.Models;

namespace MetadataGate.Core.Services;

public interface IMetadataDiffer
{
    IReadOnlyList<FieldChange> ComputeChanges(MetadataObject current, MetadataObject proposed);
}
