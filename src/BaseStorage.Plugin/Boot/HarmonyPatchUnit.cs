namespace BaseStorage.Plugin.Boot;

using System;
using BaseStorage.Core.Patching;
using HarmonyLib;

/// <summary>Wraps one Harmony patch class as an independently applicable patch. Exceptions propagate to PatchApplier.</summary>
internal sealed class HarmonyPatchUnit : IPatchUnit
{
    private readonly Harmony harmony;
    private readonly Type patchType;

    internal HarmonyPatchUnit(Harmony harmony, Type patchType)
    {
        this.harmony = harmony;
        this.patchType = patchType;
    }

    /// <inheritdoc />
    public string Name => patchType.FullName ?? patchType.Name;

    /// <inheritdoc />
    public void Apply()
    {
        harmony.CreateClassProcessor(patchType).Patch();
    }
}
