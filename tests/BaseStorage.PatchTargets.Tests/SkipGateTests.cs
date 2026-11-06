namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Infrastructure;

/// <summary>
/// AC-4: the skip decision texts and order (contract 4.8.2, error-codes.md BST2002 to BST2004), on temp folders that
/// are deleted afterwards. The tier attributes are checked for the right wiring, not for a literal Skip.
/// </summary>
public sealed class SkipGateTests : IDisposable
{
    private const string Tail = " Set VALHEIM_DIR or create Directory.Build.user.props with <ValheimDir>, then rebuild the tests.";

    private readonly List<string> folders = [];

    // GW-U-102
    [Fact]
    public void MissingFolderGivesBst2002()
    {
        string missing = Path.Combine(Path.GetTempPath(), "NoValheim-" + Guid.NewGuid().ToString("N"));

        Assert.Equal(
            "BST2002: Skipped: ValheimDir '" + missing + "' does not exist." + Tail,
            Decide(missing, @"C:\p\Dons-QOL.dll", needsPlugin: false));
    }

    // GW-U-102
    [Fact]
    public void EmptyFolderGivesBst2003NamingTheGameDll()
    {
        string folder = TempFolder("EmptyValheim");

        Assert.Equal(
            "BST2003: Skipped: '" + folder + @"\valheim_Data\Managed\assembly_valheim.dll' does not exist." + Tail,
            Decide(folder, @"C:\p\Dons-QOL.dll", needsPlugin: false));
    }

    // GW-U-102
    [Fact]
    public void MissingPluginGivesBst2004WithThePathAndTheConfiguration()
    {
        string folder = TempFolder("PlaceholderValheim");
        string plugin = Path.Combine(Path.GetTempPath(), "missing-plugin", "Dons-QOL.dll");

        Assert.Equal(
            "BST2004: Skipped: Plugin assembly '" + plugin + "' does not exist. Build BaseStorage.sln -c Release first.",
            Decide(folder, plugin, needsPlugin: true, fileExists: p => p == GameDllOf(folder)));
    }

    // GW-U-102
    [Fact]
    public void MissingPluginIsIgnoredWhenTheTestDoesNotNeedIt()
    {
        string folder = TempFolder("NoPluginNeeded");

        Assert.Null(Decide(
            folder,
            Path.Combine(Path.GetTempPath(), "missing-plugin", "Dons-QOL.dll"),
            needsPlugin: false,
            fileExists: p => p == GameDllOf(folder)));
    }

    // GW-U-102
    [Fact]
    public void AllPresentGivesNull()
    {
        string folder = TempFolder("AllPresent");
        string plugin = Path.Combine(TempFolder("PluginFolder"), "Dons-QOL.dll");

        Assert.Null(Decide(folder, plugin, needsPlugin: true, fileExists: p => p == GameDllOf(folder) || p == plugin));
    }

    // GW-U-102
    [Fact]
    public void MissingFolderIsReportedBeforeAMissingPlugin()
    {
        string missing = Path.Combine(Path.GetTempPath(), "NoValheim-" + Guid.NewGuid().ToString("N"));

        Assert.Equal(
            "BST2002: Skipped: ValheimDir '" + missing + "' does not exist." + Tail,
            Decide(missing, Path.Combine(Path.GetTempPath(), "missing-plugin", "Dons-QOL.dll"), needsPlugin: true));
    }

    // GW-U-102
    [Fact]
    public void TrailingSeparatorIsUsedVerbatim()
    {
        string folder = TempFolder("TrailingValheim") + @"\";

        string? message = Decide(folder, pluginDll: @"C:\p\Dons-QOL.dll", needsPlugin: false);

        Assert.Equal(
            "BST2003: Skipped: '" + folder + @"\valheim_Data\Managed\assembly_valheim.dll' does not exist." + Tail,
            message);
        Assert.Contains(@"\\valheim_Data", message);
    }

