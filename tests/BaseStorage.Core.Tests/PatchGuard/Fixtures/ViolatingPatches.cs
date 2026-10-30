namespace BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Tests.PatchGuard.Fixtures.FakeHarmony;
using BaseStorage.Plugin.Patching;

// One violation per fixture (GW-U-141). Never executed, only scanned.

/// <summary>G1: a patch that never calls the helper.</summary>
[HarmonyPatch]
public static class NoHelperPatch
{
    public static void Postfix(ref string __result)
    {
        __result = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating.NoHelperPatch.Postfix";
    }
}

/// <summary>G1 through the attribute: a HarmonyPostfix that never calls the helper.</summary>
public static class AttributeNoHelperPatch
{
    [HarmonyPostfix]
    public static void AfterUpdate(ref string __result)
    {
        __result = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating.AttributeNoHelperPatch.AfterUpdate";
    }
}

/// <summary>G1: a transpiler that uses Run instead of RunTranspiler.</summary>
[HarmonyPatch]
public static class TranspilerUsesRunPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating.TranspilerUsesRunPatch.Transpiler";

    public static IEnumerable<string> Transpiler(IEnumerable<string> instructions)
    {
        return PatchSafety.Run<IEnumerable<string>>(Key, instructions, sequence => sequence);
    }
}

/// <summary>G2: calls ToString in its own IL, before the helper.</summary>
[HarmonyPatch]
public static class CallOutsidePatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating.CallOutsidePatch.Prefix";

    public static bool Prefix(object __instance)
    {
        _ = __instance.ToString();
        return PatchSafety.RunPrefix(Key, () => true);
    }
}

/// <summary>G2: its own try and catch around the helper call.</summary>
[HarmonyPatch]
public static class OwnTryCatchPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating.OwnTryCatchPatch.Postfix";

    public static void Postfix()
    {
        try
        {
            PatchSafety.Run(Key, () => { });
        }
        catch (Exception)
        {
        }
    }
}

/// <summary>A user static, read in the patch's own IL.</summary>
public static class StaticHolder
{
    public static readonly int Value = 1;
}

/// <summary>G2: reads a user static field before the helper.</summary>
[HarmonyPatch]
public static class StaticReadPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating.StaticReadPatch.Prefix";

    public static bool Prefix()
    {
        _ = StaticHolder.Value;
        return PatchSafety.RunPrefix(Key, () => true);
    }
}

/// <summary>G3: calls the helper with a key that names another patch.</summary>
[HarmonyPatch]
public static class WrongKeyPatch
{
    public static void Postfix(ref string __result)
    {
        __result = PatchSafety.Run("BaseStorage.Core.Tests.PatchGuard.Fixtures.Violating.SomeOtherPatch.Postfix", __result, value => value);
    }
}
