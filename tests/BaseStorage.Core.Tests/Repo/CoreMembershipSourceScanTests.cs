namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-1, AC-12 (contract M1, M7, M8, M9; ADR-GWIN-022, ADR-GWIN-023): the Membership sources are pure, game-free and settings-free, and
/// membership uses the one build-range seam. The scans strip comments, normalise CRLF, and assert the file names they read. They are
/// text checks, not compiler checks.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreMembershipSourceScanTests
{
    private const string MembershipFolder = "src/BaseStorage.Core/Bases/Membership";
    private const string BaseMembershipFile = MembershipFolder + "/BaseMembership.cs";
    private const string EligibilityOptionsFile = MembershipFolder + "/EligibilityOptions.cs";

    private static readonly string[] ExpectedFiles =
    {
        "BaseMembership.cs",
        "ContainerEligibility.cs",
        "ContainerExclusion.cs",
        "ContainerPurpose.cs",
        "EligibilityOptions.cs",
        "EligibilityResult.cs",
        "ExclusionReason.cs",
        "MembershipInput.cs",
    };

    private static readonly string[] ForbiddenTokens =
    {
        "DateTime",
        "Stopwatch",
        "Environment.",
        "ILogSink",
        "Random",
        "ISettingsSource",
        "InUseByOther",
        "InternalsVisibleTo",
        "UnityEngine",
        "BepInEx",
        "Valheim",
    };

    // GW-U-226 (AC-1, M7): BaseMembership calls the one build-range seam and does no distance arithmetic of its own.
    [Fact]
    public void BaseMembershipCallsTheSeamAndDoesNoDistanceArithmetic()
    {
        string code = Strip(RepoRoot.ReadText(BaseMembershipFile));

        Assert.Contains("BuildRangeRule.IsInRange(", code, StringComparison.Ordinal);
        foreach (string token in new[] { "Math.", ".X", ".Z", "Distance" })
        {
            Assert.DoesNotContain(token, code, StringComparison.Ordinal);
        }
    }

    // GW-U-227 (AC-12, M1, M8, M9): the Membership folder holds exactly the eight ADR-GWIN-022 files, and every file is pure and game-free.
    [Fact]
    public void MembershipFilesArePureGameFreeAndSettingsFree()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(MembershipFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        foreach (string name in names)
        {
            string relative = MembershipFolder + "/" + name;
            string raw = RepoRoot.ReadText(relative);
            string code = Strip(raw);

            Assert.False(string.IsNullOrWhiteSpace(code), name + " was read as empty.");
            Assert.DoesNotContain("using UnityEngine", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("using BepInEx", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("using HarmonyLib", raw, StringComparison.Ordinal);

            foreach (string token in ForbiddenTokens)
            {
                Assert.False(code.Contains(token, StringComparison.Ordinal), name + " must not contain " + token + ".");
            }

            Assert.False(HasMutableStaticField(code), name + " declares a static field that is neither const nor readonly.");

            if (relative != EligibilityOptionsFile)
            {
                Assert.False(code.Contains("BaseStorageSettings", StringComparison.Ordinal), name + " must not read BaseStorageSettings.");
            }
        }
    }

    /// <summary>True when the code declares a static field (a line of static declaration with no parenthesis before = or ;) that is not const or readonly.</summary>
    private static bool HasMutableStaticField(string code)
    {
        foreach (Match match in Regex.Matches(code, @"(?m)^([^=\n(]*\bstatic\b[^=\n(]*)[=;]"))
        {
            string declaration = match.Groups[1].Value;
            if (!Regex.IsMatch(declaration, @"\b(readonly|const|class|operator)\b"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
