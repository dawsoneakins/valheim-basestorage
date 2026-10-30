namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1230 (contract S-E004-003 section 7.1, Core purity): the 8 ChestCategories files stay free of game tokens (raw text,
/// comments included), of system-time and I/O tokens (comments removed), import only the allowed namespaces, and hold no English
/// strings-table text. Each scan asserts the file names it read, so an empty folder cannot pass.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreChestCategoriesSourceScanTests
{
    private const string Folder = "src/BaseStorage.Core/ChestCategories";

    private static readonly string[] ExpectedFiles =
    {
        "CategoryCycle.cs",
        "CategoryWriteOutcome.cs",
        "CategoryWriteResult.cs",
        "CategoryWriteRules.cs",
        "CategoryWriteStatus.cs",
        "ChestCategoryReader.cs",
        "PendingCategoryWrite.cs",
        "PendingWriteAction.cs",
    };

    private static readonly string[] GameTokens =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "assembly_valheim", "MonoBehaviour",
        "ZDO", "ZNet", "ZNetView", "ZRoutedRpc", "Inventory", "Container", "MessageHud", "Notices",
    };

    private static readonly string[] TimeAndIoTokens =
    {
        "DateTime", "DateTimeOffset", "Stopwatch", "TickCount", "Thread", "Task", "Timer", "Sleep", "System.IO", "Console", "lock", "CultureInfo",
    };

    private static readonly string[] AllowedUsings =
    {
        "System",
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "BaseStorage.Core.Categories",
        "BaseStorage.Core.Persistence",
        "BaseStorage.Core.Reservations",
        "BaseStorage.Core.Snapshots",
        "BaseStorage.Core.Ui",
    };

    private static readonly string[] EnglishLiterals =
    {
        "\"Auto\"", "\"Keep\"", "\"Chest category", "\"in use by", "\"another player\"", "\"no access", "\"no answer", "\"not available",
    };

    // GW-U-1230: the folder holds exactly the eight contracted files.
    [Fact]
    public void ScanReadsTheEightChestCategoriesFiles()
    {
        Assert.Equal(ExpectedFiles, Files());
    }

    // GW-U-1230: no game token appears in the raw text, comments included (ContainerId is allowed).
    [Fact]
    public void FilesHaveNoGameTokens()
    {
        var offenders = Files()
            .SelectMany(name => GameTokens
                .Where(token => Regex.IsMatch(RepoRoot.ReadText(Folder + "/" + name), @"\b" + Regex.Escape(token) + @"\b"))
                .Select(token => name + " contains " + token))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-1230: no system-time, threading, culture or I/O token in code.
    [Fact]
    public void FilesReadNoSystemTimeAndDoNoIo()
    {
        var offenders = Files()
            .SelectMany(name =>
            {
                string code = PluginSourceScan.StripComments(RepoRoot.ReadText(Folder + "/" + name));
                return TimeAndIoTokens
                    .Where(token => Regex.IsMatch(code, @"\b" + Regex.Escape(token) + @"\b"))
                    .Select(token => name + " contains " + token);
            })
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-1230: every using directive is in the allowed set, and nothing imports the Plugin.
    [Fact]
    public void FilesImportOnlyTheAllowedNamespaces()
    {
        var offenders = Files()
            .SelectMany(name => Regex.Matches(RepoRoot.ReadText(Folder + "/" + name), @"^\s*using\s+([^;]+);", RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value.Trim())
                .Where(ns => !AllowedUsings.Contains(ns, StringComparer.Ordinal))
                .Select(ns => name + " imports " + ns))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-1230: user-facing text lives only in the strings table (U6); the rules name the keys instead.
    [Fact]
    public void FilesHoldNoEnglishLiteralAndTheRulesNameTheKeys()
    {
        var offenders = Files()
            .SelectMany(name =>
            {
                string code = PluginSourceScan.StripComments(RepoRoot.ReadText(Folder + "/" + name));
                return EnglishLiterals.Where(literal => code.Contains(literal, StringComparison.Ordinal)).Select(literal => name + " contains " + literal);
            })
            .ToArray();

        Assert.Empty(offenders);
        string rules = RepoRoot.ReadText(Folder + "/CategoryWriteRules.cs");
        foreach (string key in new[] { "MessageCategorySet", "MessageCategoryRefused", "CategoryRefusalDenied", "CategoryRefusalInUse", "CategoryRefusalTimeout", "CategoryRefusalUnavailable" })
        {
            Assert.Contains("StringKeys." + key, rules, StringComparison.Ordinal);
        }

        string reader = RepoRoot.ReadText(Folder + "/ChestCategoryReader.cs");
        Assert.Contains("StringKeys.ChestCategoryAuto", reader, StringComparison.Ordinal);
        Assert.Contains("StringKeys.ChestCategoryKeep", reader, StringComparison.Ordinal);
    }

    // GW-U-1230: every static field in the folder is const or readonly (no static mutable state).
    [Fact]
    public void NoStaticMutableState()
    {
        var offenders = Files()
            .SelectMany(name => Regex.Matches(PluginSourceScan.StripComments(RepoRoot.ReadText(Folder + "/" + name)), @"\bstatic\s+(?!readonly\b|class\b)(?![^;={(]*\boperator\b)[^;={(]*(=|;)")
                .Select(match => name + ": " + match.Value.Trim()))
            .ToArray();

        Assert.Empty(offenders);
    }

    private static string[] Files() =>
        Directory.EnumerateFiles(RepoRoot.Combine(Folder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
