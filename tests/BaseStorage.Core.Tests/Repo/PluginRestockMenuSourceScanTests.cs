namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>GW-U-2550 to 2552: the Restock button, the layout the menu applies, and the click path (S-QR-002 ADR-GWIN-175, P6 and P7).</summary>
public sealed class PluginRestockMenuSourceScanTests
{
    private const string MenuFile = "src/BaseStorage.Plugin/BaseMenu/BaseMenuPanel.cs";
    private const string RunnerFile = "src/BaseStorage.Plugin/Organize/OrganizeRunner.cs";

    /// <summary>GW-U-2550: the Restock button is one vanilla button, placed after Deposit and before Mark, with the menu.restock id and the click handler.</summary>
    [Fact]
    public void TheRestockButtonIsAVanillaButtonNextToPutAway()
    {
        string menu = RepoRoot.ReadText(MenuFile);
        Assert.Contains("RestockId = \"menu.restock\"", menu, StringComparison.Ordinal);

        string[] creates = menu.Replace("\r\n", "\n").Split('\n')
            .Where(line => line.Contains("VanillaButtonFactory.Create(", StringComparison.Ordinal))
            .ToArray();
        string restock = Assert.Single(creates, line => line.Contains("VanillaButtonFactory.Create(gui.m_stackAllButton, parent, RestockId, StringKeys.MenuRestock, OnRestockClick)", StringComparison.Ordinal));
        int at = Array.IndexOf(creates, restock);
        Assert.True(at > 0 && at < creates.Length - 1, "The Restock create is not between two other creates.");
        Assert.Contains("Deposit", creates[at - 1], StringComparison.Ordinal);
        Assert.Contains("Mark", creates[at + 1], StringComparison.Ordinal);
    }

    /// <summary>GW-U-2551: the menu applies the Core layout from one Fit, reads the measured widths, pins Top with a minus, and keeps no fixed offset for the buttons.</summary>
    [Fact]
    public void TheMenuUsesTheCoreLayoutAndNoFixedOffsets()
    {
        string menu = RepoRoot.ReadText(MenuFile);
        Assert.True(Regex.Matches(menu, Regex.Escape("MenuButtonLayout.Arrange(")).Count == 1);
        Assert.Contains("sizeDelta.x", menu, StringComparison.Ordinal);
        Assert.Contains("(X, -Top)", menu, StringComparison.Ordinal);
        Assert.Contains("FitBetween(", menu, StringComparison.Ordinal);

        foreach (string offset in new[] { "Margin + 156f", "Margin + 348f", "Margin + 444f", "Margin + 136f", "Margin + 302f", "Margin + 428f" })
        {
            Assert.DoesNotContain(offset, menu, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(new Regex(@"SearchTop\b[^;]*(Pin|Place|anchoredPosition)"), menu);
    }

    /// <summary>GW-U-2552: the click path adds no notice; the click forwards to the runner, which refuses with NoPlayer when there is no coordinator.</summary>
    [Fact]
    public void TheRestockClickForwardsToTheRunnerOnly()
    {
        string menu = RepoRoot.ReadText(MenuFile);
        Assert.DoesNotContain("Notices.Show(", menu.Substring(Math.Max(0, menu.IndexOf("OnRestockClick", StringComparison.Ordinal))), StringComparison.Ordinal);
        Assert.Contains("OrganizeRunner.StartRestockAt(", menu, StringComparison.Ordinal);

        string runner = RepoRoot.ReadText(RunnerFile);
        Assert.Contains("public static OrganizeStartOutcome StartRestockAt(WorldPosition position)", runner, StringComparison.Ordinal);
        Assert.Contains("NoPlayer", runner, StringComparison.Ordinal);
        Assert.Contains("coordinator.StartRestock(position)", runner, StringComparison.Ordinal);
    }
}
