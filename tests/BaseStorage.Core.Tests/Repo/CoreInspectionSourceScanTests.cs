namespace BaseStorage.Core.Tests.Repo;

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-4 (ADR-GWIN-032, ADR-GWIN-033): the Inspection sources read bases only through BaseCache, name no game or Plugin type, read no
/// wall-clock time or culture, and keep every user-facing word in the formatter and the string table. Sources are comment-stripped and CRLF-normalised.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreInspectionSourceScanTests
{
    private const string InspectionFolder = "src/BaseStorage.Core/Bases/Inspection";
    private const string InspectionFile = "src/BaseStorage.Core/Bases/Inspection/HoverInspector.cs";

    private static readonly string[] ForbiddenNames =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "Valheim", "ZDO", "ZNet", "BaseStorage.Plugin", "System.Linq",
        "DateTime", "Stopwatch", "CultureInfo.CurrentCulture", "StationGraph", "BaseMembership", "IsInRange(", "TryFindSeed",
    };

    private static readonly string[] UserFacingLiterals = { "[In base", "Not in a base", "\"Keep\"", "chests]", "station]" };

    // GW-U-550 (AC-4): the inspection folder holds exactly HoverInspector.cs, and the scan read it.
    [Fact]
    public void TheInspectionFolderHoldsExactlyHoverInspector()
    {
        string[] files = Directory.GetFiles(RepoRoot.Combine(InspectionFolder), "*.cs")
            .Select(file => Path.GetFileName(file) ?? string.Empty)
            .OrderBy(name => name, System.StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "HoverInspector.cs" }, files);
        Assert.False(string.IsNullOrWhiteSpace(RepoRoot.ReadText(InspectionFile)), "The scan did not read HoverInspector.cs.");
    }

    // GW-U-550 (AC-4): no game, Plugin, wall-clock, culture, or base-internal name, and no user-facing literal.
    [Fact]
    public void TheInspectionSourceNamesNoGameTypeAndHasNoUserFacingLiteral()
    {
        string text = PluginSourceScan.StripComments(RepoRoot.ReadText(InspectionFile));
        Assert.False(string.IsNullOrWhiteSpace(text), "The scan read an empty HoverInspector.cs.");

        foreach (string name in ForbiddenNames)
        {
            Assert.DoesNotContain(name, text);
        }

        foreach (string literal in UserFacingLiterals)
        {
            Assert.DoesNotContain(literal, text);
        }

        string[] stringKeysUsed = Regex.Matches(text, @"StringKeys\.(\w+)")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToArray();
        Assert.All(stringKeysUsed, key => Assert.Equal("ChestCategoryKeep", key));
    }

    // GW-U-550 (AC-4): every static field is readonly or const.
    [Fact]
    public void EveryStaticFieldIsReadonlyOrConst()
    {
        string text = PluginSourceScan.StripComments(RepoRoot.ReadText(InspectionFile));

        string[] mutableStatics = Regex.Matches(
                text,
                @"(?m)^\s*(?:(?:public|private|internal|protected)\s+)?static\s+(?!readonly\b|const\b|class\b|sealed\b)[\w<>\[\],?\s]+\s+\w+\s*(?:=[^;]*)?;")
            .Select(match => match.Value.Trim())
            .ToArray();

        Assert.Empty(mutableStatics);
    }
}
