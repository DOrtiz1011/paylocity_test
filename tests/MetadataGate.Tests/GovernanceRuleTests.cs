using MetadataGate.Core.Models;
using MetadataGate.Core.Rules;

namespace MetadataGate.Tests;

public class GovernanceRuleTests
{
    private readonly G1PiiApprovalRule _g1 = new();
    private readonly G2CertifiedImpactApprovalRule _g2 = new();
    private readonly G3DeprecationNoteRule _g3 = new();

    [Fact]
    public void G1_Triggers_When_Brand_New_Field_Has_Pii_Without_Approval()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, new[]
        {
            new FieldDefinition("ssn", "string", new[] { "pii" }, "active", "SSN")
        });

        var context = new RuleEvaluationContext(
            Current: current,
            Proposed: proposed,
            Changes: Array.Empty<FieldChange>(),
            Assets: Array.Empty<Asset>(),
            ImpactedAssets: Array.Empty<ImpactedAsset>());

        var violations = _g1.Evaluate(context);

        Assert.Single(violations);
        var v = violations[0];
        Assert.Equal("G1", v.Rule);
        Assert.Equal("ssn", v.Field);
        Assert.True(v.Blocking);
    }

    [Fact]
    public void G1_Passes_When_New_Pii_Field_Has_Approval()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", "security-lead", new[]
        {
            new FieldDefinition("ssn", "string", new[] { "pii" }, "active", "SSN")
        });

        var context = new RuleEvaluationContext(
            Current: current,
            Proposed: proposed,
            Changes: Array.Empty<FieldChange>(),
            Assets: Array.Empty<Asset>(),
            ImpactedAssets: Array.Empty<ImpactedAsset>());

        var violations = _g1.Evaluate(context);

        Assert.Empty(violations);
    }

    [Fact]
    public void G1_Triggers_When_Existing_Field_Newly_Gains_Pii_Tag_Without_Approval()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, new[]
        {
            new FieldDefinition("email", "string", Array.Empty<string>(), "active", "User email")
        });
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, new[]
        {
            new FieldDefinition("email", "string", new[] { "pii" }, "active", "User email")
        });

        var context = new RuleEvaluationContext(
            Current: current,
            Proposed: proposed,
            Changes: Array.Empty<FieldChange>(),
            Assets: Array.Empty<Asset>(),
            ImpactedAssets: Array.Empty<ImpactedAsset>());

        var violations = _g1.Evaluate(context);

        Assert.Single(violations);
        Assert.Equal("email", violations[0].Field);
    }

    [Fact]
    public void G1_Does_Not_Trigger_When_Field_Already_Had_Pii_Tag_In_Current()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, new[]
        {
            new FieldDefinition("user_id", "string", new[] { "pii" }, "active", "ID")
        });
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, new[]
        {
            new FieldDefinition("user_id", "string", new[] { "pii" }, "active", "ID")
        });

        var context = new RuleEvaluationContext(
            Current: current,
            Proposed: proposed,
            Changes: Array.Empty<FieldChange>(),
            Assets: Array.Empty<Asset>(),
            ImpactedAssets: Array.Empty<ImpactedAsset>());

        var violations = _g1.Evaluate(context);

        Assert.Empty(violations);
    }

    [Fact]
    public void G2_Triggers_When_Breaking_Change_Impacts_Certified_Asset_Without_Approval()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, Array.Empty<FieldDefinition>());
        var changes = new[] { new FieldChange("total", GateConstants.ChangeTypes.Removed, true) };
        var assets = new[]
        {
            new Asset("asset1", "report", "finance", true, new[] { new AssetReference("ds1", "total") })
        };
        var impacted = new[]
        {
            new ImpactedAsset("asset1", "report", "finance", "total")
        };

        var context = new RuleEvaluationContext(current, proposed, changes, assets, impacted);

        var violations = _g2.Evaluate(context);

        Assert.Single(violations);
        Assert.Equal("G2", violations[0].Rule);
        Assert.Equal("total", violations[0].Field);
        Assert.True(violations[0].Blocking);
    }

    [Fact]
    public void G2_Passes_When_Breaking_Change_Impacts_Certified_Asset_With_Approval()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", "product-mgr", Array.Empty<FieldDefinition>());
        var changes = new[] { new FieldChange("total", GateConstants.ChangeTypes.Removed, true) };
        var assets = new[]
        {
            new Asset("asset1", "report", "finance", true, new[] { new AssetReference("ds1", "total") })
        };
        var impacted = new[]
        {
            new ImpactedAsset("asset1", "report", "finance", "total")
        };

        var context = new RuleEvaluationContext(current, proposed, changes, assets, impacted);

        var violations = _g2.Evaluate(context);

        Assert.Empty(violations);
    }

    [Fact]
    public void G2_Passes_When_Breaking_Change_Only_Impacts_Uncertified_Assets()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, Array.Empty<FieldDefinition>());
        var changes = new[] { new FieldChange("total", GateConstants.ChangeTypes.Removed, true) };
        var assets = new[]
        {
            new Asset("asset1", "visualization", "marketing", false, new[] { new AssetReference("ds1", "total") })
        };
        var impacted = new[]
        {
            new ImpactedAsset("asset1", "visualization", "marketing", "total")
        };

        var context = new RuleEvaluationContext(current, proposed, changes, assets, impacted);

        var violations = _g2.Evaluate(context);

        Assert.Empty(violations);
    }

    [Fact]
    public void G3_Triggers_When_Deprecated_Field_Has_No_Deprecation_Note()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, new[]
        {
            new FieldDefinition("old_field", "string", Array.Empty<string>(), "deprecated", "Desc", null)
        });

        var context = new RuleEvaluationContext(current, proposed, Array.Empty<FieldChange>(), Array.Empty<Asset>(), Array.Empty<ImpactedAsset>());

        var violations = _g3.Evaluate(context);

        Assert.Single(violations);
        var v = violations[0];
        Assert.Equal("G3", v.Rule);
        Assert.Equal("old_field", v.Field);
        Assert.False(v.Blocking);
    }

    [Fact]
    public void G3_Passes_When_Deprecated_Field_Includes_Deprecation_Note()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, new[]
        {
            new FieldDefinition("old_field", "string", Array.Empty<string>(), "deprecated", "Desc", "Use new_field instead")
        });

        var context = new RuleEvaluationContext(current, proposed, Array.Empty<FieldChange>(), Array.Empty<Asset>(), Array.Empty<ImpactedAsset>());

        var violations = _g3.Evaluate(context);

        Assert.Empty(violations);
    }
}
