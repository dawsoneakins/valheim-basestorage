namespace BaseStorage.Core.Tests.PatchGuard.Fixtures.Gated;

using BaseStorage.Core.Tests.PatchGuard.Fixtures.FakeHarmony;
using BaseStorage.Plugin.Patching;

// Fixtures for the optional gate (ADR-GWIN-136, contract 8.3 errata 1). Each wrapper calls the gate first and then one same-type
// <Name>Core that does the PatchSafety work. Never executed; PatchGuardGatedFixtureTests scans them (GW-U-1825..1827).
// StandInCraftFromBaseGate stands in for the real CraftFromBaseGate: its csproj link is deferred to implement (contract 1.2),
// and linking it before implement breaks the test build (CS2001).

/// <summary>The stand-in for the gate. The scanner reads only the call to it, never runs it.</summary>
public static class StandInCraftFromBaseGate
{
    public static bool IsOpen() => true;
}

/// <summary>Shape A behind the gate over the linked PatchSafety: the compliant case.</summary>
[HarmonyPatch]
public static class GatedCompliant
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Gated.GatedCompliant.Prefix";

    public static bool Prefix() => !StandInCraftFromBaseGate.IsOpen() || PrefixCore();

    private static bool PrefixCore() => PatchSafety.RunPrefix(Key, () => Check());

    private static bool Check() => true;
}

/// <summary>The Core passes a key that is not the wrapper's own: G3 only.</summary>
[HarmonyPatch]
public static class GatedCoreWithoutKey
{
    private const string OtherKey = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Gated.Elsewhere.Prefix";

    public static bool Prefix() => !StandInCraftFromBaseGate.IsOpen() || PrefixCore();

    private static bool PrefixCore() => PatchSafety.RunPrefix(OtherKey, () => Check());

    private static bool Check() => true;
}

/// <summary>The wrapper calls a second method besides the gate and the Core: G2 only.</summary>
[HarmonyPatch]
public static class GatedExtraCall
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Gated.GatedExtraCall.Prefix";

    public static bool Prefix()
    {
        if (StandInCraftFromBaseGate.IsOpen())
        {
            _ = Extra();
            return PrefixCore();
        }

        return true;
    }

    private static bool PrefixCore() => PatchSafety.RunPrefix(Key, () => Check());

    private static bool Extra() => true;

    private static bool Check() => true;
}

/// <summary>The wrapper calls the gate and never calls a Core: G1 only.</summary>
[HarmonyPatch]
public static class GatedNoCore
{
    public static bool Prefix()
    {
        if (StandInCraftFromBaseGate.IsOpen())
        {
            return false;
        }

        return true;
    }
}
