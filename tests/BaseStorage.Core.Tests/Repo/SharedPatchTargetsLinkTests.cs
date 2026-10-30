namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

/// <summary>
/// AC-9: src/Shared/PatchTargetNames.cs is one file. It is linked, not copied, into the Plugin and PatchTargets.Tests
/// projects, and no other project compiles it (contract section 4.1, ADR-GWIN-007).
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class SharedPatchTargetsLinkTests
{
    private const string SharedFile = "src/Shared/PatchTargetNames.cs";
    private const string SharedFolder = "src/Shared";
    private const string SharedLink = "Shared\\PatchTargetNames.cs";
    private const string FileName = "PatchTargetNames.cs";

    // GW-U-042
    [Fact]
    public void SharedFileExistsAndTheSharedFolderHoldsNoProject()
    {
        Assert.True(RepoRoot.Exists(SharedFile), $"{SharedFile} is missing.");

        string folder = RepoRoot.Combine(SharedFolder);
        string[] projects = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.csproj") : Array.Empty<string>();
        Assert.Empty(projects);
    }

    // GW-U-042
    [Theory]
    [InlineData(ContractPaths.PluginProject, "..\\Shared\\PatchTargetNames.cs")]
    [InlineData(ContractPaths.PatchTargetsProject, "..\\..\\src\\Shared\\PatchTargetNames.cs")]
    public void LinkingProjectsIncludeTheSharedFileOnceWithTheContractedLink(string project, string include)
    {
        string projectDirectory = Path.GetDirectoryName(RepoRoot.Combine(project))!;
        string resolved = Path.GetFullPath(Path.Combine(projectDirectory, include.Replace('\\', Path.DirectorySeparatorChar)));
        Assert.Equal(RepoRoot.Combine(SharedFile), resolved, ignoreCase: true);

        XElement[] matches = RepoRoot.Named(RepoRoot.LoadXml(project), "Compile")
            .Where(c => RepoRoot.Attr(c, "Include") == include)
            .ToArray();
        Assert.Single(matches);
        Assert.Equal(SharedLink, RepoRoot.Attr(matches[0], "Link"));
    }

    // GW-U-042
    [Theory]
    [InlineData(ContractPaths.CoreProject)]
    [InlineData(ContractPaths.CoreTestsProject)]
    public void CoreProjectsDoNotMentionThePatchTargetFile(string project)
    {
        Assert.DoesNotContain("PatchTargetNames", RepoRoot.ReadText(project));
    }

    // GW-U-042
    [Fact]
    public void ExactlyOneCopyOfThePatchTargetFileExistsInTheRepo()
    {
        string root = RepoRoot.FullPath;
        string[] copies = Directory.EnumerateFiles(root, FileName, SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(relative => !RepoRoot.IsExcludedPath(relative))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { SharedFile }, copies);
    }

    // GW-U-043
    [Fact]
    public void SharedFileDeclaresTheSharedNamespaceAndTheInternalStaticClass()
    {
        RequireSharedFile();
        string text = RepoRoot.ReadText(SharedFile);

        Assert.Contains("namespace BaseStorage.Shared;", text);
        Assert.Contains("internal static class PatchTargetNames", text);
    }

    // GW-U-043
    [Fact]
    public void SharedFileHasNoUsingTypeofOrAttributeLines()
    {
        RequireSharedFile();
        string text = RepoRoot.ReadText(SharedFile);
        string[] lines = text.Split('\n').Select(line => line.Trim()).ToArray();

        Assert.DoesNotContain(lines, line => line.StartsWith("using ", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.StartsWith("[", StringComparison.Ordinal));
        Assert.DoesNotContain("typeof(", text);
    }

    // GW-U-043
    [Fact]
    public void SharedFileIsLfOnlyAndHasNoByteOrderMark()
    {
        RequireSharedFile();
        byte[] bytes = File.ReadAllBytes(RepoRoot.Combine(SharedFile));

        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Assert.False(hasBom, "PatchTargetNames.cs must not start with a UTF-8 byte order mark.");
        Assert.DoesNotContain((byte)'\r', bytes);
    }

    // GW-I-008
    [Theory]
    [InlineData(ContractPaths.PluginProject)]
    [InlineData(ContractPaths.PatchTargetsProject)]
    public void MsbuildCompilesTheSharedFileOnceInLinkingProjects(string project)
    {
        string sharedFull = RepoRoot.Combine(SharedFile);

        JsonElement[] matches = CompileItems(project)
            .Where(item => string.Equals(StringOf(item, "FullPath"), sharedFull, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Single(matches);
        Assert.Equal(SharedLink, StringOf(matches[0], "Link"));
    }

    // GW-I-008
    [Theory]
    [InlineData(ContractPaths.CoreProject)]
    [InlineData(ContractPaths.CoreTestsProject)]
    public void MsbuildDoesNotCompileTheSharedFileInCoreProjects(string project)
    {
        string sharedFull = RepoRoot.Combine(SharedFile);

        Assert.DoesNotContain(
            CompileItems(project),
            item => string.Equals(StringOf(item, "FullPath"), sharedFull, StringComparison.OrdinalIgnoreCase));
    }

    private static void RequireSharedFile()
    {
        Assert.True(RepoRoot.Exists(SharedFile), $"{SharedFile} is missing.");
    }

    /// <summary>The Compile items of a project, from msbuild -getItem (evaluation only, no build).</summary>
    private static IReadOnlyList<JsonElement> CompileItems(string project)
    {
        DotnetResult result = DotnetProcess.Msbuild(RepoRoot.Combine(project), ["-getItem:Compile"]);
        Assert.Equal(0, result.ExitCode);

        string output = result.Output;
        int start = output.IndexOf('{');
        int end = output.LastIndexOf('}');
        Assert.True(start >= 0 && end > start, "msbuild -getItem printed no JSON: " + output);

        using JsonDocument document = JsonDocument.Parse(output[start..(end + 1)]);
        if (!document.RootElement.TryGetProperty("Items", out JsonElement items)
            || !items.TryGetProperty("Compile", out JsonElement compile))
        {
            return Array.Empty<JsonElement>();
        }

        return compile.EnumerateArray().Select(item => item.Clone()).ToArray();
    }

    private static string StringOf(JsonElement item, string name) =>
        item.TryGetProperty(name, out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;
}
