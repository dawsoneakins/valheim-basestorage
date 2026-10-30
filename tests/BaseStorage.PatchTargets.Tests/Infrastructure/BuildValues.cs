namespace BaseStorage.PatchTargets.Tests.Infrastructure;

using System;
using System.Linq;
using System.Reflection;

/// <summary>
/// Build-time values that MSBuild captured as assembly metadata when this test project was built (contract 4.8.1).
/// The tests never resolve ValheimDir or the Plugin path at run time.
/// </summary>
internal static class BuildValues
{
    private const string ManagedSuffix = @"\valheim_Data\Managed";

    /// <summary>ValheimDir, verbatim from the <c>BaseStorage.ValheimDir</c> metadata item.</summary>
    public static string ValheimDir => Read("BaseStorage.ValheimDir");

    /// <summary>The build configuration, from the <c>BaseStorage.Configuration</c> metadata item.</summary>
    public static string Configuration => Read("BaseStorage.Configuration");

    /// <summary>Absolute path of the built Plugin assembly, from the <c>BaseStorage.PluginAssemblyPath</c> metadata item.</summary>
    public static string PluginAssemblyPath => Read("BaseStorage.PluginAssemblyPath");

    /// <summary>The game's Managed folder: ValheimDir + <c>\valheim_Data\Managed</c>.</summary>
    public static string ManagedDir => ValheimDir + ManagedSuffix;

    /// <summary>The game assembly: ValheimDir + <c>\valheim_Data\Managed\assembly_valheim.dll</c>.</summary>
    public static string GameDll => ManagedDir + @"\assembly_valheim.dll";

    private static string Read(string key)
    {
        AssemblyMetadataAttribute? item = typeof(BuildValues).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key);

        return item?.Value ?? throw new InvalidOperationException("Assembly metadata item '" + key + "' is missing.");
    }
}
