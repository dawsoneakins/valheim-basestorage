namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Text scans of the Plugin sources (ADR-GWIN-013). Tests never load the Plugin, because it needs the game and BepInEx.
/// Files are read CRLF-normalised, and // and /* */ comments are removed, so that a commented-out line cannot satisfy a rule.
/// Not a test class.
/// </summary>
public static class PluginSourceScan
{
    /// <summary>The Plugin project folder, relative to the repo root.</summary>
    public const string PluginFolder = "src/BaseStorage.Plugin";

    /// <summary>The entry point file, which holds the plugin class and the Harmony instance.</summary>
    public const string EntryPoint = "src/BaseStorage.Plugin/BaseStoragePlugin.cs";

    /// <summary>The patch discovery file.</summary>
    public const string DiscoveryFile = "src/BaseStorage.Plugin/Boot/PatchDiscovery.cs";

    /// <summary>The adapter that wraps one Harmony patch class as an IPatchUnit.</summary>
    public const string UnitFile = "src/BaseStorage.Plugin/Boot/HarmonyPatchUnit.cs";

    /// <summary>The adapter that wraps the BepInEx ManualLogSource as an ILogSink.</summary>
    public const string SinkFile = "src/BaseStorage.Plugin/Boot/BepInExLogSink.cs";

    /// <summary>The only Plugin file that binds settings to the BepInEx config (contract S-E001-004 section 6.1).</summary>
    public const string SettingsFile = "src/BaseStorage.Plugin/Settings/BepInExSettingsSource.cs";

    /// <summary>The three files under Boot/ that every boot scan must read.</summary>
    public static readonly string[] BootFiles = [DiscoveryFile, UnitFile, SinkFile];

    /// <summary>
    /// Every .cs file under the Plugin folder, except bin and obj, as a forward-slash relative path mapped to its
    /// comment-stripped text. Fails when the entry point or a Boot file was not read, so an empty scan cannot pass.
    /// </summary>
    public static Dictionary<string, string> Sources()
    {
        string folder = RepoRoot.Combine(PluginFolder);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/');
            if (relative.Split('/').Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }

            result[relative] = StripComments(RepoRoot.ReadText(relative));
        }

        Assert.True(result.ContainsKey(EntryPoint), $"Scan did not read {EntryPoint}.");
        foreach (string bootFile in BootFiles)
        {
            Assert.True(result.ContainsKey(bootFile), $"Scan did not read {bootFile}.");
        }

        return result;
    }

    /// <summary>Removes /* */ block comments and // line comments.</summary>
    public static string StripComments(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }

    /// <summary>Returns the text of the first member whose signature matches, between its outer braces.</summary>
    public static string MethodBody(string text, string signaturePattern)
    {
        Match match = Regex.Match(text, signaturePattern);
        Assert.True(match.Success, $"No member matches {signaturePattern}.");

        int open = text.IndexOf('{', match.Index + match.Length);
        Assert.True(open >= 0, $"No body follows {signaturePattern}.");

        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(open + 1, i - open - 1);
                }
            }
        }

        Assert.Fail($"Braces are not balanced after {signaturePattern}.");
        return string.Empty;
    }

    /// <summary>Total number of regex matches over every scanned file.</summary>
    public static int CountMatches(Dictionary<string, string> sources, string pattern) =>
        sources.Values.Sum(text => Regex.Matches(text, pattern).Count);

    /// <summary>Keys of the scanned files that match the pattern, in sorted order.</summary>
    public static string[] FilesMatching(Dictionary<string, string> sources, string pattern) =>
        sources.Where(pair => Regex.IsMatch(pair.Value, pattern))
            .Select(pair => pair.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
}
