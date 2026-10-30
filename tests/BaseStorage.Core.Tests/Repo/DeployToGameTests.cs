namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

/// <summary>AC-14, AC-15: the DeployToGame target, its success and skip messages, and its default-off state.</summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class DeployToGameTests
{
    private const string Bs1002Prefix = "BS1002: DeployToGame skipped: '";
    private const string Bs1003Prefix = "BS1003: DeployToGame copied 2 file(s) to '";

    // GW-U-020
    [Fact]
    public void DeployTargetRunsAfterBuildOnlyWhenEnabledAndCopiesTwoFiles()
    {
        XElement? target = RepoRoot.Named(RepoRoot.LoadXml(ContractPaths.PluginProject), "Target")
            .SingleOrDefault(t => RepoRoot.Attr(t, "Name") == "DeployToGame");
        Assert.NotNull(target);
        XElement deploy = target!;

        Assert.Equal("Build", RepoRoot.Attr(deploy, "AfterTargets"));
        Assert.Equal("'$(DeployToGame)' == 'true'", RepoRoot.Attr(deploy, "Condition"));
        Assert.DoesNotContain(deploy.Descendants(), e => e.Name.LocalName is "Warning" or "Error");

        string[] includes = deploy.Descendants()
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .SelectMany(v => v!.Split(';'))
            .Select(v => v.Trim())
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "$(TargetDir)BaseStorage.Core.dll", "$(TargetPath)" },
            includes);
    }

    // GW-I-004
    [Fact]
    public void DeploySuccessCopiesExactlyTwoFilesAndLogsBs1003()
    {
        using var gameFolder = new TempFolder();
        using var outputFolder = new TempFolder();
        Directory.CreateDirectory(gameFolder.Combine("BepInEx"));
        WriteFakeOutputs(outputFolder);

        DotnetResult result = Deploy(gameFolder, outputFolder, deployProperty: true);

        Assert.Equal(0, result.ExitCode);
        string destination = gameFolder.Combine("BepInEx", "plugins", "BaseStorage");
        string[] files = Directory.EnumerateFiles(gameFolder.Root, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(gameFolder.Root, file))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                Path.Combine("BepInEx", "plugins", "BaseStorage", "BaseStorage.Core.dll"),
                Path.Combine("BepInEx", "plugins", "BaseStorage", "BaseStorage.dll"),
            },
            files);
        Assert.Contains(
            Bs1003Prefix + destination + Path.DirectorySeparatorChar + "'.",
            RepoRoot.Normalise(result.Output));
    }

    // GW-I-005
    [Fact]
    public void DeploySkipWithoutBepInExLogsBs1002AndChangesNothing()
    {
        using var gameFolder = new TempFolder();
        using var outputFolder = new TempFolder();
        WriteFakeOutputs(outputFolder);
        string[] before = RecursiveListing(gameFolder.Root);

        DotnetResult result = Deploy(gameFolder, outputFolder, deployProperty: true);

        Assert.Equal(0, result.ExitCode);
        string output = RepoRoot.Normalise(result.Output);
        Assert.Contains(Bs1002Prefix + gameFolder.Combine("BepInEx") + "' does not exist.", output);
        Assert.DoesNotContain(": warning ", output);
        Assert.Equal(before, RecursiveListing(gameFolder.Root));
    }

    // GW-I-006
    [Fact]
    public void DeployToGameIsOffByDefault()
    {
        DotnetResult result = DotnetProcess.Msbuild(
            RepoRoot.Combine(ContractPaths.PluginProject),
            ["-getProperty:DeployToGame"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("false", result.Output.Trim());
    }

    // GW-I-006
    [Fact]
    public void DeployTargetWithoutThePropertyDoesNothingEvenWithBepInExPresent()
    {
        using var gameFolder = new TempFolder();
        using var outputFolder = new TempFolder();
        Directory.CreateDirectory(gameFolder.Combine("BepInEx"));
        WriteFakeOutputs(outputFolder);

        DotnetResult result = Deploy(gameFolder, outputFolder, deployProperty: false);

        Assert.Equal(0, result.ExitCode);
        string output = RepoRoot.Normalise(result.Output);
        Assert.DoesNotContain("BS1002", output);
        Assert.DoesNotContain("BS1003", output);
        Assert.False(Directory.Exists(gameFolder.Combine("BepInEx", "plugins")));
    }

    private static DotnetResult Deploy(TempFolder gameFolder, TempFolder outputFolder, bool deployProperty)
    {
        var arguments = new List<string>
        {
            "-t:DeployToGame",
            "-p:Configuration=Release",
            "-p:ValheimDir=" + gameFolder.Root,
            "-p:OutDir=" + outputFolder.Root + Path.DirectorySeparatorChar,
        };
        if (deployProperty)
        {
            arguments.Add("-p:DeployToGame=true");
        }

        return DotnetProcess.Msbuild(RepoRoot.Combine(ContractPaths.PluginProject), arguments);
    }

    private static void WriteFakeOutputs(TempFolder outputFolder)
    {
        File.WriteAllText(outputFolder.Combine("BaseStorage.dll"), "fake");
        File.WriteAllText(outputFolder.Combine("BaseStorage.Core.dll"), "fake");
    }

    private static string[] RecursiveListing(string root) =>
        Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories)
            .Select(entry => Path.GetRelativePath(root, entry))
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToArray();
}
