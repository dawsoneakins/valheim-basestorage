namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// AC-1 and contract K1: the three key literals are declared only in PersistenceKeys.cs under src/ (ADR-GWIN-013).
/// Callers and docs use the constants, so the scan reads the production sources only.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PersistenceKeySingleSourceTests
{
    private const string KeysFile = "src/BaseStorage.Core/Persistence/PersistenceKeys.cs";

    private static readonly string[] Literals =
    [
        "\"basestorage.cat\"",
        "\"basestorage.lock\"",
        "\"basestorage.fav\"",
    ];

    // GW-U-068
    [Fact]
    public void KeyLiteralsAppearOnlyInPersistenceKeysFile()
    {
        string keysText = RepoRoot.ReadText(KeysFile);
        Dictionary<string, string> sources = ScanProductionSources();

        Assert.Contains(KeysFile, sources.Keys);
        Assert.Contains(Literals[0], keysText, StringComparison.Ordinal);

        foreach (string literal in Literals)
        {
            string[] files = sources
                .Where(pair => pair.Value.Contains(literal, StringComparison.Ordinal))
                .Select(pair => pair.Key)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(new[] { KeysFile }, files);
        }
    }

    /// <summary>Every *.cs file under src/ except bin and obj, keyed by its repo-relative forward-slash path.</summary>
    private static Dictionary<string, string> ScanProductionSources()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(RepoRoot.Combine("src"), "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(RepoRoot.FullPath, file).Replace('\\', '/');
            if (relative.Split('/').Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }

            result[relative] = RepoRoot.Normalise(File.ReadAllText(file));
        }

        return result;
    }
}
