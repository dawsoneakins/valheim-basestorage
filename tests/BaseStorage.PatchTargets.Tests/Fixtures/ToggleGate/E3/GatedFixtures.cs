namespace BaseStorage.PatchTargets.Tests.Fixtures.ToggleGate.E3;

using HarmonyLib;

/// <summary>Shape A (contract 4.1) over the fixture gate: passes the checker.</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class GatedBool
{
    private static bool Prefix(object instance) => !FixtureGate.IsOpen() || PrefixCore(instance);

    private static bool PrefixCore(object instance) => FixtureWork.Touch();
}

/// <summary>Shape B (contract 4.2), a bool prefix with ref __result: passes the checker.</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class GatedRef
{
    private static bool Prefix(object instance, ref bool __result) =>
        !FixtureGate.IsOpen() || PrefixCore(instance, ref __result);

    private static bool PrefixCore(object instance, ref bool result)
    {
        result = FixtureWork.Touch();
        return false;
    }
}

/// <summary>Shape C (contract 4.3), a void prefix and a void postfix: passes the checker.</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class GatedVoidPair
{
    private static void Prefix(int quality)
    {
        if (FixtureGate.IsOpen())
        {
            PrefixCore(quality);
        }
    }

    private static void PrefixCore(int quality) => FixtureWork.Touch();

    private static void Postfix()
    {
        if (FixtureGate.IsOpen())
        {
            PostfixCore();
        }
    }

    private static void PostfixCore() => FixtureWork.Run(() => true);
}

/// <summary>Shape D (contract 4.4), a postfix with ref __result: passes the checker.</summary>
[HarmonyPatch]
[FixtureE3Patch]
internal static class GatedRefPostfix
{
    private static void Postfix(ref bool __result)
    {
        if (FixtureGate.IsOpen())
        {
            PostfixCore(ref __result);
        }
    }

    private static void PostfixCore(ref bool result) => result = FixtureWork.Touch();
}
