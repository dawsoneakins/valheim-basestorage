namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-6.1 and spec section 3 (contract version-rpc-contracts.md 0.2, ADR-GWIN-099): the Peers sources hold the six contract files,
/// name no game, Plugin, logging, settings or reflection type, read time only through IClock, carry no version literal and no
/// English notice, and use the strings table only through the two network keys. Comments are stripped and CRLF normalised.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CorePeersSourceScanTests
{
    private const string PeersFolder = "src/BaseStorage.Core/Networking/Peers";

    private static readonly string[] ExpectedFiles =
    {
        "PeerVersionAction.cs",
        "PeerVersionEntry.cs",
        "PeerVersionMessages.cs",
        "PeerVersionOutcome.cs",
        "PeerVersionTable.cs",
        "VersionRpc.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine", "BepInEx", "Valheim", "HarmonyLib", "ZPackage", "ZRpc", "ZNet", "ZRoutedRpc", "MessageHud", "FejdStartup",
        "BaseStorage.Plugin", "System.Reflection", "System.IO", "File.", "Directory.", "Environment.", "Assembly.", "DateTime",
        "Stopwatch", "ILogSink", "LogPolicy", "ISettingsSource", "BaseStorageSettings", "Console.", "Regex", "CurrentCulture",
        "ToLower", "ToUpper",
    };

    private static readonly string[] ConcurrencyPatterns = { @"\block\s*\(", "Monitor", "Interlocked", "Thread", "Task", "Timer", @"\bevent\b" };

    // GW-U-1262 (0.2 P1, P2): exactly the six files; no forbidden token; no concurrency primitive; the clock only in the table.
    [Fact]
    public void PeersFilesAreTheSixNamedFilesAndUseOnlyAllowedNames()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(PeersFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        foreach (string name in names)
        {
            string code = CodeOf(name);
            Assert.False(string.IsNullOrWhiteSpace(code), name + " was read as empty.");
            foreach (string token in ForbiddenTokens)
            {
                Assert.False(code.Contains(token, StringComparison.Ordinal), name + " contains the token " + token + ".");
            }

            foreach (string pattern in ConcurrencyPatterns)
            {
                Assert.False(Regex.IsMatch(code, pattern), name + " matches the concurrency pattern " + pattern + ".");
            }

            if (name != "PeerVersionTable.cs")
            {
                Assert.DoesNotContain("IClock", code, StringComparison.Ordinal);
            }
        }

        Assert.Contains("IClock clock", CodeOf("PeerVersionTable.cs"), StringComparison.Ordinal);
        Assert.Matches(@"namespace BaseStorage\.Core\.Networking\.Peers;", CodeOf("PeerVersionTable.cs"));
    }

    // GW-U-1262 (V1, V7): no version-shaped literal, no notice wording, and StringKeys used only for the two network keys in the outcome file.
    [Fact]
    public void NoVersionLiteralAndTextComesOnlyFromTheTableByKey()
    {
        string version = RepoRoot.ValuesOf(ContractPaths.VersionProps, "Version").Single();
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.False(Regex.IsMatch(code, "\"[0-9]+\\.[0-9]+\\.[0-9]+\""), name + " contains a version-shaped literal.");
            Assert.DoesNotContain(version, code, StringComparison.Ordinal);
            Assert.False(code.Contains("version mismatch", StringComparison.OrdinalIgnoreCase), name + " holds the English notice.");
            Assert.DoesNotContain("\"network.version", code, StringComparison.Ordinal);
            Assert.DoesNotContain("joining player", code, StringComparison.Ordinal);
            Assert.False(Regex.IsMatch(code, @"\brecord\b"), name + " declares a record.");

            MatchCollection matches = Regex.Matches(code, @"StringKeys\.(\w+)");
            if (name != "PeerVersionOutcome.cs")
            {
                Assert.Empty(matches);
            }
            else
            {
                Assert.Equal(
                    new[] { "NetworkVersionMismatch", "NetworkVersionMissing" },
                    matches.Select(m => m.Groups[1].Value).Distinct().OrderBy(m => m, StringComparer.Ordinal).ToArray());
            }
        }
    }

    // GW-U-1262 (P3): the deadline and the grace are the only time constants, declared once in the table as readonly TimeSpans.
    [Fact]
    public void TheTwoTimeConstantsAreDeclaredOnceInTheTable()
    {
        string table = CodeOf("PeerVersionTable.cs");

        Assert.Single(Regex.Matches(table, @"public static readonly TimeSpan MissingPayloadTimeout = TimeSpan\.FromSeconds\(5\);"));
        Assert.Single(Regex.Matches(table, @"public static readonly TimeSpan DisconnectGrace = TimeSpan\.FromSeconds\(1\);"));
        foreach (string name in ExpectedFiles.Where(n => n != "PeerVersionTable.cs"))
        {
            Assert.DoesNotContain("TimeSpan.From", CodeOf(name), StringComparison.Ordinal);
        }
    }

    private static string CodeOf(string name)
    {
        string text = RepoRoot.ReadText(PeersFolder + "/" + name).Replace("\r\n", "\n", StringComparison.Ordinal);
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
