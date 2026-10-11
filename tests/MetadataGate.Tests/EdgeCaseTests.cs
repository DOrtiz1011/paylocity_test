using MetadataGate.Core.Models;
using MetadataGate.Core.Services;

namespace MetadataGate.Tests;

public class EdgeCaseTests
{
    private readonly MetadataGateEvaluator _evaluator = new();

    [Fact]
    public void Asset_Referencing_Multiple_Broken_Fields_Appears_Once_Per_Field()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, new[]
        {
            new FieldDefinition("col_a", "string", Array.Empty<string>(), "active", "Col A"),
            new FieldDefinition("col_b", "int", Array.Empty<string>(), "active", "Col B")
        });
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, Array.Empty<FieldDefinition>());

        var asset = new Asset("multi_asset", "pipeline", "team-data", false, new[]
        {
            new AssetReference("ds1", "col_a"),
            new AssetReference("ds1", "col_b")
        });

        var result = _evaluator.Evaluate(current, proposed, new[] { asset });

        Assert.Equal(2, result.ImpactedAssets.Count);
        Assert.Contains(result.ImpactedAssets, ia => ia.AssetId == "multi_asset" && ia.Field == "col_a");
        Assert.Contains(result.ImpactedAssets, ia => ia.AssetId == "multi_asset" && ia.Field == "col_b");
    }

    [Fact]
    public void Assets_Referencing_Other_Objects_Are_Not_Impacted()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, new[]
        {
            new FieldDefinition("col_a", "string", Array.Empty<string>(), "active", "Col A")
        });
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, Array.Empty<FieldDefinition>());

        var foreignAsset = new Asset("foreign_asset", "report", "team-other", true, new[]
        {
            new AssetReference("ds_DIFFERENT", "col_a")
        });

        var result = _evaluator.Evaluate(current, proposed, new[] { foreignAsset });

        Assert.Empty(result.ImpactedAssets);
        Assert.Empty(result.Violations);
        Assert.Equal(GateConstants.Decisions.AllowWithWarnings, result.Decision);
    }

    [Fact]
    public void Whitespace_Only_ApprovedBy_Is_Treated_As_Missing_Approval()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, Array.Empty<FieldDefinition>());
        var proposed = new MetadataObject("ds1", "DRAFT", "team", "   ", new[]
        {
            new FieldDefinition("credit_card", "string", new[] { "pii" }, "active", "Card")
        });

        var result = _evaluator.Evaluate(current, proposed, Array.Empty<Asset>());

        Assert.Equal(GateConstants.Decisions.Block, result.Decision);
        Assert.Single(result.Violations);
        Assert.Equal("G1", result.Violations[0].Rule);
    }

    [Fact]
    public void Whitespace_Only_DeprecationNote_Triggers_G3()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, new[]
        {
            new FieldDefinition("legacy_field", "string", Array.Empty<string>(), "active", "Desc")
        });
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, new[]
        {
            new FieldDefinition("legacy_field", "string", Array.Empty<string>(), "deprecated", "Desc", "   ")
        });

        var result = _evaluator.Evaluate(current, proposed, Array.Empty<Asset>());

        Assert.Equal(GateConstants.Decisions.AllowWithWarnings, result.Decision);
        Assert.Single(result.Violations);
        Assert.Equal("G3", result.Violations[0].Rule);
        Assert.False(result.Violations[0].Blocking);
    }

    [Fact]
    public void Certified_Impact_With_Approval_Yields_AllowWithWarnings()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, new[]
        {
            new FieldDefinition("balance", "decimal", Array.Empty<string>(), "active", "Account balance")
        });
        var proposed = new MetadataObject("ds1", "DRAFT", "team", "vp-engineering", new[]
        {
            new FieldDefinition("balance", "string", Array.Empty<string>(), "active", "Account balance")
        });

        var certifiedAsset = new Asset("cert_rpt", "report", "finance", true, new[]
        {
            new AssetReference("ds1", "balance")
        });

        var result = _evaluator.Evaluate(current, proposed, new[] { certifiedAsset });

        // G2 is satisfied because approvedBy is set
        Assert.Empty(result.Violations);
        // But because breaking changes exist, decision is ALLOW_WITH_WARNINGS
        Assert.Equal(GateConstants.Decisions.AllowWithWarnings, result.Decision);
        Assert.Equal(GateConstants.VersionBumps.Major, result.RecommendedBump);
        Assert.Single(result.ImpactedAssets);
    }

    [Fact]
    public void Completely_Identical_Versions_Result_In_Allow_And_Patch_Bump()
    {
        var current = new MetadataObject("ds1", "1.0.0", "team", null, new[]
        {
            new FieldDefinition("name", "string", Array.Empty<string>(), "active", "Name")
        });
        var proposed = new MetadataObject("ds1", "DRAFT", "team", null, new[]
        {
            new FieldDefinition("name", "string", Array.Empty<string>(), "active", "Name")
        });

        var result = _evaluator.Evaluate(current, proposed, Array.Empty<Asset>());

        Assert.Equal(GateConstants.Decisions.Allow, result.Decision);
        Assert.Equal(GateConstants.VersionBumps.Patch, result.RecommendedBump);
        Assert.Empty(result.Changes);
        Assert.Empty(result.ImpactedAssets);
        Assert.Empty(result.Violations);
    }
}
