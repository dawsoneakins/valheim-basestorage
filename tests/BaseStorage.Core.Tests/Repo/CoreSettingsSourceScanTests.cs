namespace BaseStorage.Core.Tests.Repo;

using System;
using System.IO;
using System.Linq;

/// <summary>
/// AC-6 (contract 0 rule S1, ADR-GWIN-014/015): no Core settings file names an engine type, not even in a comment or XML doc.
/// The scan reads the raw text, so comments are included, and it asserts that it read the 9 expected files.
/// </summary>
[Trait("Category", "Repo")]
public sealed class CoreSettingsSourceScanTests
{
    private const string SettingsFolder = "src/BaseStorage.Core/Settings";

    private static readonly string[] ExpectedFiles =
    {
        "BaseStorageSettings.cs",
        "ISettingsSource.cs",
        "ItemListText.cs",
        "ItemListTokens.cs",
        "SettingDefinition.cs",
        "SettingKeys.cs",
        "SettingKind.cs",
        "SettingSections.cs",
        "SettingsCatalog.cs",
    };

    private static readonly string[] ForbiddenTokens = { "KeyboardShortcut", "KeyCode", "ConfigEntry", "ConfigFile", "BepInEx", "UnityEngine" };

    // GW-U-109 (AC-6)
    [Fact]
    public void NoCoreSettingsFileNamesAnEngineTypeOrNamespaceIncludingInComments()
    {
        string[] names = Directory.EnumerateFiles(RepoRoot.Combine(SettingsFolder), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles.OrderBy(name => name, StringComparer.Ordinal), names);

        foreach (string name in names)
        {
            string text = RepoRoot.ReadText(SettingsFolder + "/" + name);
            foreach (string token in ForbiddenTokens)
            {
                Assert.False(text.Contains(token, StringComparison.Ordinal), $"{SettingsFolder}/{name} contains the engine token {token}.");
            }
        }
    }
}
