namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-1379 (contract organize-runner-contracts.md section 1, ADR-GWIN-108): the Run sources are the nine contracted files, they
/// name no game, Plugin, time, culture, Linq, IO or threading token, every static field is readonly or const, and no file holds a
/// strings-table text or the storage key. The scans strip comments and normalise CRLF.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreOrganizeRunSourceScanTests
{
    private const string Folder = "src/BaseStorage.Core/Organize/Run";

    private static readonly string[] ExpectedFiles =
    {
        "HotkeyChord.cs",
        "IOrganizeRunPort.cs",
        "OrganizeBaseFacts.cs",
        "OrganizeLayerRules.cs",
        "OrganizeNoticePlacement.cs",
        "OrganizeRunCoordinator.cs",
        "OrganizeRunState.cs",
        "OrganizeRunText.cs",
        "OrganizeStartOutcome.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "Valheim", "ZDO", "ZNet", "KeyCode", "ZInput", "MessageHud", "UnifiedPopup",
        "BaseStorage.Plugin", "Stopwatch", "DateTime", "Random", "Environment.", "CultureInfo", "ToLower", "ToUpper",
        "using System.Linq", "System.IO", "System.Threading", "Thread", "Task", "Timer", "lock (",
    };

    private static readonly Regex MutableStaticField = new Regex(
        @"\bstatic\s+(?!readonly\s|const\s|class\s|bool\s|string\s+StopReasonKey|HotkeyChord\s+Parse|ReservationStatus\s|int\s+IndexOfSource|\w+Status\s+FailedStatus)(?![^\n]*\boperator\b)[^(){}=;]*(?:;|=(?!>))",
        RegexOptions.None);

    // GW-U-1379
    [Fact]
    public void TheFolderHoldsExactlyTheNineFilesAndEachStartsWithTheNamespace()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(Folder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        foreach (string name in names)
        {
            Assert.StartsWith("namespace BaseStorage.Core.Organize.Run;", RepoRoot.ReadText(Folder + "/" + name).TrimStart(), StringComparison.Ordinal);
        }
    }

    // GW-U-1379
    [Fact]
    public void NoRunFileUsesAForbiddenTokenOrAnEnglishText()
    {
        var offenders = new List<string>();
        foreach (string name in ExpectedFiles)
        {
            string code = Strip(RepoRoot.ReadText(Folder + "/" + name));
            foreach (string token in ForbiddenTokens)
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    offenders.Add(name + ": " + token);
                }
            }

            foreach (Match literal in Regex.Matches(code, "\"(?:[^\"\\\\]|\\\\.)*\""))
            {
                string inner = literal.Value.Substring(1, literal.Value.Length - 2);
                foreach ((string _, string english) in Ui.UiFixtures.EnglishTable)
                {
                    if (inner == english || (english.Length >= 8 && inner.Contains(english, StringComparison.Ordinal)))
                    {
                        offenders.Add(name + ": " + literal.Value);
                    }
                }
            }

            if (code.Contains("basestorage.lock", StringComparison.Ordinal))
            {
                offenders.Add(name + ": storage key literal");
            }
        }

        Assert.Empty(offenders);
    }

    // GW-U-1379: time and settings only through the injected clock and settings; the executor is stepped exactly once per tick.
    [Fact]
    public void TheCoordinatorStepsOnceAndResolvesOncePerStart()
    {
        string code = Strip(RepoRoot.ReadText(Folder + "/OrganizeRunCoordinator.cs"));

        Assert.Single(Regex.Matches(code, @"\.Step\(\)"));
        Assert.Single(Regex.Matches(code, @"cache\.Resolve\("));
        Assert.Single(Regex.Matches(code, @"OrganizePlanner\.Plan\("));
        Assert.Single(Regex.Matches(code, @"port\.GatherFacts\("));
        Assert.Single(Regex.Matches(code, @"port\.CreateLayer\("));
        Assert.Single(Regex.Matches(code, @"LogPolicy\.Write\("));
        Assert.Contains("LogCategory.OrganizeSummary", code, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(code, @"OrganizeSummaryFormatter\.Summary\("));
        Assert.Single(Regex.Matches(code, @"new OrganizeExecutor\("));
        Assert.Single(Regex.Matches(code, @"new OrganizeLock\("));
        Assert.DoesNotMatch(@"\b30\b", code);
    }

    // GW-U-1379: every static field is readonly or const.
    [Fact]
    public void EveryStaticFieldIsReadOnlyOrConst()
    {
        var offenders = new List<string>();
        foreach (string name in ExpectedFiles)
        {
            string code = Strip(RepoRoot.ReadText(Folder + "/" + name));
            foreach (Match match in Regex.Matches(code, @"(?m)^\s*(?:private|internal|public)\s+static\s+(?!readonly\b|class\b)(?<type>[\w<>\[\]?.]+)\s+(?<name>\w+)\s*(=|;)"))
            {
                if (match.Groups["type"].Value != "const")
                {
                    offenders.Add(name + ": " + match.Value.Trim());
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static string Strip(string text)
    {
        string normalised = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        string withoutBlocks = Regex.Replace(normalised, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
