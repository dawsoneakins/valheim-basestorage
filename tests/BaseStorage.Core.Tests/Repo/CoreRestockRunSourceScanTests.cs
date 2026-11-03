namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Text.RegularExpressions;

/// <summary>GW-U-2583 and 2618: the single restock sites of the coordinator, and the Restock.Run folder shape (S-QR-002 ADR-GWIN-173, RR4 and RR15).</summary>
public sealed class CoreRestockRunSourceScanTests
{
    private const string Coordinator = "src/BaseStorage.Core/Organize/Run/OrganizeRunCoordinator.cs";

    /// <summary>GW-U-2583: the one restock plan call takes the gathered assignments and the settings, and the coordinator builds no dictionary of its own.</summary>
    [Fact]
    public void TheRestockPlanUsesTheGatheredAssignments()
    {
        string text = RepoRoot.ReadText(Coordinator);
        Assert.True(Regex.Matches(text, Regex.Escape("RestockPlanner.Plan(")).Count == 1);
        int at = text.IndexOf("RestockPlanner.Plan(", StringComparison.Ordinal);
        string call = text.Substring(at, Math.Min(200, text.Length - at));
        Assert.Contains("facts.Assignments", call, StringComparison.Ordinal);
        Assert.Contains("EligibilityOptions.FromSettings(settings)", call, StringComparison.Ordinal);
        Assert.DoesNotContain("new Dictionary<ContainerId", text, StringComparison.Ordinal);
    }

    /// <summary>GW-U-2618: each restock step has exactly one call site in the coordinator, and the Run folder holds exactly the five files with a namespace line, no forbidden token, no table text, and only readonly or const statics.</summary>
    [Fact]
    public void RR15_RestockAddsExactlyOneCallSiteEach()
    {
        string text = RepoRoot.ReadText(Coordinator);
        foreach (string pattern in new[] { @"RestockPlanner\.Plan\(", @"new RestockExecutor\(", @"port\.CaptureCarried\(", @"port\.CaptureChests\(", @"port\.CreateRestockLayer\(" })
        {
            Assert.True(Regex.Matches(text, pattern).Count == 1, "Expected exactly one call matching " + pattern + ".");
        }

        string[] files = System.IO.Directory.GetFiles(RepoRoot.Combine("src/BaseStorage.Core/Restock/Run"), "*.cs");
        Assert.True(files.Length == 5);
        foreach (string file in files)
        {
            string body = RepoRoot.ReadText("src/BaseStorage.Core/Restock/Run/" + System.IO.Path.GetFileName(file));
            Assert.StartsWith("namespace BaseStorage.Core.Restock.Run;", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Nothing to restock", body, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"\bstatic\s+(?!readonly|const|class|void|bool|int|string|double|float|long|object|async)[A-Za-z<>\[\],?]+\s+[A-Za-z_]+\s*(=|;)"), body);
        }
    }
}
