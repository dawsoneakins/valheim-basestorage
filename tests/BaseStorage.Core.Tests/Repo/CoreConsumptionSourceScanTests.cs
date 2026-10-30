namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// AC-1 and AC-4 (contract 0.2 CX1, CX2, CX3, 4.3 source-scan rows): the consumption files stay free of time, thread and game tokens, use only
/// the contract usings, and never name the message text. Each scan asserts the files it read, so an empty folder cannot pass.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreConsumptionSourceScanTests
{
    private const string ConsumptionFolder = "src/BaseStorage.Core/Consumption";
    private const string TestFolder = "tests/BaseStorage.Core.Tests/Consumption";
    private const string AvailabilityFolder = "src/BaseStorage.Core/Availability";

    private static readonly string[] ExpectedCoreFiles =
    {
        "ConsumptionExecution.cs",
        "ConsumptionExecutor.cs",
        "ConsumptionOutcome.cs",
        "ConsumptionResult.cs",
    };

    private static readonly string[] TimeAndThreadTokens =
    {
        "Elapsed", "DateTime", "DateTimeOffset", "Stopwatch", "TickCount", "Thread", "Task", "Timer", "Sleep", "lock",
    };

    private static readonly string[] GameTokens = { "UnityEngine", "BepInEx", "HarmonyLib", "Valheim", "ZDO", "ZNet", "MessageHud" };

    private static readonly string[] AllowedUsings =
    {
        "System",
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "BaseStorage.Core.Abstractions",
        "BaseStorage.Core.Snapshots",
        "BaseStorage.Core.Availability",
        "BaseStorage.Core.Reservations",
        "BaseStorage.Core.Settings",
        "BaseStorage.Core.Ui",
    };

    private static readonly string[] TestWaitTokens = { "Thread.Sleep", "Task.Delay", "SpinWait", "Stopwatch", "DateTime.", "Environment.TickCount" };

    // GW-U-509 (AC-1, CX2): no consumption file reads the clock, a date, a timer, a thread or a lock (comments removed).
    [Fact]
    public void ConsumptionFilesReadNoClockAndStartNoThread()
    {
        string[] names = ConsumptionFiles();
        Assert.Equal(ExpectedCoreFiles, names);

        string[] offenders = names
            .SelectMany(name => TimeAndThreadTokens
                .Where(token => Regex.IsMatch(Strip(RepoRoot.ReadText(ConsumptionFolder + "/" + name)), @"\b" + Regex.Escape(token) + @"\b"))
                .Select(token => name + " contains " + token))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-509 (AC-1, CX3): ReserveTimeoutMs occurs at most once in the code, and only in ConsumptionExecutor.cs.
    [Fact]
    public void ReserveTimeoutMsOccursAtMostOnceAndOnlyInTheExecutor()
    {
        string[] hits = ConsumptionFiles()
            .SelectMany(name => Regex.Matches(Strip(RepoRoot.ReadText(ConsumptionFolder + "/" + name)), @"\bReserveTimeoutMs\b")
                .Select(_ => name))
            .ToArray();

        Assert.True(hits.Length <= 1, "ReserveTimeoutMs occurs more than once: " + string.Join(",", hits));
        Assert.All(hits, name => Assert.Equal("ConsumptionExecutor.cs", name));
    }

    // GW-U-509 (AC-1, CX3): the numeric literal of the default timeout appears in no consumption file's code.
    [Fact]
    public void NoConsumptionFileContainsTheDefaultTimeoutLiteral()
    {
        string[] offenders = ConsumptionFiles()
            .Where(name => Regex.IsMatch(Strip(RepoRoot.ReadText(ConsumptionFolder + "/" + name)), @"\b1500\b"))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-509 (AC-1, spec NFR): no consumption test file waits, sleeps, or reads the system clock, comments included.
    [Fact]
    public void NoConsumptionTestFileWaitsOrReadsTheSystemClock()
    {
        string[] offenders = TestFiles()
            .SelectMany(name => TestWaitTokens
                .Where(token => RepoRoot.ReadText(TestFolder + "/" + name).Contains(token, StringComparison.Ordinal))
                .Select(token => name + " contains " + token))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-539 (AC-4, CX1): the folder holds exactly the four contracted files, and the scan read them.
    [Fact]
    public void ScanReadsTheFourConsumptionFiles()
    {
        Assert.Equal(ExpectedCoreFiles, ConsumptionFiles());
    }

    // GW-U-539 (AC-4, CX1): no game or engine token appears in the raw text of the four files, comments included.
    [Fact]
    public void ConsumptionFilesHaveNoGameTokenInTheirRawText()
    {
        string[] offenders = ConsumptionFiles()
            .SelectMany(name => GameTokens
                .Where(token => Regex.IsMatch(RepoRoot.ReadText(ConsumptionFolder + "/" + name), @"\b" + Regex.Escape(token) + @"\b"))
                .Select(token => name + " contains " + token))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-539 (AC-4, CX1): every using directive is in the contract 1 allowed set, so no System.Linq and no game namespace is imported.
    [Fact]
    public void ConsumptionFilesImportOnlyTheAllowedNamespaces()
    {
        string[] offenders = ConsumptionFiles()
            .SelectMany(name => Regex.Matches(RepoRoot.ReadText(ConsumptionFolder + "/" + name), @"^\s*using\s+([^;]+);", RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value.Trim())
                .Where(ns => !AllowedUsings.Contains(ns, StringComparer.Ordinal))
                .Select(ns => name + " imports " + ns))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-539 (AC-4, CX11): the materials-moved key and its English text are never written as literals in the consumption files.
    [Fact]
    public void NoMaterialsMovedLiteralAppearsInTheConsumptionFiles()
    {
        string[] offenders = ConsumptionFiles()
            .SelectMany(name => new[] { "message.craft.materials-moved", "Materials moved" }
                .Where(literal => RepoRoot.ReadText(ConsumptionFolder + "/" + name).Contains(literal, StringComparison.Ordinal))
                .Select(literal => name + " contains " + literal))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-539 (AC-4, CX11): the only StringKeys member the consumption files use is MessageCraftMaterialsMoved.
    [Fact]
    public void TheOnlyStringKeysMemberUsedIsMessageCraftMaterialsMoved()
    {
        string[] members = ConsumptionFiles()
            .SelectMany(name => Regex.Matches(RepoRoot.ReadText(ConsumptionFolder + "/" + name), @"\bStringKeys\.(\w+)")
                .Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.All(members, member => Assert.Equal("MessageCraftMaterialsMoved", member));
    }

    // GW-U-539 (AC-4, spec AC-4): the Availability folder still holds its nine pure files, so the existing GW-U-435 pin stays green.
    [Fact]
    public void AvailabilityStillHoldsItsNineFiles()
    {
        int count = Directory.EnumerateFiles(RepoRoot.Combine(AvailabilityFolder), "*.cs", SearchOption.TopDirectoryOnly).Count();

        Assert.Equal(9, count);
    }

    /// <summary>The file names directly under the consumption folder, ordinal sorted.</summary>
    private static string[] ConsumptionFiles() => FilesIn(ConsumptionFolder);

    /// <summary>The file names directly under the consumption test folder, ordinal sorted.</summary>
    private static string[] TestFiles() => FilesIn(TestFolder);

    private static string[] FilesIn(string folder) =>
        Directory.EnumerateFiles(RepoRoot.Combine(folder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
