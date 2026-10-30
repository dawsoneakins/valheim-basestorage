namespace BaseStorage.Plugin.Boot;

using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

/// <summary>Finds the Harmony patch classes of an assembly.</summary>
internal static class PatchDiscovery
{
    /// <summary>Returns the classes that declare HarmonyPatch directly, ordered by full name (ordinal). Static classes are kept.</summary>
    internal static Type[] FindPatchClasses(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(type => type.IsClass && !(type.IsAbstract && !type.IsSealed))
            .Where(type => type.IsDefined(typeof(HarmonyPatch), inherit: false))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }
}
