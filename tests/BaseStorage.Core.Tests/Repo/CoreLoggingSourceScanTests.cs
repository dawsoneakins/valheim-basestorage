namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;

/// <summary>
/// L1 Core purity of the log policy (log-policy-contract.md v1, section 0): the raw text of the three Core logging files,
/// comments included, names no Unity, BepInEx, Harmony, Valheim or ManualLogSource type (ADR-GWIN-023).
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreLoggingSourceScanTests
{
    private const string Folder = "src/BaseStorage.Core/Logging";

    private static readonly string[] ExpectedFiles = { "DebugGatedLogSink.cs", "LogCategory.cs", "LogPolicy.cs" };

    private static readonly string[] ForbiddenTokens =
    {
        "UnityEngine", "BepInEx", "HarmonyLib", "Valheim", "ManualLogSource", "Debug.Log",
    };

    // GW-U-215
    [Fact]
    public void CoreLoggingFilesNameNoEngineOrPluginTypeEvenInComments()
    {
        string folder = RepoRoot.Combine(Folder);
        Assert.True(Directory.Exists(folder), $"Scan folder is missing: {Folder}");

        string[] files = Directory.EnumerateFiles(folder, "*.cs")
            .Select(file => Path.GetFileName(file) ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        foreach (string name in ExpectedFiles)
        {
            string text = RepoRoot.ReadText(Folder + "/" + name);
            foreach (string token in ForbiddenTokens)
            {
                Assert.False(text.Contains(token, StringComparison.Ordinal), $"{Folder}/{name} mentions {token}.");
            }
        }
    }
}
