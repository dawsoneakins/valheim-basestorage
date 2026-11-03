namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Source scans of the S-QF-001 Plugin edits (ADR-GWIN-179, contract section 5): the feeder reads the real chest assignments and passes them to
/// eligibility and to the planner, the feed panel floors the max fuel for display, the stored key is unchanged, and no registration, resolve, RPC
/// or game member is added. Plugin behaviour is proven by these scans only (ADR-GWIN-180).
/// </summary>
public sealed class PluginFeedingSourceScanTests
{
    private const string FeederFile = "src/BaseStorage.Plugin/Feeding/StationFeeder.cs";
    private const string PanelFile = "src/BaseStorage.Plugin/Feeding/FeedPanel.cs";
    private const string AssignmentsField = @"static readonly Dictionary<ContainerId, CategoryAssignment>\s+(\w+)\s*=";

    // GW-U-2648
    [Fact]
    public void TheFeedPanelFloorsTheFloatMaxFuel()
    {
        string panel = PluginSourceScan.StripComments(RepoRoot.ReadText(PanelFile));
        string[] lines = panel.Split('\n');
        string[] statusLines = Array.FindAll(lines, line => line.Contains("StringKeys.FeedStatus,", StringComparison.Ordinal));
        Assert.Single(statusLines);

        string status = statusLines[0];
        Assert.Contains("Mathf.FloorToInt(state.MaxFuel)", status, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(status, @"state\.MaxFuel"));
        Assert.Contains("Mathf.CeilToInt(state.Fuel)", status, StringComparison.Ordinal);
        Assert.Contains("state.Queued", status, StringComparison.Ordinal);
        Assert.Contains("state.MaxQueued", status, StringComparison.Ordinal);
    }

    // GW-U-2655
    [Fact]
    public void TheFeederReadsAndWritesTheSameFeedKey()
    {
        string feeder = PluginSourceScan.StripComments(RepoRoot.ReadText(FeederFile));

        string read = PluginSourceScan.MethodBody(feeder, @"internal static FeedSelection Read\(");
        Assert.Contains("FeedSelection.Decode(", read, StringComparison.Ordinal);
        Assert.Contains("PersistenceKeys.StationFeed", read, StringComparison.Ordinal);

        string write = PluginSourceScan.MethodBody(feeder, @"internal static void Write\(");
        Assert.Contains("selection.Encode()", write, StringComparison.Ordinal);
        Assert.Contains("SetSelectionRpc", write, StringComparison.Ordinal);

        Assert.Contains("SetSelectionRpc = \"BaseStorage_SetFeed\";", feeder, StringComparison.Ordinal);
    }

    // GW-U-2677
    [Fact]
    public void TheFeederPassesRealAssignmentsToEligibilityAndThePlanner()
    {
        string feeder = PluginSourceScan.StripComments(RepoRoot.ReadText(FeederFile));
        Assert.DoesNotContain("NoAssignments", feeder, StringComparison.Ordinal);

        Assert.Single(Regex.Matches(feeder, @"static readonly Dictionary<ContainerId, CategoryAssignment>\s+\w+\s*="));
        Match field = Regex.Match(feeder, AssignmentsField);
        Assert.True(field.Success, "StationFeeder declares no static readonly Dictionary<ContainerId, CategoryAssignment> field.");
        string name = field.Groups[1].Value;

        string load = PluginSourceScan.MethodBody(feeder, @"internal static bool LoadChests\(");
        int clear = load.IndexOf(name + ".Clear()", StringComparison.Ordinal);
        int read = load.IndexOf("ChestCategoryWriter.Read(", StringComparison.Ordinal);
        int evaluate = load.IndexOf(
            "ContainerEligibility.Evaluate(resolution.Containers, " + name + ", ContainerPurpose.CraftAvailability, EligibilityOptions.FromSettings(config))",
            StringComparison.Ordinal);
        Assert.True(clear >= 0, "LoadChests does not clear " + name + ".");
        Assert.True(read > clear, "LoadChests does not read the chest categories after clearing " + name + ".");
        Assert.True(evaluate > read, "LoadChests does not evaluate eligibility with " + name + " after reading the categories.");

        string tryStart = PluginSourceScan.MethodBody(feeder, @"private static bool TryStart\(");
        Assert.Contains(
            "FeedPlanner.Next(StateOf(station), selection, Chests, " + name + ", EligibilityOptions.FromSettings(",
            tryStart,
            StringComparison.Ordinal);

        string target = PluginSourceScan.StripComments(RepoRoot.ReadText("src/BaseStorage.Plugin/Feeding/SmelterFeedTarget.cs"));
        Assert.Contains("FeedStationState.SmelterClass(", target, StringComparison.Ordinal);

        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.Equal(0, PluginSourceScan.CountMatches(sources, @"new FeedStationState\("));
    }

    // GW-U-2678
    [Fact]
    public void TheFeederAddsNoRegistrationOrResolve()
    {
        string feeder = PluginSourceScan.StripComments(RepoRoot.ReadText(FeederFile));
        Assert.Single(Regex.Matches(feeder, @"\.Register<"));
        Assert.Single(Regex.Matches(feeder, @"\.Resolve\("));
    }
}
