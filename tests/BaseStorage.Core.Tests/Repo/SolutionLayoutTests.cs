namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>AC-1: solution contents, target frameworks and the reference graph.</summary>
[Trait("Category", "Repo")]
public sealed class SolutionLayoutTests
{
    private const string SolutionFolderTypeGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

    private static readonly Regex ProjectLine = new(
        @"^Project\(""\{(?<type>[0-9A-Fa-f-]+)\}""\)\s*=\s*""[^""]*"",\s*""(?<path>[^""]+)"",",
        RegexOptions.Multiline);

    // GW-U-001
    [Fact]
    public void SolutionListsExactlyTheFourContractProjects()
    {
        Assert.True(RepoRoot.Exists(ContractPaths.Solution), "BaseStorage.sln is missing.");
        Assert.False(RepoRoot.Exists("BaseStorage.slnx"), "BaseStorage.slnx must not exist; AC-1 names BaseStorage.sln.");

        string text = RepoRoot.ReadText(ContractPaths.Solution);
        string[] paths = ProjectLine.Matches(text)
            .Where(m => !string.Equals(m.Groups["type"].Value, SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Groups["path"].Value.Replace('\\', '/'))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(4, paths.Length);
        Assert.Equal(
            ContractPaths.AllProjects.OrderBy(p => p, StringComparer.Ordinal).ToArray(),
            paths);
    }

    // GW-U-002
    [Theory]
    [InlineData(ContractPaths.CoreProject, "netstandard2.0")]
    [InlineData(ContractPaths.PluginProject, "net462")]
    [InlineData(ContractPaths.CoreTestsProject, "net10.0")]
    [InlineData(ContractPaths.PatchTargetsProject, "net10.0")]
    public void EachProjectHasASingleTargetFramework(string project, string expected)
    {
        XDocument doc = RepoRoot.LoadXml(project);

        Assert.Empty(RepoRoot.Named(doc, "TargetFrameworks"));
        string[] frameworks = RepoRoot.Named(doc, "TargetFramework").Select(e => e.Value.Trim()).ToArray();
        Assert.Equal(new[] { expected }, frameworks);
    }

    // GW-U-002
    [Fact]
    public void PluginAssemblyNameIsBaseStorage()
    {
        XDocument doc = RepoRoot.LoadXml(ContractPaths.PluginProject);
        string[] names = RepoRoot.Named(doc, "AssemblyName").Select(e => e.Value.Trim()).ToArray();

        Assert.Equal(new[] { "BaseStorage" }, names);
    }

    // GW-U-003
    [Fact]
    public void ReferenceGraphMatchesTheContract()
    {
        Assert.Equal(new[] { Canon(ContractPaths.CoreProject) }, ProjectReferenceTargets(ContractPaths.PluginProject));
        Assert.Empty(ProjectReferenceTargets(ContractPaths.CoreProject));
        Assert.Equal(new[] { Canon(ContractPaths.CoreProject) }, ProjectReferenceTargets(ContractPaths.CoreTestsProject));
        Assert.Empty(ProjectReferenceTargets(ContractPaths.PatchTargetsProject));

        foreach (string project in ContractPaths.AllProjects)
        {
            Assert.DoesNotContain(Canon(ContractPaths.PluginProject), ProjectReferenceTargets(project));
        }
    }

    // GW-U-003
    [Theory]
    [InlineData(ContractPaths.CoreTestsProject)]
    [InlineData(ContractPaths.PatchTargetsProject)]
    public void TestProjectsReferenceXunitAndItsRunnerAndTestSdk(string project)
    {
        string[] ids = RepoRoot.Named(RepoRoot.LoadXml(project), "PackageReference")
            .Select(e => RepoRoot.Attr(e, "Include"))
            .ToArray();

        Assert.Contains("xunit", ids);
        Assert.Contains("xunit.runner.visualstudio", ids);
        Assert.Contains("Microsoft.NET.Test.Sdk", ids);
    }

    private static string Canon(string repoRelative) => RepoRoot.Combine(repoRelative).ToUpperInvariant();

    private static string[] ProjectReferenceTargets(string project)
    {
        string projectDir = Path.GetDirectoryName(RepoRoot.Combine(project))!;
        return RepoRoot.Named(RepoRoot.LoadXml(project), "ProjectReference")
            .Select(e => Path.GetFullPath(Path.Combine(projectDir, RepoRoot.Attr(e, "Include"))).ToUpperInvariant())
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
    }
}
