using System.Text.Json;
using MetadataGate.Core.Models;
using MetadataGate.Core.Services;

namespace MetadataGate.Tests;

public class FixtureRegressionTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly MetadataGateEvaluator _evaluator = new();

    private static string FindFixturesDirectory()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "fixtures");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(current);
            current = parent?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find 'fixtures' directory.");
    }

    public static IEnumerable<object[]> GetFixtureScenarios()
    {
        var fixturesDir = FindFixturesDirectory();
        var scenarioDirs = Directory.GetDirectories(fixturesDir)
            .Where(d => File.Exists(Path.Combine(d, "proposed.json")) && File.Exists(Path.Combine(d, "expected.json")))
            .OrderBy(d => d);

        foreach (var dir in scenarioDirs)
        {
            yield return new object[] { Path.GetFileName(dir) };
        }
    }

    [Theory]
    [MemberData(nameof(GetFixtureScenarios))]
    public void Evaluates_Fixture_Scenario_Accurately(string scenarioName)
    {
        var fixturesDir = FindFixturesDirectory();
        var currentJson = File.ReadAllText(Path.Combine(fixturesDir, "current.json"));
        var assetsJson = File.ReadAllText(Path.Combine(fixturesDir, "assets.json"));

        var scenarioDir = Path.Combine(fixturesDir, scenarioName);
        var proposedJson = File.ReadAllText(Path.Combine(scenarioDir, "proposed.json"));
        var expectedJson = File.ReadAllText(Path.Combine(scenarioDir, "expected.json"));

        var current = JsonSerializer.Deserialize<MetadataObject>(currentJson, JsonOptions)!;
        var proposed = JsonSerializer.Deserialize<MetadataObject>(proposedJson, JsonOptions)!;
        var assets = JsonSerializer.Deserialize<List<Asset>>(assetsJson, JsonOptions)!;
        var expected = JsonSerializer.Deserialize<EvaluationResult>(expectedJson, JsonOptions)!;

        var actual = _evaluator.Evaluate(current, proposed, assets);

        // 1. Verify decision and recommended bump
        Assert.Equal(expected.Decision, actual.Decision);
        Assert.Equal(expected.RecommendedBump, actual.RecommendedBump);

        // 2. Verify changes (order-agnostic)
        Assert.Equal(expected.Changes.Count, actual.Changes.Count);
        foreach (var expectedChange in expected.Changes)
        {
            Assert.Contains(actual.Changes, actualChange =>
                actualChange.Field == expectedChange.Field &&
                actualChange.ChangeType == expectedChange.ChangeType &&
                actualChange.Breaking == expectedChange.Breaking);
        }

        // 3. Verify impacted assets (order-agnostic)
        Assert.Equal(expected.ImpactedAssets.Count, actual.ImpactedAssets.Count);
        foreach (var expectedImpact in expected.ImpactedAssets)
        {
            Assert.Contains(actual.ImpactedAssets, actualImpact =>
                actualImpact.AssetId == expectedImpact.AssetId &&
                actualImpact.Type == expectedImpact.Type &&
                actualImpact.Owner == expectedImpact.Owner &&
                actualImpact.Field == expectedImpact.Field);
        }

        // 4. Verify violations (order-agnostic: rule, field, blocking)
        Assert.Equal(expected.Violations.Count, actual.Violations.Count);
        foreach (var expectedViolation in expected.Violations)
        {
            Assert.Contains(actual.Violations, actualViolation =>
                actualViolation.Rule == expectedViolation.Rule &&
                actualViolation.Field == expectedViolation.Field &&
                actualViolation.Blocking == expectedViolation.Blocking);
        }
    }
}
