namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-1 (contract C1, C2, spec NFR purity): the Categories files are pure. The scans strip comments, normalise CRLF, and assert
/// that they read the 9 files of ADR-GWIN-018. Each scan targets the text of the files, and none of them is a compiler check.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreCategoriesSourceScanTests
{
    private const string CategoriesFolder = "src/BaseStorage.Core/Categories";

    private static readonly string[] ExpectedFiles =
    {
        "CategoryCatalog.cs",
        "CategoryCatalogBuilder.cs",
        "CategoryDefinition.cs",
        "CategoryFile.cs",
        "CategoryParseIssue.cs",
        "CategoryParseIssueKind.cs",
        "CategoryParseResult.cs",
        "FallbackCategories.cs",
        "ItemKind.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "System.IO.File",
        "Directory.",
        "FileStream",
        "StreamReader",
        "CultureInfo",
        "ToLower",
        "ToUpper",
        "CurrentCulture",
        "InvariantCulture",
        "Console.",
        "ILogSink",
        "IClock",
        "UnityEngine",
        "BepInEx",
        "HarmonyLib",
    };

    // A static field declaration that is neither readonly nor const. Operators and expression-bodied members are excluded.
    private static readonly Regex MutableStaticField = new Regex(
        @"\bstatic\s+(?!readonly\s|const\s)(?![^\n]*\boperator\b)[^(){}=;]*(?:;|=(?!>))",
        RegexOptions.None);

    // GW-U-160
    [Fact]
    public void ScanReadsTheNineCategoriesFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(CategoriesFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedFiles, names);
    }

    // GW-U-160
    [Fact]
    public void NoCategoriesFileUsesFileIoClockLoggingCultureOrUnityTokens()
    {
        var offenders = new List<string>();

        foreach (string relative in CategoriesFiles())
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

    // GW-U-160
    [Fact]
    public void EveryStaticFieldIsReadOnlyOrConst()
    {
        var offenders = new List<string>();

        foreach (string relative in CategoriesFiles())
        {
            string code = Strip(RepoRoot.ReadText(relative));
            foreach (Match match in MutableStaticField.Matches(code))
            {
                offenders.Add(relative + ": " + match.Value.Trim());
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>Root-relative, forward-slash paths of the Categories source files, in ordinal order.</summary>
    private static string[] CategoriesFiles() =>
        Directory.EnumerateFiles(RepoRoot.Combine(CategoriesFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => CategoriesFolder + "/" + Path.GetFileName(file))
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
