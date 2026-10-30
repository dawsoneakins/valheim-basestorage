namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-8 (rules R1-R4, S4; ADR-GWIN-121): the HostSync.Session folder holds exactly the eight contract files. Each has the file-scoped
/// namespace and one top-level type, and names no game, RPC, file, clock, thread, reflection or culture token. The scans strip comments
/// and normalise CRLF.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreConfigSyncSourceScanTests
{
    private const string SessionFolder = "src/BaseStorage.Core/HostSync/Session";

    private static readonly string[] ExpectedFiles =
    {
        "ConfigSyncAction.cs",
        "ConfigSyncMessages.cs",
        "ConfigSyncMode.cs",
        "ConfigSyncOutcome.cs",
        "ConfigSyncRejectReason.cs",
        "ConfigSyncRpc.cs",
        "ConfigSyncSession.cs",
        "ConfigSyncTick.cs",
    };

    private static readonly string[] ForbiddenPatterns =
    {
        @"\bUnityEngine\b",
        @"\bBepInEx\b",
        @"\bValheim\b",
        @"\bHarmonyLib\b",
        @"\bZPackage\b",
        @"\bZRpc\b",
        @"\bZNet\b",
        @"\bZRoutedRpc\b",
        @"BaseStorage\.Plugin\b",
        @"\bSystem\.IO\b",
        @"\bSystem\.Reflection\b",
        @"\bFile\.",
        @"\bDirectory\.",
        @"\bEnvironment\.",
        @"\bConsole\.",
        @"\bDateTime\b",
        @"\bStopwatch\b",
        @"\bIClock\b",
        @"\bThread\b",
        @"\bTask\b",
        @"\bTimer\b",
        @"\block\s*\(",
        @"\bMonitor\b",
        @"\bInterlocked\b",
        @"\bCurrentCulture\b",
        @"\bLogSeverity\.Info\b",
    };

    // GW-U-1402: the folder holds exactly the eight contract files, and each file reads as non-empty code.
    [Fact]
    public void SessionFolderHoldsExactlyTheEightContractFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(SessionFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedFiles, names);
    }

    // GW-U-1402: each file starts with the file-scoped namespace, declares one top-level type, names no forbidden token, and has no mutable static.
    [Fact]
    public void EachFileIsPureCoreCodeWithOneTypeAndNoMutableStatic()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.False(string.IsNullOrWhiteSpace(code), name + " was read as empty.");
            Assert.True(Regex.IsMatch(code, @"\A\s*namespace BaseStorage\.Core\.HostSync\.Session;"), name + " does not start with the file-scoped Session namespace.");

            Assert.Single(Regex.Matches(code, @"(?m)^(?:public|internal)\s+(?:(?:sealed|static|abstract|partial|readonly)\s+)*(?:class|enum|struct|interface)\s+\w+"));

            foreach (string pattern in ForbiddenPatterns)
            {
                Assert.False(Regex.IsMatch(code, pattern), name + " matches the forbidden pattern " + pattern + ".");
            }

            Assert.False(Regex.IsMatch(code, @"\bbrecord\b|\brecord\s+\w"), name + " declares a record.");

            MatchCollection mutableStatics = Regex.Matches(
                code,
                @"^\s*(?:public|internal|private|protected)?\s*static\s+(?!class\b)(?!readonly\b)(?!const\b)(?!sealed\b)(?!partial\b)[\w\?\[\]<>,\s]+?\s+\w+\s*(?:=(?![>=])|;)",
                RegexOptions.Multiline);
            Assert.True(mutableStatics.Count == 0, name + " has a mutable static field: " + string.Join(" | ", mutableStatics.Select(m => m.Value.Trim())));

            if (Regex.IsMatch(code, @"\.ToString\(\)|string\.Format\(|\$"""))
            {
                Assert.True(code.Contains("InvariantCulture", StringComparison.Ordinal), name + " formats text without CultureInfo.InvariantCulture.");
            }
        }
    }

    /// <summary>The code of one Session file, with comments removed and CRLF normalised, so only code is matched.</summary>
    private static string CodeOf(string name)
    {
        string text = RepoRoot.ReadText(SessionFolder + "/" + name).Replace("\r\n", "\n", StringComparison.Ordinal);
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
