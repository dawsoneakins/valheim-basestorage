namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>AC-15: the gate commands never pass DeployToGame, and the Plugin default is off.</summary>
[Trait("Category", "Repo")]
public sealed class GateCommandsTests
{
    private static readonly string[] GateKeys = ["typecheck", "test", "lint", "build"];

    // GW-U-021
    [Fact]
    public void ConfigTextDoesNotMentionDeployToGame()
    {
        string config = RepoRoot.ReadText(ContractPaths.PipelineConfig);

        Assert.False(
            config.Contains("deploytogame", StringComparison.OrdinalIgnoreCase),
            ".pipeline/config.yml must not mention DeployToGame.");
    }

    // GW-U-021
    [Fact]
    public void FourContractGateStringsDoNotMentionDeployToGame()
    {
        string[] gates = ContractGateStrings();

        Assert.True(
            gates.Length == GateKeys.Length,
            $"Expected {GateKeys.Length} gate strings ({string.Join(", ", GateKeys)}) in build-layout.yml, found {gates.Length}: "
            + (gates.Length == 0 ? "(none)" : string.Join(" | ", gates)));
        foreach (string gate in gates)
        {
            Assert.False(
                gate.Contains("deploytogame", StringComparison.OrdinalIgnoreCase),
                "Gate command must not pass DeployToGame: " + gate);
        }
    }

    // GW-U-021
    [Fact]
    public void PluginDeployToGameDefaultsToFalse()
    {
        string[] values = RepoRoot.ValuesOf(ContractPaths.PluginProject, "DeployToGame");

        Assert.Equal(new[] { "false" }, values);
    }

    // GW-U-021
    [Fact]
    public void NoTrackedMsBuildFileSetsDeployToGameToTrue()
    {
        IReadOnlyList<string> files = RepoRoot.MsBuildFiles();
        Assert.Contains(ContractPaths.PluginProject, files);

        var offenders = new List<string>();
        foreach (string file in files)
        {
            foreach (var element in RepoRoot.Named(RepoRoot.LoadXml(file), "DeployToGame"))
            {
                if (string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add(file);
                }
            }
        }

        Assert.True(offenders.Count == 0, "DeployToGame set to true in: " + string.Join(", ", offenders));
    }

    // Reads the four gate lines under "gates:" in contracts/build-layout.yml, the source of the gate strings.
    private static string[] ContractGateStrings()
    {
        string[] lines = RepoRoot.ReadText(ContractPaths.BuildLayoutContract).Split('\n');
        int start = Array.IndexOf(lines, "gates:");
        Assert.True(start >= 0, "The gates: block is missing from build-layout.yml.");

        var gates = new List<string>();
        foreach (string line in lines.Skip(start + 1).TakeWhile(l => l.StartsWith("  ", StringComparison.Ordinal)))
        {
            string trimmed = line.Trim();
            int colon = trimmed.IndexOf(':');
            if (colon > 0 && GateKeys.Contains(trimmed[..colon]))
            {
                gates.Add(trimmed[(colon + 1)..].Trim());
            }
        }

        return gates.ToArray();
    }
}
