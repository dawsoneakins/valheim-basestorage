namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// AC-1: the PatchTargets.Tests sources never load, run or write game code or folders (contract 4.6.1). A text scan of
/// the test project's .cs files, with // comments removed. The only exceptions are the temp-folder calls in
/// SkipGateTests.cs and BuildValuesTests.cs, and those files must use Path.GetTempPath().
/// </summary>
[Trait("Category", "Repo")]
public sealed class PatchTargetsReadOnlyScanTests
{
    private const string TestsFolder = "tests/BaseStorage.PatchTargets.Tests";
    private const string FixtureFile = "Infrastructure/ValheimAssemblyFixture.cs";
    private const string CheckerFile = "Checking/GameTargetChecker.cs";

    private static readonly string[] Forbidden =
    [
        "Assembly.Load",
        "LoadFrom(",
        "LoadFile(",
        "AssemblyLoadContext.Default",
        "AssemblyLoadContext.Load",
        "Activator.",
        ".Invoke(",
        "CreateInstance",
        "File.Write",
        "File.Create",
        "File.Delete",
        "File.Copy",
        "File.Move",
        "File.SetLastWriteTime",
        "FileMode.Create",
        "FileMode.Append",
        "FileMode.Truncate",
        "FileMode.OpenOrCreate",
        "FileAccess.Write",
        "FileAccess.ReadWrite",
        "Directory.CreateDirectory(",
        "Directory.Delete(",
    ];

    private static readonly string[] TempFolderFiles = ["SkipGateTests.cs", "BuildValuesTests.cs"];
    private static readonly string[] TempFolderCalls = ["Directory.CreateDirectory(", "Directory.Delete("];

    // GW-U-106
    [Fact]
    public void NoSourceCallsLoadRunOrWritesGameCode()
    {
        Dictionary<string, string> sources = ReadSources();
        List<string> offences = [];

        foreach ((string file, string text) in sources)
        {
            foreach (string token in Forbidden)
            {
                if (!text.Contains(token, StringComparison.Ordinal))
                {
                    continue;
                }

                bool tempOnly = TempFolderFiles.Contains(Path.GetFileName(file)) && TempFolderCalls.Contains(token);
                if (tempOnly && text.Contains("Path.GetTempPath()", StringComparison.Ordinal))
                {
                    continue;
                }

                offences.Add(file + ": " + token);
            }
        }

        Assert.Empty(offences);
    }

    // GW-U-106
    [Fact]
    public void TheMetadataLoadContextIsCreatedExactlyOnceInTheFixture()
    {
        Dictionary<string, string> sources = ReadSources();

        Assert.Contains(sources.Keys, key => key.EndsWith(FixtureFile, StringComparison.Ordinal));
        Assert.Contains(sources.Keys, key => key.EndsWith(CheckerFile, StringComparison.Ordinal));

        string[] hits = sources
            .Where(source => source.Value.Contains("new MetadataLoadContext(", StringComparison.Ordinal))
            .Select(source => source.Key)
            .ToArray();

        Assert.Single(hits);
        Assert.EndsWith(FixtureFile, hits[0], StringComparison.Ordinal);
    }

    private static Dictionary<string, string> ReadSources()
    {
        string root = RepoRoot.Combine(TestsFolder);
        Dictionary<string, string> result = new(StringComparer.Ordinal);

        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (RepoRoot.IsExcludedPath(TestsFolder + "/" + relative))
            {
                continue;
            }

            result[relative] = StripLineComments(RepoRoot.Normalise(File.ReadAllText(file)));
        }

        Assert.NotEmpty(result);
        return result;
    }

    private static string StripLineComments(string text)
    {
        return string.Join(
            "\n",
            text.Split('\n').Select(line =>
            {
                int index = line.IndexOf("//", StringComparison.Ordinal);
                return index >= 0 ? line[..index] : line;
            }));
    }
}
