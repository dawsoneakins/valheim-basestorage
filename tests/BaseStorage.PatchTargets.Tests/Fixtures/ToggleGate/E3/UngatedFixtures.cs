namespace BaseStorage.PatchTargets.Tests.Fixtures.ToggleGate.E3;

using HarmonyLib;

/// <summary>Marked, and its prefix does work before any gate: BST2030 (found: call FixtureWork::Touch).</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class Ungated
{
    private static bool Prefix(object instance) => FixtureWork.Touch();
}

/// <summary>Marked, and the closure is created before the gate: BST2030 (found: newobj of the display class).</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class CapturingGate
{
    private static bool Prefix(object instance) =>
        !FixtureGate.IsOpen() || FixtureWork.Run(() => instance != null);
}

/// <summary>Marked, and the work runs before the gate call: BST2030 (found: call FixtureWork::Touch).</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class LateGate
{
    private static bool Prefix(object instance)
    {
        FixtureWork.Touch();
        return !FixtureGate.IsOpen();
    }
}

/// <summary>Marked, and the gate result is discarded: BST2031.</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class DiscardedGate
{
    private static bool Prefix(object instance)
    {
        FixtureGate.IsOpen();
        return FixtureWork.Touch();
    }
}

/// <summary>In an E3 namespace but not marked: BST2032 only (its shape is correct).</summary>
[HarmonyPatch]
internal static class Unmarked
{
    private static bool Prefix(object instance) => !FixtureGate.IsOpen() || PrefixCore(instance);

    private static bool PrefixCore(object instance) => FixtureWork.Touch();
}

/// <summary>Marked, with only a Transpiler: BST2033 (found: Transpiler).</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class TranspilerOnly
{
    private static int Transpiler(int instruction) => instruction;
}
