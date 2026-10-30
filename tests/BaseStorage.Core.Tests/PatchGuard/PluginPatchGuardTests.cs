namespace BaseStorage.Core.Tests.PatchGuard;

using System.Collections.Generic;
using System.Reflection;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// AC-8 (ADR-GWIN-021): the built Plugin dll defines the helper with Run, RunPrefix and RunTranspiler, and the guard finds no
/// violation in it. Zero patch bodies is a pass (spec A-4). The dll is read with PEReader and never loaded.
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class PluginPatchGuardTests
{
    private const string Helper = "BaseStorage.Plugin.Patching.PatchSafety";

    // GW-I-120
    [Fact]
    public void BuiltPluginDefinesTheHelperAndPassesTheGuard()
    {
        AssemblyConfigurationAttribute? configuration = typeof(PluginPatchGuardTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        Assert.NotNull(configuration);
        string path = PluginDll.EnsureBuilt(configuration.Configuration ?? string.Empty);

        IReadOnlyList<string> methods = PatchGuardScanner.MethodNamesOf(path, Helper);
        Assert.Contains("Run", methods);
        Assert.Contains("RunPrefix", methods);
        Assert.Contains("RunTranspiler", methods);

        // Edited by S-E003-008, craft-from-base-toggle 8.3 amendment (ADR-GWIN-136)
        PatchGuardResult result = PatchGuardScanner.Scan(path, new PatchGuardRules("HarmonyLib", Helper, GateMethodFullName: "BaseStorage.Plugin.Patching.CraftFromBaseGate.IsOpen"));
        Assert.True(result.Violations.Count == 0, PatchGuardScanner.FormatViolations(result));
    }
}
