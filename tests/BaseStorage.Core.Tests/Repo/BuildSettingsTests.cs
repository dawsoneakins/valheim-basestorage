namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;
using System.Xml.Linq;

/// <summary>AC-18: editor config, Release build settings and their evaluation through MSBuild.</summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class BuildSettingsTests
{
    private const string ReleaseCondition = "'$(Configuration)' == 'Release'";

    // GW-U-024
    [Fact]
    public void EditorConfigIsRootAndUsesLfUnderTheWildcardSection()
    {
        string[] lines = RepoRoot.ReadText(ContractPaths.EditorConfig).Split('\n').Select(l => l.Trim()).ToArray();

        Assert.Contains("root = true", lines);

        int start = Array.IndexOf(lines, "[*]");
        Assert.True(start >= 0, "The [*] section is missing from .editorconfig.");
        string[] section = lines.Skip(start + 1).TakeWhile(l => !l.StartsWith('[')).ToArray();
        Assert.Contains("end_of_line = lf", section);
    }

    // GW-U-024
    [Fact]
    public void DirectoryBuildPropsSetsLangVersionNullableAndWarningsAsErrorsInRelease()
    {
        XDocument doc = RepoRoot.LoadXml(ContractPaths.DirectoryBuildProps);

        Assert.Equal("latest", ValueOf(ContractPaths.DirectoryBuildProps, "LangVersion"));
        Assert.Equal("enable", ValueOf(ContractPaths.DirectoryBuildProps, "Nullable"));
        Assert.Equal("true", ValueOf(ContractPaths.DirectoryBuildProps, "TreatWarningsAsErrors"));

        XElement warnings = RepoRoot.Named(doc, "TreatWarningsAsErrors").Single();
        string condition = warnings.Attribute("Condition")?.Value ?? warnings.Parent?.Attribute("Condition")?.Value ?? string.Empty;
        Assert.Contains(ReleaseCondition, condition);
    }

    // GW-I-007
    [Theory]
    [InlineData(ContractPaths.CoreProject)]
    [InlineData(ContractPaths.PluginProject)]
    [InlineData(ContractPaths.CoreTestsProject)]
    [InlineData(ContractPaths.PatchTargetsProject)]
    public void ReleaseEvaluationSetsLangVersionAndTreatWarningsAsErrors(string project)
    {
        Assert.Equal("latest", Evaluate(project, "LangVersion", "Release"));
        Assert.Equal("true", Evaluate(project, "TreatWarningsAsErrors", "Release"));
    }

    // GW-I-007
    [Fact]
    public void CoreEvaluatesNullableEnable()
    {
        Assert.Equal("enable", Evaluate(ContractPaths.CoreProject, "Nullable", "Release"));
    }

    // GW-I-007
    [Theory]
    [InlineData(ContractPaths.CoreProject)]
    [InlineData(ContractPaths.PluginProject)]
    [InlineData(ContractPaths.CoreTestsProject)]
    [InlineData(ContractPaths.PatchTargetsProject)]
    public void DebugEvaluationDoesNotTreatWarningsAsErrors(string project)
    {
        Assert.NotEqual("true", Evaluate(project, "TreatWarningsAsErrors", "Debug"));
    }

    private static string ValueOf(string relative, string localName) =>
        RepoRoot.ValuesOf(relative, localName).SingleOrDefault() ?? string.Empty;

    private static string Evaluate(string project, string property, string configuration)
    {
        DotnetResult result = DotnetProcess.Msbuild(
            RepoRoot.Combine(project),
            ["-getProperty:" + property, "-p:Configuration=" + configuration]);

        Assert.Equal(0, result.ExitCode);
        return result.Output.Trim();
    }
}
