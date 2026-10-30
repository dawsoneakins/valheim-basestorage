namespace BaseStorage.PatchTargets.Tests.Fixtures.ToggleGate.Elsewhere;

using HarmonyLib;

/// <summary>Marked, outside the fixture E3 namespace, and gated: found by the marker (D2), passes.</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class MarkedGated
{
    private static bool Prefix(object instance) => !FixtureGate.IsOpen() || PrefixCore(instance);

    private static bool PrefixCore(object instance) => FixtureWork.Touch();
}

/// <summary>Marked but without HarmonyPatch: BST2034 only.</summary>
[FixtureE3Patch]
internal static class MarkerOnly
{
    private static void Noop()
    {
    }
}

/// <summary>A HarmonyPatch class that is neither marked nor in an E3 namespace, and ungated: ignored.</summary>
[HarmonyPatch]
internal static class NotE3
{
    private static bool Prefix(object instance) => FixtureWork.Touch();
}
