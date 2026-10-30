namespace BaseStorage.Core.Tests.Repo;

using System.IO;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// Repo paths named by contracts/build-layout.yml. Shared by every repo test (ADR-GWIN-004).
/// </summary>
public static class ContractPaths
{
    public const string Solution = "BaseStorage.sln";
    public const string CoreProject = "src/BaseStorage.Core/BaseStorage.Core.csproj";
    public const string PluginProject = "src/BaseStorage.Plugin/BaseStorage.Plugin.csproj";
    public const string CoreTestsProject = "tests/BaseStorage.Core.Tests/BaseStorage.Core.Tests.csproj";
    public const string PatchTargetsProject = "tests/BaseStorage.PatchTargets.Tests/BaseStorage.PatchTargets.Tests.csproj";
    public const string DirectoryBuildProps = "Directory.Build.props";
    public const string VersionProps = "Version.props";
    public const string NuGetConfig = "nuget.config";
    public const string EditorConfig = ".editorconfig";
    public const string GitIgnore = ".gitignore";
    public const string BuildLayoutContract = ".pipeline/contracts/build-layout.yml";
    public const string PipelineConfig = ".pipeline/config.yml";

    public static readonly string[] AllProjects = [CoreProject, PluginProject, CoreTestsProject, PatchTargetsProject];
}

/// <summary>
/// Finds the repository root (the folder that holds BaseStorage.sln) and reads repo files.
/// Not a test class.
/// </summary>
public static class RepoRoot
{
    private static readonly string[] ExcludedSegments = [".git", "bin", "obj", "TestResults"];
    private static readonly string[] MsBuildExtensions = [".csproj", ".props", ".targets"];
    private static readonly Lazy<string> Root = new(FindRoot);

    public static string FullPath => Root.Value;

    public static string Combine(params string[] parts) =>
        Path.GetFullPath(Path.Combine(new[] { FullPath }.Concat(parts).ToArray()));

    public static bool Exists(string relative) => File.Exists(Combine(relative));

    public static string Normalise(string text) => text.Replace("\r\n", "\n");

    public static string ReadText(string relative)
    {
        string path = Combine(relative);
        Assert.True(File.Exists(path), $"Required repo file is missing: {relative}");
        return Normalise(File.ReadAllText(path));
    }

    public static XDocument LoadXml(string relative)
    {
        string path = Combine(relative);
        Assert.True(File.Exists(path), $"Required repo file is missing: {relative}");
        return XDocument.Load(path);
    }

    public static IEnumerable<XElement> Named(XContainer container, string localName) =>
        container.Descendants().Where(e => e.Name.LocalName == localName);

    /// <summary>Trimmed values of every element named <paramref name="localName"/> in the XML file at <paramref name="relative"/>.</summary>
    public static string[] ValuesOf(string relative, string localName) =>
        Named(LoadXml(relative), localName).Select(e => e.Value.Trim()).ToArray();

    public static string Attr(XElement element, string name) =>
        element.Attribute(name)?.Value.Trim() ?? string.Empty;

    /// <summary>
    /// Tracked-looking MSBuild files (*.csproj, *.props, *.targets) under the repo root, as forward-slash
    /// paths relative to the root. Build output folders and the gitignored user props are skipped.
    /// </summary>
    public static IReadOnlyList<string> MsBuildFiles()
    {
        return Directory.EnumerateFiles(FullPath, "*", SearchOption.AllDirectories)
            .Where(file => MsBuildExtensions.Contains(Path.GetExtension(file) ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(FullPath, file).Replace('\\', '/'))
            .Where(relative => !IsExcludedPath(relative))
            .Where(relative => !string.Equals(Path.GetFileName(relative), "Directory.Build.user.props", StringComparison.OrdinalIgnoreCase))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// True for a root-relative, forward-slash path under .pipeline/worktrees/. Those are separate git
    /// worktrees for parallel stories (git-excluded), not part of this repo's tree.
    /// </summary>
    public static bool IsNestedWorktree(string relative) =>
        relative.StartsWith(".pipeline/worktrees/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for a root-relative, forward-slash path that has an excluded segment (.git, bin, obj, TestResults)
    /// or sits under a nested worktree.
    /// </summary>
    public static bool IsExcludedPath(string relative) =>
        relative.Split('/').Any(segment => ExcludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase))
        || IsNestedWorktree(relative);

    private static string FindRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ContractPaths.Solution)))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"{ContractPaths.Solution} was not found above {AppContext.BaseDirectory}.");
    }
}
