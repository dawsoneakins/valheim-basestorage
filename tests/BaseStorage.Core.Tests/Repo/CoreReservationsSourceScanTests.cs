namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-1, AC-2 (contract 0.2 S1, ADR-GWIN-023): the reservation files stay free of game, system-time and I/O tokens, and the reservation
/// tests never wait. Game tokens are scanned on the raw text (comments included). Time and I/O tokens are scanned on the text with comments
/// removed, as in CoreBasesSourceScanTests. Each scan asserts the file names it read, so an empty folder cannot pass.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreReservationsSourceScanTests
{
    private const string ReservationsFolder = "src/BaseStorage.Core/Reservations";
    private const string TestFolder = "tests/BaseStorage.Core.Tests/Reservations";
    private const string ManualClockFile = "ManualClock.cs";
    private const string TimeoutTestsFile = "ReservationTimeoutTests.cs";

    private static readonly string[] ExpectedCoreFiles =
    {
        "ChestReservations.cs",
        "Reservation.cs",
        "ReservationEvent.cs",
        "ReservationFailure.cs",
        "ReservationState.cs",
        "ReservationTransition.cs",
        "ReservationTransitions.cs",
    };

    private static readonly string[] GameTokens =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "assembly_valheim", "MonoBehaviour",
        "ZDO", "ZNet", "ZNetView", "ZRoutedRpc", "Inventory", "Container",
    };

    private static readonly string[] TimeAndIoTokens =
    {
        "DateTime", "DateTimeOffset", "Stopwatch", "TickCount", "Thread", "Task", "Timer", "Sleep", "System.IO", "Console", "lock",
    };

    private static readonly string[] AllowedUsings =
    {
        "System",
        "System.Collections.Generic",
        "System.Collections.ObjectModel",
        "BaseStorage.Core.Abstractions",
        "BaseStorage.Core.Snapshots",
    };

    private static readonly string[] TestWaitTokens =
    {
        "Thread.Sleep", "Task.Delay", "SpinWait", "Stopwatch", "DateTime.", "Environment.TickCount",
    };

    // GW-U-159 (AC-1, R1): the folder holds exactly the seven contracted files.
    [Fact]
    public void ScanReadsTheSevenReservationFiles()
    {
        string[] names = ReservationFiles();

        Assert.Equal(ExpectedCoreFiles, names);
    }

    // GW-U-159 (AC-1, R1): no game token appears anywhere in the raw text of the seven files, comments included.
    [Fact]
    public void ReservationFilesHaveNoGameTokens()
    {
        var offenders = ReservationFiles()
            .SelectMany(name => GameTokens
                .Where(token => Regex.IsMatch(RepoRoot.ReadText(ReservationsFolder + "/" + name), @"\b" + Regex.Escape(token) + @"\b"))
                .Select(token => name + " contains " + token))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-159 (AC-1, R1): every using directive is in the allowed set, so no game or extra namespace is imported.
    [Fact]
    public void ReservationFilesImportOnlyTheAllowedNamespaces()
    {
        var offenders = ReservationFiles()
            .SelectMany(name => Regex.Matches(RepoRoot.ReadText(ReservationsFolder + "/" + name), @"^\s*using\s+([^;]+);", RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value.Trim())
                .Where(ns => !AllowedUsings.Contains(ns, StringComparer.Ordinal))
                .Select(ns => name + " imports " + ns))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-168 (AC-2, S1): the seven files read no system time, do no I/O, start no thread and never sleep (comments removed).
    [Fact]
    public void ReservationFilesReadNoSystemTimeAndDoNoIo()
    {
        string[] names = ReservationFiles();
        Assert.Equal(7, names.Length);

        var offenders = names
            .SelectMany(name =>
            {
                string code = Strip(RepoRoot.ReadText(ReservationsFolder + "/" + name));
                return TimeAndIoTokens
                    .Where(token => Regex.IsMatch(code, @"\b" + Regex.Escape(token) + @"\b"))
                    .Select(token => name + " contains " + token);
            })
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-168 (AC-2, S3): Elapsed is read only as clock.Elapsed, so no other clock source is used.
    [Fact]
    public void ElapsedIsReadOnlyAsClockElapsed()
    {
        var offenders = ReservationFiles()
            .SelectMany(name => Regex.Matches(Strip(RepoRoot.ReadText(ReservationsFolder + "/" + name)), @"(?<!clock\.)\bElapsed\b")
                .Select(_ => name + " reads Elapsed without clock."))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-169 (spec NFR): the test folder holds the ManualClock and the timeout tests, which are the files the rule must cover.
    [Fact]
    public void ScanReadsTheTestFolderIncludingTheClockAndTheTimeoutTests()
    {
        string[] names = TestFiles();

        Assert.Contains(ManualClockFile, names);
        Assert.Contains(TimeoutTestsFile, names);
    }

    // GW-U-169 (spec NFR): no reservation test file waits, sleeps, or reads the system clock, comments included.
    [Fact]
    public void NoReservationTestFileWaitsOrReadsTheSystemClock()
    {
        var offenders = TestFiles()
            .SelectMany(name => TestWaitTokens
                .Where(token => RepoRoot.ReadText(TestFolder + "/" + name).Contains(token, StringComparison.Ordinal))
                .Select(token => name + " contains " + token))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-169 (spec NFR): ManualClock is the only reservation test type that implements IClock.
    [Fact]
    public void OnlyManualClockImplementsIClock()
    {
        string[] implementers = TestFiles()
            .Where(name => Regex.IsMatch(RepoRoot.ReadText(TestFolder + "/" + name), @"\bclass\s+\w+[^{]*\bIClock\b"))
            .ToArray();

        Assert.Equal(new[] { ManualClockFile }, implementers);
    }

    /// <summary>The file names directly under the Core Reservations folder, ordinal sorted.</summary>
    private static string[] ReservationFiles() => FilesIn(ReservationsFolder);

    /// <summary>The file names directly under the reservation test folder, ordinal sorted.</summary>
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
