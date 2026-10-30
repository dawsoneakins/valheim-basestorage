namespace BaseStorage.Core.Tests.Repo;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/// <summary>AC-7: ValheimDir resolution order and the Directory.Build.props import order.</summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class ValheimDirResolutionTests
{
    private const string DefaultValheimDir = @"C:\Program Files (x86)\Steam\steamapps\common\Valheim";
    private const string UserValheimDir = @"D:\SteamLibrary\steamapps\common\Valheim";
    private const string EnvValheimDir = @"E:\Games\Valheim";

    // GW-U-012
    [Fact]
    public void PropsImportsVersionFirstAndUserPropsLastBehindAnExistsGuard()
    {
        XDocument doc = RepoRoot.LoadXml(ContractPaths.DirectoryBuildProps);
        XElement[] imports = RepoRoot.Named(doc, "Import").ToArray();

        Assert.Equal(2, imports.Length);
        Assert.Equal("$(MSBuildThisFileDirectory)Version.props", RepoRoot.Attr(imports[0], "Project"));
        Assert.Equal(string.Empty, RepoRoot.Attr(imports[0], "Condition"));
        Assert.Equal("$(MSBuildThisFileDirectory)Directory.Build.user.props", RepoRoot.Attr(imports[1], "Project"));
        Assert.Contains(
            "Exists('$(MSBuildThisFileDirectory)Directory.Build.user.props')",
            RepoRoot.Attr(imports[1], "Condition"));
    }

    // GW-U-012
    [Fact]
    public void PropsDefaultLiteralIsTheSteamInstallPath()
    {
        Assert.Contains(DefaultValheimDir, RepoRoot.ReadText(ContractPaths.DirectoryBuildProps));
    }

    // GW-I-001
    [Theory]
    [InlineData(null, null, DefaultValheimDir)]
    [InlineData(null, EnvValheimDir, EnvValheimDir)]
    [InlineData(UserValheimDir, EnvValheimDir, UserValheimDir)]
    [InlineData(UserValheimDir, null, UserValheimDir)]
    [InlineData(null, "", DefaultValheimDir)]
    public void ValheimDirPrecedenceResolvesThroughMsBuild(string? userDir, string? envDir, string expected)
    {
        using var probeFolder = new TempFolder();
        File.Copy(RepoRoot.Combine(ContractPaths.DirectoryBuildProps), probeFolder.Combine(ContractPaths.DirectoryBuildProps));
        File.Copy(RepoRoot.Combine(ContractPaths.VersionProps), probeFolder.Combine(ContractPaths.VersionProps));
        File.WriteAllText(
            probeFolder.Combine("probe.proj"),
            "<Project>\n  <Import Project=\"Directory.Build.props\" />\n</Project>\n");

        if (userDir is not null)
        {
            File.WriteAllText(
                probeFolder.Combine("Directory.Build.user.props"),
                "<Project>\n  <PropertyGroup>\n    <ValheimDir>" + userDir + "</ValheimDir>\n  </PropertyGroup>\n</Project>\n");
        }

        IReadOnlyDictionary<string, string>? environment = envDir is null
            ? null
            : new Dictionary<string, string> { ["VALHEIM_DIR"] = envDir };

        DotnetResult result = DotnetProcess.Msbuild(
            probeFolder.Combine("probe.proj"),
            ["-getProperty:ValheimDir"],
            environment);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expected, result.Output.Trim());
    }
}
