namespace BaseStorage.Plugin.Patching;

using System;

/// <summary>
/// Marks a Harmony patch class that belongs to craft-from-base (E3). Every patch method of a marked class checks
/// <see cref="CraftFromBaseGate.IsOpen"/> as its first instruction (contract craft-from-base-toggle T1, T7).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class CraftFromBasePatchAttribute : Attribute
{
}
