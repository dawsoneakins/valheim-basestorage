namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Deposit contract 0.2 (D7 pure, ADR-GWIN-085): the Deposit sources are the 8 contracted files, name no game, Plugin, wall-clock,
/// culture, Linq or logging type, keep static state read-only, and spell the hotbar row once. Comments are stripped and CRLF normalised.
/// The S-E004-006 Organize scan reads the top directory only, so it stays untouched.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreDepositSourceScanTests
{
    private const string DepositFolder = "src/BaseStorage.Core/Organize/Deposit";

    private static readonly string[] ExpectedFiles =
    {
        "DepositExclusion.cs",
        "DepositExclusions.cs",
        "DepositPlan.cs",
        "DepositPlanner.cs",
        "ExcludedStack.cs",
        "InventoryItemTraits.cs",
        "PlayerInventorySnapshot.cs",
        "PlayerInventoryStack.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "Valheim", "ZDO", "ZNet", "BaseStorage.Plugin", "ILogSink", "IClock", "Stopwatch", "DateTime",
        "Random", "Environment.", "CultureInfo", "ToLower", "ToUpper", "CurrentCulture", "InvariantCulture", "using System.Linq", "System.IO",
        "System.Threading", "System.Net",
    };

    // A static field declaration that is neither readonly nor const. Operators and expression-bodied members are excluded.
    private static readonly Regex MutableStaticField = new Regex(
        @"\bstatic\s+(?!readonly\s|const\s)(?![^\n]*\boperator\b)[^(){}=;]*(?:;|=(?!>))",
        RegexOptions.None);

    // GW-U-1079 (D7): exactly the 8 files, none naming a forbidden type, every static field read-only, the hotbar row spelled once.
    [Fact]
    public void TheDepositSourcesAreTheEightPureFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(DepositFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray()!;
        Assert.Equal(ExpectedFiles, names);

        int hotbarRows = 0;
        foreach (string name in ExpectedFiles)
        {
            string text = PluginSourceScan.StripComments(RepoRoot.ReadText(DepositFolder + "/" + name));
            Assert.False(string.IsNullOrWhiteSpace(text), "The scan read an empty " + name + ".");
            Assert.StartsWith("namespace BaseStorage.Core.Organize.Deposit;", text.TrimStart(), StringComparison.Ordinal);
            foreach (string token in ForbiddenTokens)
            {
                Assert.False(text.Contains(token, StringComparison.Ordinal), name + " names " + token + ".");
            }

            Assert.False(MutableStaticField.IsMatch(text), name + " has a static field that is neither readonly nor const.");
            hotbarRows += Regex.Matches(text, @"\bHotbarRow\s*=\s*0\b").Count;
        }

        Assert.Equal(1, hotbarRows);
        Assert.False(RepoRoot.ReadText("src/BaseStorage.Core/Organize/OrganizePlanner.cs").Contains("Deposit.", StringComparison.Ordinal), "OrganizePlanner must not reference the Deposit namespace.");
    }
}
