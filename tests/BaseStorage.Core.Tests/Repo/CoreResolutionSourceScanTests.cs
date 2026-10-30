namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-1, AC-3, AC-4 (ADR-GWIN-024, ADR-GWIN-025): the Resolution sources name only the allowed Core types, read time only through IClock, and
/// keep the position math in the one range seam. The scans strip comments, normalise CRLF, and read the seven Resolution files by name. They are
/// text checks, not compiler checks; GW-U-255 is the compiled check of the same dependencies.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreResolutionSourceScanTests
{
    private const string ResolutionFolder = "src/BaseStorage.Core/Bases/Resolution";

    private static readonly string[] ExpectedFiles =
    {
        "BaseCache.cs",
        "BaseCacheOptions.cs",
        "BaseResolution.cs",
        "BaseResolver.cs",
        "BaseStart.cs",
        "BaseStartKind.cs",
        "IBaseSnapshotSource.cs",
    };

    private static readonly string[] ForbiddenNames =
    {
        "BaseStorage.Core.Settings",
        "BaseStorage.Core.Persistence",
        "BaseStorage.Core.Patching",
        "BaseStorage.Core.Diagnostics",
        "ILogSink",
        "IContainerLayer",
        "ContainerEligibility",
        "EligibilityOptions",
        "ContainerPurpose",
        "UnityEngine",
        "BepInEx",
        "Valheim",
    };

    private static readonly string[] TimeTokens = { "DateTime", "Stopwatch", "Environment.TickCount", "Time." };

    // GW-U-257 (AC-4): the folder holds exactly the seven ADR-GWIN-024 files, and none of them names a forbidden type, a feature word or a
    // game assembly. The only StringKeys member read is MessageBaseNotInBase.
    [Fact]
    public void ResolutionFilesAreTheSevenNamedFilesAndUseOnlyAllowedNames()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(ResolutionFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, names);

        var stringKeyMembers = new List<string>();
        foreach (string name in names)
        {
            string code = CodeOf(name);
            Assert.False(string.IsNullOrWhiteSpace(code), name + " was read as empty.");

            foreach (string forbidden in ForbiddenNames)
            {
                Assert.DoesNotContain(forbidden, code, StringComparison.Ordinal);
            }

            Assert.False(
                Regex.IsMatch(code, @"\b(Organi[sz]e|Deposit|Craft|Feed|Search|Hammer|Recipe|Piece)\w*"),
                name + " names a feature word.");

            stringKeyMembers.AddRange(Regex.Matches(code, @"StringKeys\.(\w+)").Select(match => match.Groups[1].Value));
        }

        Assert.All(stringKeyMembers, member => Assert.Equal("MessageBaseNotInBase", member));
    }

    // GW-U-257 (AC-3): the Resolution code reads no wall clock, timer or environment tick; time comes only through IClock, and IClock
    // appears only in BaseCache.cs.
    [Fact]
    public void TimeComesOnlyFromIClock()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            foreach (string token in TimeTokens)
            {
                Assert.DoesNotContain(token, code, StringComparison.Ordinal);
            }

            if (name != "BaseCache.cs")
            {
                Assert.DoesNotContain("IClock", code, StringComparison.Ordinal);
            }
        }

        Assert.Contains("IClock", CodeOf("BaseCache.cs"), StringComparison.Ordinal);
    }

    // GW-U-257 (AC-1): the position math stays in the one range seam. IsInRange( appears only in BaseResolver.cs, and no Resolution file
    // takes a square root or a distance, which is consistent with the GW-U-125 allowlist.
    [Fact]
    public void PositionMathStaysInTheRangeSeam()
    {
        foreach (string name in ExpectedFiles)
        {
            string code = CodeOf(name);
            Assert.DoesNotContain("Sqrt", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Distance", code, StringComparison.Ordinal);

            if (name != "BaseResolver.cs")
            {
                Assert.DoesNotContain("IsInRange(", code, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>The code of one Resolution file, with comments removed, so only code is matched.</summary>
    private static string CodeOf(string name) => Strip(RepoRoot.ReadText(ResolutionFolder + "/" + name));

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }
}
