namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-6.1, spec section 3 (ADR-GWIN-061, ADR-GWIN-064): the Networking sources hold the five contract files, name no game or Plugin
/// type, read nothing at runtime (no file, assembly attribute, environment or clock), carry no version literal and no English notice,
/// and use the strings table only through StringKeys.NetworkVersionMismatch. The scans strip comments and normalise CRLF.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreNetworkingSourceScanTests
{
    private const string NetworkingFolder = "src/BaseStorage.Core/Networking";

    private static readonly string[] ExpectedFiles =
    {
        "ModVersion.cs",
        "VersionCheck.cs",
        "VersionCheckResult.cs",
        "VersionCheckStatus.cs",
        "VersionHandshake.cs",
    };

    private static readonly string[] GameTokens =
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
    };

    private static readonly string[] RuntimeReadTokens =
    {
        "AssemblyInformationalVersion",
        "GetCustomAttribute",
        "System.Reflection",
        "System.IO",
        "File.",
        "Directory.",
        "Environment.",
        "Assembly.",
        "DateTime",
        "Stopwatch",
        "IClock",
        "ILogSink",
        "LogPolicy",
        "ISettingsSource",
        "BaseStorageSettings",
        "Console.",
        "Regex",
        "CultureInfo",
        "ToLower",
        "ToUpper",
        "CurrentCulture",
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

    // GW-U-814 (R1, V6): the folder holds exactly the five contract files; none names a game type, a runtime read, or a concurrency primitive.
    [Fact]
    public void NetworkingFilesAreTheFiveNamedFilesAndUseOnlyAllowedNames()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(NetworkingFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        foreach (string name in names)
        {
            string code = CodeOf(name);
            Assert.False(string.IsNullOrWhiteSpace(code), name + " was read as empty.");

            foreach (string token in GameTokens.Concat(RuntimeReadTokens))
            {
                Assert.False(code.Contains(token, StringComparison.Ordinal), name + " contains the token " + token + ".");
            }

            foreach (string pattern in ConcurrencyPatterns)
            {
                Assert.False(Regex.IsMatch(code, pattern), name + " matches the concurrency pattern " + pattern + ".");
            }
        }
    }

    // GW-U-814 (V1): no Networking file holds a version-shaped literal; the build constant is referenced only from ModVersion.cs.
    [Fact]
    public void VersionComesOnlyFromTheBuildConstant()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.False(Regex.IsMatch(code, "\"[0-9]+\\.[0-9]+\\.[0-9]+\""), name + " contains a version-shaped literal.");

            if (name != "ModVersion.cs")
            {
                Assert.DoesNotContain("CoreBuildInfo", code, StringComparison.Ordinal);
            }
        }

        Assert.Contains("CoreBuildInfo.Version", CodeOf("ModVersion.cs"), StringComparison.Ordinal);
        string version = RepoRoot.ValuesOf(ContractPaths.VersionProps, "Version").Single();
        Assert.All(ExpectedFiles, name => Assert.DoesNotContain(version, CodeOf(name), StringComparison.Ordinal));
    }

    // GW-U-814 (V7): no English notice literal; the only StringKeys member used is NetworkVersionMismatch, and only from VersionCheckResult.cs.
    [Fact]
    public void TextComesOnlyFromTheStringsTableByKey()
    {
        var stringKeyMembers = new List<string>();
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.False(code.Contains("version mismatch", StringComparison.OrdinalIgnoreCase), name + " holds the English notice.");
            Assert.DoesNotContain("\"network.version", code, StringComparison.Ordinal);
            Assert.DoesNotContain("you ", code, StringComparison.Ordinal);

            MatchCollection matches = Regex.Matches(code, @"StringKeys\.(\w+)");
            if (name != "VersionCheckResult.cs")
            {
                Assert.Empty(matches);
            }

            stringKeyMembers.AddRange(matches.Select(match => match.Groups[1].Value));
        }

        Assert.NotEmpty(stringKeyMembers);
        Assert.All(stringKeyMembers, member => Assert.Equal("NetworkVersionMismatch", member));
    }

    // GW-U-814 (R3, R4): no record types, and every static field is const or readonly.
    [Fact]
    public void NoRecordsAndEveryStaticFieldIsConstOrReadonly()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.False(Regex.IsMatch(code, @"\brecord\b"), name + " declares a record.");

            // A static field declaration: "static <type> <name> =" or "static <type> <name>;" that is neither readonly nor const.
            // Expression-bodied members ("=>"), operators ("==") and methods ("(") do not match.
            MatchCollection mutableStatics = Regex.Matches(
                code,
                @"^\s*(?:public|internal|private|protected)?\s*static\s+(?!class\b)(?!readonly\b)(?!const\b)[\w\?\[\]<>,\s]+?\s+\w+\s*(?:=(?![>=])|;)",
                RegexOptions.Multiline);
            Assert.True(mutableStatics.Count == 0, name + " has a mutable static field: " + string.Join(" | ", mutableStatics.Select(m => m.Value.Trim())));
        }
    }

    /// <summary>The code of one Networking file, with comments removed and CRLF normalised, so only code is matched.</summary>
    private static string CodeOf(string name)
    {
        string text = RepoRoot.ReadText(NetworkingFolder + "/" + name).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Strip(text);
    }

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
