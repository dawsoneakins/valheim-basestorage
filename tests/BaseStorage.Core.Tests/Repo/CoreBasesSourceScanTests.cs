namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// AC-2 (contract 7.4, ADR-GWIN-016, ADR-GWIN-017): the build-range rule lives in one place in Core. The scans strip comments,
/// normalise CRLF, and assert that they read the 3 Bases files. Each scan targets the text of one file, and none of them is a
/// compiler check. ADR-GWIN-022 adds Bases/Membership/BaseMembership.cs to the files allowed to call the seam, and ADR-GWIN-024 adds
/// Bases/Resolution/BaseResolver.cs.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreBasesSourceScanTests
{
    private const string CoreFolder = "src/BaseStorage.Core";
    private const string BasesFolder = "src/BaseStorage.Core/Bases";
    private const string BuildRangeRuleFile = BasesFolder + "/BuildRangeRule.cs";
    private const string StationGraphFile = BasesFolder + "/StationGraph.cs";
    private const string Signature = "internal static bool IsInRange(WorldPosition stationPosition, float buildRange, WorldPosition point)";

    private static readonly string[] ExpectedBasesFiles =
    {
        "BuildRangeRule.cs",
        "StationGraph.cs",
        "StationGraphResult.cs",
    };

    private static readonly string[] SourceExtensions = { ".cs", ".csproj", ".props", ".targets" };

    // GW-U-125 (AC-2)
    [Fact]
    public void ScanReadsTheThreeBasesFiles()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(BasesFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedBasesFiles, names);
    }

    // GW-U-125 (AC-2): BuildRangeRule declares the contracted signature and the verified body (horizontal, strict).
    [Fact]
    public void BuildRangeRuleDeclaresTheContractedSignatureAndTheVerifiedBody()
    {
        string code = Strip(RepoRoot.ReadText(BuildRangeRuleFile));

        Assert.Contains("internal static class BuildRangeRule", code, StringComparison.Ordinal);
        Assert.Contains(Signature, Collapse(code), StringComparison.Ordinal);
        Assert.Contains("Math.Sqrt(", code, StringComparison.Ordinal);
        Assert.Contains("< buildRange", code, StringComparison.Ordinal);
        Assert.DoesNotContain(".Y", code, StringComparison.Ordinal);
        Assert.DoesNotContain("<=", code, StringComparison.Ordinal);
    }

    // GW-U-125 (AC-2): StationGraph calls the seam and does no distance arithmetic of its own.
    [Fact]
    public void StationGraphCallsTheSeamAndDoesNoDistanceArithmetic()
    {
        string code = Strip(RepoRoot.ReadText(StationGraphFile));

        Assert.Contains("BuildRangeRule.IsInRange(", code, StringComparison.Ordinal);
        foreach (string token in new[] { "Math.", ".X", ".Z", "Distance" })
        {
            Assert.DoesNotContain(token, code, StringComparison.Ordinal);
        }
    }

    // GW-U-125 (AC-2): no other Core file measures distance or calls the seam.
    [Fact]
    public void NoOtherCoreFileHasSqrtOrTheSeamCall()
    {
        string[] allowed = { BuildRangeRuleFile, StationGraphFile, BasesFolder + "/Membership/BaseMembership.cs", BasesFolder + "/Resolution/BaseResolver.cs" };
        string[] files = SourceFilesUnder(CoreFolder);
        string[] others = files.Where(relative => !allowed.Contains(relative, StringComparer.Ordinal)).ToArray();

        Assert.Contains(BuildRangeRuleFile, files);
        Assert.Contains(StationGraphFile, files);

        string[] offenders = others
            .Where(relative =>
            {
                string code = Strip(RepoRoot.ReadText(relative));
                return code.Contains("Math.Sqrt", StringComparison.Ordinal) || code.Contains("IsInRange(", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.Empty(offenders);
    }

    // GW-U-125 (AC-2): InternalsVisibleTo is absent from src/ (contract 7.4 forbids it).
    [Fact]
    public void InternalsVisibleToAppearsNowhereInSrc()
    {
        string[] files = SourceFilesUnder("src");
        string[] hits = files
            .Where(relative => RepoRoot.ReadText(relative).Contains("InternalsVisibleTo", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(files);
        Assert.Empty(hits);
    }

    /// <summary>Root-relative, forward-slash source files under a folder, skipping bin, obj and nested worktrees.</summary>
    private static string[] SourceFilesUnder(string folder) =>
        Directory.EnumerateFiles(RepoRoot.Combine(folder), "*", SearchOption.AllDirectories)
            .Where(file => SourceExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/'))
            .Where(relative => !RepoRoot.IsExcludedPath(relative))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Removes block and line comments (including XML doc comments), so only code is matched.</summary>
    private static string Strip(string text)
    {
        string withoutBlocks = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//[^\n]*", string.Empty);
    }

    /// <summary>Collapses every run of whitespace to one space, so a signature can be matched on one line.</summary>
    private static string Collapse(string text) => Regex.Replace(text, @"\s+", " ");
}
