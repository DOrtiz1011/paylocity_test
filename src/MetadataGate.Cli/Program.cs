using System.Text.Json;
using MetadataGate.Core.Models;
using MetadataGate.Core.Services;

namespace MetadataGate.Cli;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented               = true,
        Encoder                     = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        string? currentPath  = null;
        string? proposedPath = null;
        string? assetsPath   = null;

        var positionalArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "--current" or "-c" && i + 1 < args.Length)
            {
                currentPath = args[++i];
            }
            else if (arg is "--proposed" or "-p" && i + 1 < args.Length)
            {
                proposedPath = args[++i];
            }
            else if (arg is "--assets" or "-a" && i + 1 < args.Length)
            {
                assetsPath = args[++i];
            }
            else if (!arg.StartsWith("-"))
            {
                positionalArgs.Add(arg);
            }
        }

        // Fallback to positional arguments if flags were not provided
        if (currentPath  == null && positionalArgs.Count > 0) currentPath  = positionalArgs[0];
        if (proposedPath == null && positionalArgs.Count > 1) proposedPath = positionalArgs[1];
        if (assetsPath   == null && positionalArgs.Count > 2) assetsPath   = positionalArgs[2];

        if (string.IsNullOrWhiteSpace(currentPath)  ||
            string.IsNullOrWhiteSpace(proposedPath) ||
            string.IsNullOrWhiteSpace(assetsPath))
        {
            Console.Error.WriteLine("Error: Missing required inputs. All three files (current, proposed, assets) must be specified.");
            PrintUsage();
            return 1;
        }

        if (!File.Exists(currentPath))
        {
            Console.Error.WriteLine($"Error: Current metadata file not found at: {currentPath}");
            return 1;
        }

        if (!File.Exists(proposedPath))
        {
            Console.Error.WriteLine($"Error: Proposed metadata file not found at: {proposedPath}");
            return 1;
        }

        if (!File.Exists(assetsPath))
        {
            Console.Error.WriteLine($"Error: Assets file not found at: {assetsPath}");
            return 1;
        }

        try
        {
            var currentJson  = await File.ReadAllTextAsync(currentPath);
            var proposedJson = await File.ReadAllTextAsync(proposedPath);
            var assetsJson   = await File.ReadAllTextAsync(assetsPath);

            var current  = JsonSerializer.Deserialize<MetadataObject>(currentJson, JsonOptions)  ?? throw new InvalidOperationException($"Failed to deserialize {currentPath}");
            var proposed = JsonSerializer.Deserialize<MetadataObject>(proposedJson, JsonOptions) ?? throw new InvalidOperationException($"Failed to deserialize {proposedPath}");
            var assets   = JsonSerializer.Deserialize<List<Asset>>(assetsJson, JsonOptions)      ?? throw new InvalidOperationException($"Failed to deserialize {assetsPath}");

            var evaluator = new MetadataGateEvaluator();
            var result    = evaluator.Evaluate(current, proposed, assets);

            var outputJson = JsonSerializer.Serialize(result, JsonOptions);

            Console.WriteLine(outputJson);

            return result.Decision switch
            {
                GateConstants.Decisions.Block             => 2,
                GateConstants.Decisions.AllowWithWarnings => 0,
                _ => 0
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error evaluating metadata change gate: {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
Metadata Change Gate CLI
-------------------------
Evaluates proposed metadata changes against downstream assets and governance rules.

Usage:
  dotnet run --project src/MetadataGate.Cli -- --current <file> --proposed <file> --assets <file>
  or
  dotnet run --project src/MetadataGate.Cli -- <current.json> <proposed.json> <assets.json>

Options:
  -c, --current    Path to currently published metadata JSON file
  -p, --proposed   Path to proposed metadata JSON file
  -a, --assets     Path to downstream assets JSON file
  -h, --help       Show help and usage instructions

Exit Codes:
  0 : Decision is ALLOW or ALLOW_WITH_WARNINGS
  1 : Evaluation error or invalid arguments
  2 : Decision is BLOCK
""");
    }
}
