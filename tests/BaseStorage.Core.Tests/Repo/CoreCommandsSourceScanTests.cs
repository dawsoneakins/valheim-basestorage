namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-2317 (AC-6; organize-console-command contract 1, purity rules): the Core command folder holds exactly its two files, and the
/// comment-stripped text names no time, environment, randomness, IO, threading, Linq, current-culture, game, Unity, BepInEx, Harmony or
/// Plugin token. Not a test class.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreCommandsSourceScanTests
{
    private const string Folder = "src/BaseStorage.Core/Commands";

    private static readonly string[] ExpectedFiles = { "ConsoleCommandRequest.cs", "ConsoleCommandText.cs" };

    private static readonly string[] BannedPatterns =
    {
        @"\bDateTime", @"\bEnvironment\b", @"\bRandom\b", @"\bStopwatch\b", @"\bSystem\.IO\b", @"\bSystem\.Threading\b", @"\bSystem\.Linq\b",
        @"\bCurrentCulture\b", @"\bUnityEngine\b", @"\bUnity\w*", @"\bBepInEx\w*", @"\bHarmony\w*", @"\bBaseStorage\.Plugin\b", @"\bPlugin\w*",
        @"\bValheim\w*", @"(?i)\bgame\b", @"\bThread\b", @"\bTask\b",
    };

    // GW-U-2317 (1): the folder holds exactly the two contracted files.
    [Fact]
    public void TheFolderHoldsExactlyTheTwoFiles()
    {
        string path = RepoRoot.Combine(Folder);
        Assert.True(Directory.Exists(path), Folder + " does not exist.");

        string[] names = Directory.EnumerateFiles(path, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedFiles, names);
    }

    // GW-U-2317 (1): no banned token appears in the comment-stripped text of a Core command file.
    [Fact]
    public void NoCommandFileNamesABannedToken()
    {
        var offenders = new List<string>();
        foreach (string name in ExpectedFiles)
        {
            string code = PluginSourceScan.StripComments(RepoRoot.ReadText(Folder + "/" + name));
            foreach (string pattern in BannedPatterns)
            {
                if (Regex.IsMatch(code, pattern))
                {
                    offenders.Add(name + ": " + pattern);
                }
            }
        }

        Assert.Empty(offenders);
    }
}
