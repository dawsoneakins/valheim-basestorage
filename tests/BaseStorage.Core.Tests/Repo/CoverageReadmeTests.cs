namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// AC-9.1 (S-E009-001, ADR-GWIN-162, contract quality-audit 1.4): the root README pins one Core coverage section, with the exact
/// coverlet command, the report path, the reading rule and the 85% bar. The test reads the README as text and never runs coverage.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoverageReadmeTests
{
    private const string ReadmeFile = "README.md";
    private const string CoverageHeading = "## Core coverage (AC-9.1)";
    private const string BuildHeading = "## Build, test and format";
    private const string PatchTargetsHeading = "## Patch-target contract tests (AC-1.4)";
    private const string Command = "dotnet test tests/BaseStorage.Core.Tests -c Release --nologo --collect:\"XPlat Code Coverage\" --results-directory TestResults/coverage";
    private const string Collector = "--collect:\"XPlat Code Coverage\"";

    // GW-U-2340 (AC-2, QA1.4): exactly one coverage heading, between the build heading and the patch-target heading.
    [Fact]
    public void TheCoverageSectionSitsBetweenBuildAndPatchTargets()
    {
        string[] lines = Lines();
        int count = lines.Count(line => line == CoverageHeading);
        Assert.True(count == 1, $"README.md must have exactly one '{CoverageHeading}' line, found {count}.");

        int index = Array.IndexOf(lines, CoverageHeading);
        int before = NearestLevel2(lines, index, -1);
        int after = NearestLevel2(lines, index, 1);
        Assert.True(before >= 0 && lines[before] == BuildHeading, $"The level-2 heading before '{CoverageHeading}' must be '{BuildHeading}'.");
        Assert.True(after >= 0 && lines[after] == PatchTargetsHeading, $"The level-2 heading after '{CoverageHeading}' must be '{PatchTargetsHeading}'.");
    }

    // GW-U-2341 (AC-2, QA1.1, QA1.4): the section holds the command as one whole line and names no release command.
    [Fact]
    public void TheCoverageCommandAppearsVerbatimAsAWholeLine()
    {
        string[] section = Section();
        Assert.True(section.Contains(Command, StringComparer.Ordinal), $"The coverage section has no line equal to: {Command}");

        string body = string.Join("\n", section);
        Assert.DoesNotContain("DeployToGame", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PackageThunderstore", body, StringComparison.OrdinalIgnoreCase);
    }

    // GW-U-2342 (AC-1, AC-2, QA1.3): the section names the report path, the attribute, the package name and the bar.
    [Fact]
    public void TheSectionNamesTheReportPathTheAttributeAndTheBar()
    {
        string body = string.Join("\n", Section());
        Assert.Contains("TestResults/coverage/<run-guid>/coverage.cobertura.xml", body, StringComparison.Ordinal);
        Assert.Contains("line-rate", body, StringComparison.Ordinal);
        Assert.Contains("<package name=\"BaseStorage.Core\"", body, StringComparison.Ordinal);
        Assert.Contains("85%", body, StringComparison.Ordinal);
    }

    // GW-U-2343 (AC-1, QA1.4): the README command uses the collector that the Core test csproj references.
    [Fact]
    public void TheCoverageCommandUsesTheReferencedCollector()
    {
        string commandLine = Section().FirstOrDefault(line => line.StartsWith("dotnet test ", StringComparison.Ordinal)) ?? string.Empty;
        Assert.True(commandLine.Length > 0, "The coverage section has no 'dotnet test' command line.");
        Assert.Contains(Collector, commandLine, StringComparison.Ordinal);

        XDocument project = RepoRoot.LoadXml(ContractPaths.CoreTestsProject);
        bool referenced = RepoRoot.Named(project, "PackageReference")
            .Any(element => RepoRoot.Attr(element, "Include") == "coverlet.collector");
        Assert.True(referenced, $"{ContractPaths.CoreTestsProject} has no PackageReference Include=\"coverlet.collector\".");
    }

    private static string[] Lines() =>
        RepoRoot.ReadText(ReadmeFile).Split('\n').Select(line => line.TrimEnd(' ', '\t')).ToArray();

    /// <summary>The coverage section: from its heading line up to the next level-2 heading, or the end of the file.</summary>
    private static string[] Section()
    {
        string[] lines = Lines();
        int start = Array.IndexOf(lines, CoverageHeading);
        Assert.True(start >= 0, $"README.md has no '{CoverageHeading}' line.");

        int end = NearestLevel2(lines, start, 1);
        return lines[start..(end < 0 ? lines.Length : end)];
    }

    /// <summary>The index of the nearest level-2 heading before (step -1) or after (step 1) the index, or -1.</summary>
    private static int NearestLevel2(string[] lines, int index, int step)
    {
        for (int i = index + step; i >= 0 && i < lines.Length; i += step)
        {
            if (lines[i].StartsWith("## ", StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
