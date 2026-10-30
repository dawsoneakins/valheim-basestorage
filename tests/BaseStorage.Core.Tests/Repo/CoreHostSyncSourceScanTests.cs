namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// AC-6.2, rules H1 and H7 (ADR-GWIN-081, ADR-GWIN-084): the HostSync sources hold the four contract files, name no game, RPC or
/// Plugin type, read nothing at runtime (no file, environment, clock, logging), name no setting key (the catalog is the only source
/// of what is synced), and use no concurrency primitive, record or mutable static. The scans strip comments and normalise CRLF.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreHostSyncSourceScanTests
{
    private const string HostSyncFolder = "src/BaseStorage.Core/HostSync";

    private static readonly string[] ExpectedFiles =
    {
        "HostSyncCodec.cs",
        "HostSyncPayload.cs",
        "HostSyncValueText.cs",
        "LayeredSettingsSource.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine",
        "BepInEx",
        "Valheim",
        "HarmonyLib",
        "ZPackage",
        "ZRpc",
        "ZNet",
        "ZRoutedRpc",
        "MessageHud",
        "BaseStorage.Plugin",
        "ConfigEntry",
        "ConfigFile",
        "System.IO",
        "File.",
        "Directory.",
        "Environment.",
        "Console.",
        "DateTime",
        "Stopwatch",
        "IClock",
        "ILogSink",
        "LogPolicy",
        "LogCategory",
        "StringKeys",
        "StringTable",
        "SettingKeys.",
        "CategoryFile.",
        "CurrentCulture",
        "ToLower(",
        "ToUpper(",
    };

    private static readonly string[] ConcurrencyPatterns =
    {
        @"\block\s*\(",
        @"\bMonitor\b",
        @"\bInterlocked\b",
        @"\bThread\b",
        @"\bTask\b",
        @"\bTimer\b",
        @"\bevent\b",
    };

    // GW-U-1012 (R1, H7): the folder holds exactly the four contract files; none names a game type, a runtime read, logging, or a concurrency primitive.
    [Fact]
    public void HostSyncFilesAreTheFourNamedFilesAndUseOnlyAllowedNames()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(HostSyncFolder), "*.cs", SearchOption.TopDirectoryOnly)
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
        }
    }

    // GW-U-1012 (H1): no HostSync file names a setting key literal or a SettingKeys constant; the synced set comes from SettingsCatalog.HostSynced.
    [Fact]
    public void SyncedKeysComeOnlyFromTheCatalog()
    {
        bool catalogReferenced = false;
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            foreach (var row in Section7Table.Rows)
            {
                Assert.False(code.Contains("\"" + row.Key + "\"", StringComparison.Ordinal), name + " holds the key literal " + row.Key + ".");
                Assert.False(Regex.IsMatch(code, @"SettingKeys\." + row.Key + @"\b"), name + " names SettingKeys." + row.Key + ".");
            }

            catalogReferenced |= code.Contains("SettingsCatalog.HostSynced", StringComparison.Ordinal);
        }

        Assert.True(catalogReferenced, "No HostSync file reads SettingsCatalog.HostSynced.");
    }

    // GW-U-1012 (R3, R4): no record types, and every static field is const or readonly.
    [Fact]
    public void NoRecordsAndEveryStaticFieldIsConstOrReadonly()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.False(Regex.IsMatch(code, @"\brecord\b"), name + " declares a record.");

            MatchCollection mutableStatics = Regex.Matches(
                code,
                @"^\s*(?:public|internal|private|protected)?\s*static\s+(?!class\b)(?!readonly\b)(?!const\b)[\w\?\[\]<>,\s]+?\s+\w+\s*(?:=(?![>=])|;)",
                RegexOptions.Multiline);
            Assert.True(mutableStatics.Count == 0, name + " has a mutable static field: " + string.Join(" | ", mutableStatics.Select(m => m.Value.Trim())));
        }
    }

    // GW-U-1012 (H7): the three pure types declare no instance state beyond the payload's readonly fields; the layered source has no public field.
    [Fact]
    public void PureTypesHaveNoMutableInstanceField()
    {
        foreach (string name in new[] { "HostSyncCodec.cs", "HostSyncValueText.cs", "HostSyncPayload.cs" })
        {
            string code = CodeOf(name);
            MatchCollection instanceFields = Regex.Matches(
                code,
                @"^\s*(?:private|internal|public|protected)\s+(?!static\b)(?!readonly\b)(?!const\b)(?!sealed\b)(?!class\b)(?!override\b)(?!abstract\b)[\w\?\[\]<>,\s]+?\s+\w+\s*(?:=(?![>=])|;)",
                RegexOptions.Multiline);
            Assert.True(instanceFields.Count == 0, name + " has a mutable instance field: " + string.Join(" | ", instanceFields.Select(m => m.Value.Trim())));
        }
    }

    /// <summary>The code of one HostSync file, with comments removed and CRLF normalised, so only code is matched.</summary>
    private static string CodeOf(string name)
    {
        string text = RepoRoot.ReadText(HostSyncFolder + "/" + name).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Strip(text);
    }

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
