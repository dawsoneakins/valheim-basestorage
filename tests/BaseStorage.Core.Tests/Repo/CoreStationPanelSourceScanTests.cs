namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-4 (ADR-GWIN-041, SP1, SP2, SP7): the StationPanel sources hold the seven ADR-GWIN-040 files, name no game or Core-internal namespace, use no
/// clock, culture or randomness, keep their text in the table, and hold no mutable static state. The scans strip comments and normalise CRLF.
/// The compiled checks are in StationPanel/StationPanelDependencyTests.cs.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreStationPanelSourceScanTests
{
    private const string StationPanelFolder = "src/BaseStorage.Core/StationPanel";

    private static readonly string[] ExpectedFiles =
    {
        "PanelRect.cs",
        "StationPanelDisabledReason.cs",
        "StationPanelHiddenReason.cs",
        "StationPanelInput.cs",
        "StationPanelLayout.cs",
        "StationPanelPlacement.cs",
        "StationPanelState.cs",
    };

    private static readonly string[] GameTokens =
    {
        "UnityEngine",
        "BepInEx",
        "Valheim",
        "HarmonyLib",
        "BaseStorage.Plugin",
    };

    private static readonly string[] ForbiddenFragments =
    {
        "Core.Settings",
        "Core.Locking",
        "Core.Bases",
        "Core.Abstractions",
        "Core.Snapshots",
        "CultureInfo",
        "DateTime",
        "Random",
        "Console.",
        "Thread",
        "Task",
        "MathF",
    };

    private static readonly string[] ForbiddenPatterns =
    {
        @"\block\s*\(",
        @"\brecord\b",
    };

    private static readonly string[] AllowedStringKeyMembers =
    {
        "StationActionsStatusLine",
        "StationActionsStatusLineFill",
        "StationActionsDisabledInProgress",
        "StationActionsDisabledNoChests",
    };

    // GW-U-550 (AC-4): the folder holds exactly the seven ADR-GWIN-040 files, and the test asserts that it read them.
    [Fact]
    public void StationPanelFolderHoldsExactlyTheSevenNamedFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(StationPanelFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        foreach (string name in names)
        {
            Assert.False(string.IsNullOrWhiteSpace(CodeOf(name)), name + " was read as empty.");
        }
    }

    // GW-U-550 (AC-4, SP2): no game token, and no forbidden namespace, clock, culture, randomness, thread, console or record in any file.
    [Fact]
    public void NoGameTokensOrForbiddenNamespacesOrCalls()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            foreach (string token in GameTokens)
            {
                Assert.DoesNotContain(token, code, StringComparison.Ordinal);
            }

            foreach (string fragment in ForbiddenFragments)
            {
                Assert.DoesNotContain(fragment, code, StringComparison.Ordinal);
            }

            foreach (string pattern in ForbiddenPatterns)
            {
                Assert.False(Regex.IsMatch(code, pattern), name + " matches the forbidden pattern " + pattern + ".");
            }
        }
    }

    // GW-U-550 (AC-4, SP7, U6): no station.actions key literal, and none of the English fragments of the table.
    [Fact]
    public void NoKeyLiteralsOrEnglishFragments()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.DoesNotContain("\"station.actions", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Organize in progress", code, StringComparison.Ordinal);
            Assert.DoesNotContain("No chests", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Base:", code, StringComparison.Ordinal);
        }
    }

    // GW-U-550 (AC-4, SP7): every StringKeys member used is one of the four contracted keys, so no other key sneaks in.
    [Fact]
    public void StringKeysMembersUsedAreOnlyTheContractedFour()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in ExpectedFiles)
        {
            foreach (Match match in Regex.Matches(CodeOf(name), @"StringKeys\.(\w+)"))
            {
                used.Add(match.Groups[1].Value);
            }
        }

        Assert.All(used, member => Assert.Contains(member, AllowedStringKeyMembers));
    }

    // GW-U-550 (AC-4, SP1): every static field declaration is const, so there is no mutable static state.
    [Fact]
    public void EveryStaticFieldIsConst()
    {
        foreach (string name in ExpectedFiles)
        {
            foreach (string line in CodeOf(name).Split('\n'))
            {
                bool isStaticDeclaration = Regex.IsMatch(line, @"\bstatic\b");
                bool isMemberSignature = line.Contains("(", StringComparison.Ordinal);
                bool isTypeDeclaration = Regex.IsMatch(line, @"\b(class|struct|enum|interface)\b");
                if (!isStaticDeclaration || isMemberSignature || isTypeDeclaration || line.Contains("const ", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.Fail(name + " declares a non-const static field: " + line.Trim());
            }
        }
    }

    /// <summary>The code of one StationPanel file, with comments removed and CRLF normalised, so only code is matched.</summary>
    private static string CodeOf(string name)
    {
        string text = RepoRoot.ReadText(StationPanelFolder + "/" + name).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Strip(text);
    }

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
