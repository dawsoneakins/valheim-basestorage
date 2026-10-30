namespace BaseStorage.Core.Tests.PerFrame;

using System.Reflection;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// AC-3 and AC-4 (S-E009-001, ADR-GWIN-161, contract quality-audit 3.5 row P1 and 3.7): the built Plugin dll has no direct call from an
/// Update or LateUpdate to the base resolver, the cache rebuild or the planner. The dll is read with PEReader and never loaded.
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class PerFrameAuditTests
{
    private const string PluginUpdate = "BaseStorage.Plugin.BaseStoragePlugin.Update";

    // GW-U-2350 (AC-3, QA2.5 P1) and GW-U-2356 (AC-4, contract 3.7): the same fact. The audit is non-vacuous and finds no violation on the current Plugin.
    [Fact]
    public void BuiltPluginFrameMethodsCallNoResolverCacheRebuildOrPlanner()
    {
        AssemblyConfigurationAttribute? configuration = typeof(PerFrameAuditTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        Assert.NotNull(configuration);
        string path = PluginDll.EnsureBuilt(configuration.Configuration ?? string.Empty);

        PerFrameAuditResult result = PerFrameAuditScanner.Scan(path, null);

        Assert.Contains(PluginUpdate, result.AuditedMethods);
        Assert.True(result.Violations.Count == 0, PerFrameAuditScanner.FormatViolations(result));
    }
}
