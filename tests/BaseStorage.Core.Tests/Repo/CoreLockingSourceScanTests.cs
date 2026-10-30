namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-2, AC-8 (ADR-GWIN-027): the Locking sources hold the four ADR-GWIN-026 files, name no game or Plugin type, use no concurrency primitive,
/// read time only through IClock and settings, and carry no key or English literal. The scans strip comments and normalise CRLF, and they read
/// the four files by name. They are text checks; the compiled checks are in Locking/LockingDependencyTests.cs.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreLockingSourceScanTests
{
    private const string LockingFolder = "src/BaseStorage.Core/Locking";

    private static readonly string[] ExpectedFiles =
    {
        "IOrganizeLockStore.cs",
        "OrganizeLock.cs",
        "OrganizeLockResult.cs",
        "OrganizeLockStatus.cs",
    };

    private static readonly string[] GameTokens =
    {
        "UnityEngine",
        "BepInEx",
        "Valheim",
        "HarmonyLib",
        "ZDO",
        "ZNet",
        "ZRoutedRpc",
        "MessageHud",
        "BaseStorage.Plugin",
    };

    private static readonly string[] ConcurrencyPatterns =
    {
        @"\block\s*\(",
        "Monitor",
        "Interlocked",
        "Thread",
        "Task",
        "Timer",
        @"\bevent\b",
    };

    private static readonly string[] TimeTokens = { "DateTime", "Stopwatch", "Environment.TickCount", "Time." };

    // GW-U-286 (AC-8): the folder holds exactly the four ADR-GWIN-026 files; none names a game type, a concurrency primitive, the storage key or the English notice.
    [Fact]
    public void LockingFilesAreTheFourNamedFilesAndUseOnlyAllowedNames()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(LockingFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        var stringKeyMembers = new List<string>();
        foreach (string name in names)
        {
            string code = CodeOf(name);
            Assert.False(string.IsNullOrWhiteSpace(code), name + " was read as empty.");

            foreach (string token in GameTokens)
            {
                Assert.DoesNotContain(token, code, StringComparison.Ordinal);
            }

            foreach (string pattern in ConcurrencyPatterns)
            {
                Assert.False(Regex.IsMatch(code, pattern), name + " matches the concurrency pattern " + pattern + ".");
            }

            Assert.DoesNotContain("\"basestorage.lock\"", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Organize in progress", code, StringComparison.Ordinal);

            stringKeyMembers.AddRange(Regex.Matches(code, @"StringKeys\.(\w+)").Select(match => match.Groups[1].Value));
        }

        Assert.All(stringKeyMembers, member => Assert.Equal("MessageBaseInProgress", member));
    }

    // GW-U-286 (AC-2): no Locking file reads a wall clock, a stopwatch or an environment tick; IClock and OrganizeLockSeconds appear only in OrganizeLock.cs;
    // and no file contains the literal 30, so the duration is never hard-coded.
    [Fact]
    public void TimeAndDurationComeOnlyFromIClockAndSettings()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            foreach (string token in TimeTokens)
            {
                Assert.DoesNotContain(token, code, StringComparison.Ordinal);
            }

            Assert.False(Regex.IsMatch(code, @"\b30\b"), name + " contains the literal 30.");

            if (name != "OrganizeLock.cs")
            {
                Assert.DoesNotContain("IClock", code, StringComparison.Ordinal);
                Assert.DoesNotContain("OrganizeLockSeconds", code, StringComparison.Ordinal);
            }
        }

        Assert.Contains("IClock", CodeOf("OrganizeLock.cs"), StringComparison.Ordinal);
    }

    /// <summary>The code of one Locking file, with comments removed and CRLF normalised, so only code is matched.</summary>
    private static string CodeOf(string name)
    {
        string text = RepoRoot.ReadText(LockingFolder + "/" + name).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Strip(text);
    }

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
