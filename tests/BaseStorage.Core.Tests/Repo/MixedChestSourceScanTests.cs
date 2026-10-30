namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;

/// <summary>
/// S-A-001 (AC-9, AC-10; mixed-chest-category-contracts.md 4, 5.3, 6.9 and MC13): Put away never runs the mixed-chest step or reads the
/// setting, and the Plugin names none of the new members (the setting reaches it only through the catalog loop). Text scans, with comments
/// removed where the contract allows comments.
/// </summary>
[Trait("Category", "Repo")]
public sealed class MixedChestSourceScanTests
{
    private const string DepositPlannerFile = "src/BaseStorage.Core/Organize/Deposit/DepositPlanner.cs";
    private const string CoordinatorFile = "src/BaseStorage.Core/Organize/Run/OrganizeRunCoordinator.cs";
    private const string DepositBranchStart = "if (asDeposit)";
    private const string DepositBranchEnd = "else";
    private const string SettingName = "AutoAssignMixedChests";
    private const string StepName = "AssignMixedChests";

    // GW-U-2447 (AC-9, 6.9 row 4, MC13): the Deposit planner and the Deposit branch of the coordinator never name the mixed-chest members.
    [Fact]
    public void DepositPlannerAndTheDepositBranchNeverNameTheMixedChestMembers()
    {
        string planner = PluginSourceScan.StripComments(RepoRoot.ReadText(DepositPlannerFile));
        Assert.False(planner.Contains(StepName, StringComparison.Ordinal), "DepositPlanner.cs names AssignMixedChests.");
        Assert.False(planner.Contains(SettingName, StringComparison.Ordinal), "DepositPlanner.cs names AutoAssignMixedChests.");

        string coordinator = RepoRoot.ReadText(CoordinatorFile);
        int start = coordinator.IndexOf(DepositBranchStart, StringComparison.Ordinal);
        Assert.True(start >= 0, "OrganizeRunCoordinator.cs has no 'if (asDeposit)' branch.");
        int end = coordinator.IndexOf(DepositBranchEnd, start, StringComparison.Ordinal);
        Assert.True(end > start, "The 'if (asDeposit)' branch of OrganizeRunCoordinator.cs has no 'else'.");
        string branch = PluginSourceScan.StripComments(coordinator.Substring(start, end - start));
        Assert.False(branch.Contains(SettingName, StringComparison.Ordinal), "The Deposit branch of OrganizeRunCoordinator.cs names AutoAssignMixedChests.");
    }

    // GW-U-2452 (AC-10, R4, 5.3): no Plugin source names the setting or the step; the setting reaches the Plugin only through the catalog loop.
    [Fact]
    public void NoPluginSourceNamesTheMixedChestMembers()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        Assert.NotEmpty(sources);

        foreach (KeyValuePair<string, string> pair in sources)
        {
            Assert.False(pair.Value.Contains(SettingName, StringComparison.Ordinal), pair.Key + " names AutoAssignMixedChests.");
            Assert.False(pair.Value.Contains(StepName, StringComparison.Ordinal), pair.Key + " names AssignMixedChests.");
        }
    }
}
