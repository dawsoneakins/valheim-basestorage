namespace BaseStorage.Core.Tests.Repo;

using System.Text.RegularExpressions;

/// <summary>
/// GW-U-762 (AC-1, contract 4.5 Core purity): the 4 ChestAccess files stay free of game tokens (raw text, comments included), of
/// system-time and I/O tokens (comments removed), import only the allowed namespaces, and hold no English fallback literal.
/// Each scan asserts the file names it read, so an empty folder cannot pass.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreChestAccessSourceScanTests
{
    private const string Folder = "src/BaseStorage.Core/ChestAccess";

    private static readonly string[] ExpectedFiles =
    {
        "ChestAccessRules.cs",
        "ChestInUse.cs",
        "ChestObservation.cs",
        "ReservationRoute.cs",
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
        "BaseStorage.Core.Reservations",
        "BaseStorage.Core.Snapshots",
        "BaseStorage.Core.Ui",
    };

    // GW-U-762: the folder holds exactly the four contracted files.
    [Fact]
    public void ScanReadsTheFourChestAccessFiles()
    {
        Assert.Equal(ExpectedFiles, Files());
    }

    // GW-U-762: no game token appears in the raw text, comments included (ContainerId is allowed).
    [Fact]
    public void ChestAccessFilesHaveNoGameTokens()
    {
        var offenders = Files()
            .SelectMany(name => GameTokens
                .Where(token => Regex.IsMatch(RepoRoot.ReadText(Folder + "/" + name), @"\b" + Regex.Escape(token) + @"\b"))
                .Select(token => name + " contains " + token))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-762: no system-time, threading or I/O token in code.
    [Fact]
    public void ChestAccessFilesReadNoSystemTimeAndDoNoIo()
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

    // GW-U-762: every using directive is in the allowed set.
    [Fact]
    public void ChestAccessFilesImportOnlyTheAllowedNamespaces()
    {
        var offenders = Files()
            .SelectMany(name => Regex.Matches(RepoRoot.ReadText(Folder + "/" + name), @"^\s*using\s+([^;]+);", RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value.Trim())
                .Where(ns => !AllowedUsings.Contains(ns, StringComparer.Ordinal))
                .Select(ns => name + " imports " + ns))
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-762: the fallback label lives only in the strings table (U6).
    [Fact]
    public void ChestAccessFilesHoldNoEnglishFallbackLiteral()
    {
        var offenders = Files()
            .Where(name => PluginSourceScan.StripComments(RepoRoot.ReadText(Folder + "/" + name)).Contains("\"another player\"", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(offenders);
        Assert.Contains("StringKeys.PlayerUnknown", RepoRoot.ReadText(Folder + "/ChestAccessRules.cs"), StringComparison.Ordinal);
    }

    private static string[] Files() =>
        Directory.EnumerateFiles(RepoRoot.Combine(Folder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
