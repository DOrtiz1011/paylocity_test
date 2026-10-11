namespace MetadataGate.Core.Models;

public sealed record MetadataObject(
    string Id,
    string Version,
    string Owner,
    string? ApprovedBy,
    IReadOnlyList<FieldDefinition> Fields)
{
    public MetadataObject() : this(string.Empty, string.Empty, string.Empty, null, Array.Empty<FieldDefinition>()) { }
}

public sealed record FieldDefinition(
    string Name,
    string Type,
    IReadOnlyList<string> Tags,
    string Status,
    string Description,
    string? DeprecationNote = null)
{
    public FieldDefinition() : this(string.Empty, string.Empty, Array.Empty<string>(), "active", string.Empty, null) { }
}

public sealed record Asset(
    string Id,
    string Type,
    string Owner,
    bool Certified,
    IReadOnlyList<AssetReference> References)
{
    public Asset() : this(string.Empty, string.Empty, string.Empty, false, Array.Empty<AssetReference>()) { }
}

public sealed record AssetReference(
    string ObjectId,
    string FieldName)
{
    public AssetReference() : this(string.Empty, string.Empty) { }
}
