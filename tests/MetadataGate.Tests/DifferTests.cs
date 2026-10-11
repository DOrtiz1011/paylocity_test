using MetadataGate.Core.Models;
using MetadataGate.Core.Services;

namespace MetadataGate.Tests;

public class DifferTests
{
    private readonly MetadataDiffer _differ = new();

    [Fact]
    public void Detects_Added_Field_As_NonBreaking()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, new[]
        {
            new FieldDefinition("new_col", "string", Array.Empty<string>(), "active", "A new column")
        });

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Single(changes);
        var change = changes[0];
        Assert.Equal("new_col", change.Field);
        Assert.Equal(GateConstants.ChangeTypes.Added, change.ChangeType);
        Assert.False(change.Breaking);
    }

    [Fact]
    public void Detects_Removed_Field_As_Breaking()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, new[]
        {
            new FieldDefinition("old_col", "string", Array.Empty<string>(), "active", "An old column")
        });
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, Array.Empty<FieldDefinition>());

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Single(changes);
        var change = changes[0];
        Assert.Equal("old_col", change.Field);
        Assert.Equal(GateConstants.ChangeTypes.Removed, change.ChangeType);
        Assert.True(change.Breaking);
    }

    [Fact]
    public void Detects_Type_Changed_As_Breaking()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, new[]
        {
            new FieldDefinition("amount", "decimal", Array.Empty<string>(), "active", "Amount")
        });
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, new[]
        {
            new FieldDefinition("amount", "string", Array.Empty<string>(), "active", "Amount")
        });

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Single(changes);
        var change = changes[0];
        Assert.Equal("amount", change.Field);
        Assert.Equal(GateConstants.ChangeTypes.TypeChanged, change.ChangeType);
        Assert.True(change.Breaking);
    }

    [Fact]
    public void Detects_Tag_Changed_As_NonBreaking_Ignoring_Order()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, new[]
        {
            new FieldDefinition("email", "string", new[] { "contact", "internal" }, "active", "Email")
        });
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, new[]
        {
            new FieldDefinition("email", "string", new[] { "internal", "contact", "pii" }, "active", "Email")
        });

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Single(changes);
        var change = changes[0];
        Assert.Equal("email", change.Field);
        Assert.Equal(GateConstants.ChangeTypes.TagChanged, change.ChangeType);
        Assert.False(change.Breaking);
    }

    [Fact]
    public void Does_Not_Detect_Tag_Change_When_Only_Order_Or_Casing_Differs()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, new[]
        {
            new FieldDefinition("email", "string", new[] { "tagA", "tagB" }, "active", "Email")
        });
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, new[]
        {
            new FieldDefinition("email", "string", new[] { "tAGb", "TAGA" }, "active", "Email")
        });

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Empty(changes);
    }

    [Fact]
    public void Detects_Status_Changed_As_NonBreaking()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, new[]
        {
            new FieldDefinition("legacy_id", "string", Array.Empty<string>(), "active", "Legacy ID")
        });
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, new[]
        {
            new FieldDefinition("legacy_id", "string", Array.Empty<string>(), "deprecated", "Legacy ID", "Use new_id")
        });

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Single(changes);
        var change = changes[0];
        Assert.Equal("legacy_id", change.Field);
        Assert.Equal(GateConstants.ChangeTypes.StatusChanged, change.ChangeType);
        Assert.False(change.Breaking);
    }

    [Fact]
    public void Detects_Description_Changed_As_NonBreaking()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, new[]
        {
            new FieldDefinition("notes", "string", Array.Empty<string>(), "active", "Original note")
        });
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, new[]
        {
            new FieldDefinition("notes", "string", Array.Empty<string>(), "active", "Updated note")
        });

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Single(changes);
        var change = changes[0];
        Assert.Equal("notes", change.Field);
        Assert.Equal(GateConstants.ChangeTypes.DescriptionChanged, change.ChangeType);
        Assert.False(change.Breaking);
    }

    [Fact]
    public void Detects_Multiple_Attribute_Changes_On_Same_Field()
    {
        var current = new MetadataObject("obj1", "1.0.0", "team-a", null, new[]
        {
            new FieldDefinition("status_col", "int", Array.Empty<string>(), "active", "Old desc")
        });
        var proposed = new MetadataObject("obj1", "DRAFT", "team-a", null, new[]
        {
            new FieldDefinition("status_col", "string", new[] { "pii" }, "deprecated", "New desc", "Deprecated now")
        });

        var changes = _differ.ComputeChanges(current, proposed);

        Assert.Equal(4, changes.Count);
        Assert.Contains(changes, c => c.ChangeType == GateConstants.ChangeTypes.TypeChanged && c.Breaking);
        Assert.Contains(changes, c => c.ChangeType == GateConstants.ChangeTypes.StatusChanged && !c.Breaking);
        Assert.Contains(changes, c => c.ChangeType == GateConstants.ChangeTypes.TagChanged && !c.Breaking);
        Assert.Contains(changes, c => c.ChangeType == GateConstants.ChangeTypes.DescriptionChanged && !c.Breaking);
    }
}
