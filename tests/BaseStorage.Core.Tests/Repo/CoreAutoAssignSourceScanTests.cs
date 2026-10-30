namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-10 and AC-9 (contract 0.2 A1, A3; ADR-GWIN-037): the AutoAssign sources are pure and never hard-code or read the threshold. The scans
/// strip comments and normalise CRLF, and assert that they read the 5 files of ADR-GWIN-036. They are text checks, not compiler checks.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreAutoAssignSourceScanTests
{
    private const string AutoAssignFolder = "src/BaseStorage.Core/Categories/AutoAssign";
    private const string CategoriesFolder = "src/BaseStorage.Core/Categories";
    private const string ResolverFile = AutoAssignFolder + "/AutoAssigner.cs";

    private static readonly string[] ExpectedFiles =
    {
        "AutoAssigner.cs",
        "AutoAssignOutcome.cs",
        "AutoAssignResult.cs",
        "ChestAutoAssignment.cs",
        "OverflowPool.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine",
        "BepInEx",
        "HarmonyLib",
        "Valheim",
        "ZDO",
        "ILogSink",
        "IClock",
        "Random",
        "DateTime",
        "Stopwatch",
        "System.IO",
        "System.Linq",
        "CultureInfo",
        "ToLower",
        "ToUpper",
        "IgnoreCase",
        "CurrentCulture",
        "InvariantCulture",
        "Sqrt",
        ".Flags",
        ".Quality",
        ".MaxStack",
        "basestorage.",
        "lock (",
        "Interlocked",
    };

    private static readonly string[] ForbiddenThresholdSourceTokens =
    {
        "BaseStorageSettings",
        "SettingKeys",
        "ISettingsSource",
        "BaseStorage.Core.Settings",
    };

    // A static field declaration that is neither readonly nor const. Operators and expression-bodied members are excluded.
    private static readonly Regex MutableStaticField = new Regex(
        @"\bstatic\s+(?!readonly\s|const\s)(?![^\n]*\boperator\b)[^(){}=;]*(?:;|=(?!>))",
        RegexOptions.None);

    private static readonly Regex NumericFifty = new Regex(@"\b50\b", RegexOptions.None);

    // GW-U-535
    [Fact]
    public void ScanReadsExactlyTheFiveAutoAssignFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(AutoAssignFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedFiles.OrderBy(name => name, StringComparer.Ordinal), names);
        foreach (string relative in AutoAssignFiles())
        {
            Assert.NotEmpty(Strip(RepoRoot.ReadText(relative)).Trim());
        }
    }

    // GW-U-535
    [Fact]
    public void NoAutoAssignFileUsesAForbiddenToken()
    {
        var offenders = new List<string>();

        foreach (string relative in AutoAssignFiles())
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

    // GW-U-535
    [Fact]
    public void EveryStaticFieldIsReadOnlyOrConst()
    {
        var offenders = new List<string>();

        foreach (string relative in AutoAssignFiles())
        {
            string code = Strip(RepoRoot.ReadText(relative));
            foreach (Match match in MutableStaticField.Matches(code))
            {
                offenders.Add(relative + ": " + match.Value.Trim());
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-535
    [Fact]
    public void TheCategoriesTopFolderStillHasItsNineFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(CategoriesFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(9, names.Length);
    }

    // GW-U-537
    [Fact]
    public void NoAutoAssignFileContainsTheThresholdLiteralFifty()
    {
        var offenders = new List<string>();

        foreach (string relative in AutoAssignFiles())
        {
            if (NumericFifty.IsMatch(Strip(RepoRoot.ReadText(relative))))
            {
                offenders.Add(relative);
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-537
    [Fact]
    public void NoAutoAssignFileReadsTheSettingsOrItsKeys()
    {
        var offenders = new List<string>();

        foreach (string relative in AutoAssignFiles())
        {
            string code = Strip(RepoRoot.ReadText(relative));
            foreach (string token in ForbiddenThresholdSourceTokens)
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(relative + ": " + token);
                }
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-537
    [Fact]
    public void TheResolverTakesTheContractedThresholdParameterName()
    {
        string code = Strip(RepoRoot.ReadText(ResolverFile));

        Assert.Contains("autoAssignThresholdPct", code, StringComparison.Ordinal);
    }

    /// <summary>Root-relative, forward-slash paths of the AutoAssign source files, in ordinal order.</summary>
    private static string[] AutoAssignFiles() =>
        Directory.EnumerateFiles(RepoRoot.Combine(AutoAssignFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => AutoAssignFolder + "/" + Path.GetFileName(file))
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
