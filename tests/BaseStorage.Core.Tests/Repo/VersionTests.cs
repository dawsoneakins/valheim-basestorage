namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

/// <summary>AC-12: Version.props is the sole version source.</summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class VersionTests
{
    private static readonly string[] VersionProperties =
        ["Version", "VersionPrefix", "AssemblyVersion", "FileVersion", "InformationalVersion"];

    // GW-U-017
    [Fact]
    public void VersionPropsDefinesTheSoleVersion010()
    {
        string[] versions = RepoRoot.ValuesOf(ContractPaths.VersionProps, "Version");

        Assert.Equal(new[] { "0.4.3" }, versions);
    }

    // GW-U-017
    [Fact]
    public void NoOtherTrackedMsBuildFileDefinesAVersionProperty()
    {
        IReadOnlyList<string> files = RepoRoot.MsBuildFiles();
        Assert.Contains(ContractPaths.DirectoryBuildProps, files);
        Assert.Contains(ContractPaths.VersionProps, files);

        var offenders = new List<string>();
        foreach (string file in files.Where(f => f != ContractPaths.VersionProps))
        {
            foreach (XElement property in RepoRoot.LoadXml(file).Descendants()
                         .Where(e => e.Parent?.Name.LocalName == "PropertyGroup"
                                     && VersionProperties.Contains(e.Name.LocalName)))
            {
                offenders.Add(file + " <" + property.Name.LocalName + ">");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Version properties must come only from Version.props:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    // GW-I-003
    [Theory]
    [InlineData(ContractPaths.CoreProject)]
    [InlineData(ContractPaths.PluginProject)]
    [InlineData(ContractPaths.CoreTestsProject)]
    [InlineData(ContractPaths.PatchTargetsProject)]
    public void VersionPropertyEvaluatesTo010ForEachProject(string project)
    {
        DotnetResult result = DotnetProcess.Msbuild(RepoRoot.Combine(project), ["-getProperty:Version"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0.4.3", result.Output.Trim());
    }
}