    // GW-U-103
    [Fact]
    public void TierAttributesCarryTheSkipDecisionForTheirNeedsPlugin()
    {
        string? valheimDecision = SkipGate.ForBuild(false);
        string? pluginDecision = SkipGate.ForBuild(true);

        Assert.Equal(valheimDecision, new RequiresValheimFactAttribute().Skip);
        Assert.Equal(valheimDecision, new RequiresValheimTheoryAttribute().Skip);
        Assert.Equal(pluginDecision, new RequiresPluginFactAttribute().Skip);
    }

    // GW-U-103
    [Fact]
    public void EveryTestMethodCarriesTheSkipAttributeItsTierRequires()
    {
        List<string> problems = [];
        List<string> plainTests = [];

        foreach (Type type in typeof(SkipGateTests).Assembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic && t.Namespace == "BaseStorage.PatchTargets.Tests"))
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                object[] attributes = method.GetCustomAttributes(inherit: true);
                if (!attributes.Any(a => a is FactAttribute))
                {
                    continue;
                }

                string name = type.Name + "." + method.Name;
                if (type == typeof(GameTargetContractTests) || type == typeof(KnownGoodTargetTests))
                {
                    if (!attributes.Any(a => a is RequiresValheimFactAttribute || a is RequiresValheimTheoryAttribute))
                    {
                        problems.Add(name + " must carry RequiresValheim*.");
                    }
                }
                else if (type == typeof(HarmonyAgreementTests))
                {
                    if (!attributes.Any(a => a is RequiresPluginFactAttribute))
                    {
                        problems.Add(name + " must carry RequiresPluginFact.");
                    }
                }
                else if (type == typeof(ToggleGatePluginTests))
                {
                    // Edited by S-E003-008, craft-from-base-toggle 8.3: ToggleGatePluginTests (GW-U-1820) is on the RequiresPluginFact allow-list.
                    if (!attributes.Any(a => a is RequiresPluginFactAttribute))
                    {
                        problems.Add(name + " must carry RequiresPluginFact.");
                    }
                }
                else
                {
                    bool plain = attributes.Any(a => a.GetType() == typeof(FactAttribute) || a.GetType() == typeof(TheoryAttribute));
                    if (!plain)
                    {
                        problems.Add(name + " must use a plain Fact or Theory.");
                        continue;
                    }

                    plainTests.Add(name);
                    foreach (FactAttribute fact in attributes.OfType<FactAttribute>())
                    {
                        if (fact.Skip is not null)
                        {
                            problems.Add(name + " sets a literal Skip.");
                        }
                    }
                }
            }
        }

        Assert.Empty(problems);
        Assert.Contains("PatchTargetsSmokeTests.AssemblyIsNamedBaseStoragePatchTargetsTests", plainTests);
        Assert.Contains("PatchTargetNamesTests.StarterSetHoldsTheContractedValues", plainTests);
        Assert.Contains("PatchTargetNamesTests.TheFileIsCompiledIntoThisAssemblyAndNotReferenced", plainTests);
    }

    /// <summary>Removes the temp folders that this instance made.</summary>
    public void Dispose()
    {
        foreach (string folder in folders)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    private string TempFolder(string name)
    {
        string folder = Path.Combine(Path.GetTempPath(), "bst-skip-" + Guid.NewGuid().ToString("N"), name);
        Directory.CreateDirectory(folder);
        folders.Add(Path.GetDirectoryName(folder)!);
        return folder;
    }

    private static string GameDllOf(string folder) => folder + @"\valheim_Data\Managed\assembly_valheim.dll";

    /// <summary>The file check is injected, so no placeholder file has to be written to disk.</summary>
    private static string? Decide(string valheimDir, string pluginDll, bool needsPlugin, Func<string, bool>? fileExists = null)
    {
        Func<string, bool> exists = fileExists ?? File.Exists;
        return SkipGate.Decide(
            valheimDir,
            GameDllOf(valheimDir),
            pluginDll,
            "Release",
            needsPlugin,
            Directory.Exists,
            exists);
    }
}
