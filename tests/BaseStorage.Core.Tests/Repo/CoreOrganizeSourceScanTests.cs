namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-15 (contract 0.1, 0.2 P1, P12, 4.6 and 3.1; ADR-GWIN-038 and ADR-GWIN-039): the Organize sources are pure, read no settings or clock,
/// use no culture-sensitive or Linq code, keep static state read-only, call the auto-assign lookup once, and spell each Reason once.
/// The scans strip comments and normalise CRLF. They are text checks, not compiler checks.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreOrganizeSourceScanTests
{
    private const string OrganizeFolder = "src/BaseStorage.Core/Organize";

    private static readonly string[] ExpectedFiles =
    {
        "ChestLayout.cs",
        "ChestRole.cs",
        "ItemKey.cs",
        "LayoutCompactor.cs",
        "NoRoomEntry.cs",
        "OrganizePlan.cs",
        "OrganizePlanner.cs",
        "PlannedMove.cs",
        "SkipReason.cs",
        "SkippedChest.cs",
        "WorkingChest.cs",
        "WorkingStack.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine",
        "BepInEx",
        "HarmonyLib",
        "Valheim",
        "ZDO",
        "BaseStorage.Plugin",
        "ILogSink",
        "IClock",
        "BaseStorageSettings",
        "Stopwatch",
        "DateTime",
        "Random",
        "Environment.",
        "CultureInfo",
        "ToLower",
        "ToUpper",
        "CurrentCulture",
        "InvariantCulture",
        "using System.Linq",
        "System.IO",
        "System.Threading",
    };

    private static readonly string[] ReasonLiterals =
    {
        "\"Ineligible: tombstone\"",
        "\"Ineligible: vehicle\"",
        "\"Ineligible: not player-built\"",
        "\"Ineligible: access denied\"",
        "\"Keep\"",
        "\"In use by \"",
        "\"In use by another player\"",
        "\"Invalid contents: more stacks than slots\"",
        "\"Invalid contents: stack above max stack\"",
    };

    // A static field declaration that is neither readonly nor const. Operators and expression-bodied members are excluded.
    private static readonly Regex MutableStaticField = new Regex(
        @"\bstatic\s+(?!readonly\s|const\s)(?![^\n]*\boperator\b)[^(){}=;]*(?:;|=(?!>))",
        RegexOptions.None);

    // GW-U-576
    [Fact]
    public void ScanReadsExactlyTheTwelveOrganizeFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(OrganizeFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedFiles.OrderBy(name => name, StringComparer.Ordinal), names);
        foreach (string relative in OrganizeFiles())
        {
            Assert.NotEmpty(Strip(RepoRoot.ReadText(relative)).Trim());
        }
    }

    // GW-U-576
    [Fact]
    public void NoOrganizeFileUsesAForbiddenToken()
    {
        var offenders = new List<string>();

        foreach (string relative in OrganizeFiles())
        {
            string code = Strip(RepoRoot.ReadText(relative));
            foreach (string token in ForbiddenTokens)
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(relative + ": " + token);
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-576
    [Fact]
    public void EveryStaticFieldIsReadOnlyOrConst()
    {
        var offenders = new List<string>();

        foreach (string relative in OrganizeFiles())
        {
            string code = Strip(RepoRoot.ReadText(relative));
            foreach (Match match in MutableStaticField.Matches(code))
            {
                offenders.Add(relative + ": " + match.Value.Trim());
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-576
    [Fact]
    public void GetCategoryChestsIsCalledFromExactlyOneSite()
    {
        int sites = OrganizeFiles()
            .Sum(relative => Regex.Matches(Strip(RepoRoot.ReadText(relative)), Regex.Escape("GetCategoryChests(")).Count);

        Assert.Equal(1, sites);
    }

    // GW-U-576
    [Fact]
    public void EachContractReasonLiteralAppearsExactlyOnce()
    {
        string all = string.Concat(OrganizeFiles().Select(relative => Strip(RepoRoot.ReadText(relative))));

        foreach (string literal in ReasonLiterals)
        {
            Assert.Single(Regex.Matches(all, Regex.Escape(literal)));
        }
    }

    /// <summary>Root-relative, forward-slash paths of the Organize source files, in ordinal order.</summary>
    private static string[] OrganizeFiles() =>
        Directory.EnumerateFiles(RepoRoot.Combine(OrganizeFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => OrganizeFolder + "/" + Path.GetFileName(file))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string normalised = text.Replace("\r\n", "\n");
        string withoutBlocks = Regex.Replace(normalised, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
