namespace BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Tests.PatchGuard.Fixtures.FakeHarmony;
using BaseStorage.Plugin.Patching;

// Fixtures that follow the ADR-GWIN-020 idiom: each patch method is one call into the real linked PatchSafety.
// Never executed; GW-U-139, GW-U-140 and GW-U-143 scan them.

/// <summary>Void postfix with a closure over the instance.</summary>
[HarmonyPatch]
public static class VoidPostfixPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.VoidPostfixPatch.Postfix";

    public static void Postfix(object __instance)
    {
        PatchSafety.Run(Key, () => Body(__instance));
    }

    private static void Body(object instance)
    {
        _ = instance.GetHashCode();
    }
}

/// <summary>Ref __result postfix through Run of T.</summary>
[HarmonyPatch]
public static class RefResultPostfixPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.RefResultPostfixPatch.Postfix";

    public static void Postfix(ref bool __result)
    {
        __result = PatchSafety.Run(Key, __result, value => !value);
    }
}

/// <summary>Boolean prefix through RunPrefix.</summary>
[HarmonyPatch]
public static class BoolPrefixPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.BoolPrefixPatch.Prefix";

    public static bool Prefix()
    {
        return PatchSafety.RunPrefix(Key, () => Check());
    }

    private static bool Check() => true;
}

/// <summary>Ref __result prefix through RunPrefix of T.</summary>
[HarmonyPatch]
public static class RefResultPrefixPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.RefResultPrefixPatch.Prefix";

    public static bool Prefix(ref int __result)
    {
        return PatchSafety.RunPrefix(Key, ref __result, (ref int value) => { value = 7; return false; });
    }
}

/// <summary>Transpiler through RunTranspiler.</summary>
[HarmonyPatch]
public static class TranspilerPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.TranspilerPatch.Transpiler";

    public static IEnumerable<string> Transpiler(IEnumerable<string> instructions)
    {
        return PatchSafety.RunTranspiler(Key, instructions, sequence => sequence);
    }
}

/// <summary>Finalizer through Run of an exception.</summary>
[HarmonyPatch]
public static class FinalizerPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.FinalizerPatch.Finalizer";

    public static Exception? Finalizer(Exception? __exception)
    {
        return PatchSafety.Run<Exception?>(Key, __exception, error => error);
    }
}

/// <summary>ILManipulator through Run with an Action.</summary>
[HarmonyPatch]
public static class ILManipulatorPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.ILManipulatorPatch.ILManipulator";

    public static void ILManipulator(object il)
    {
        PatchSafety.Run(Key, () => { });
    }
}

/// <summary>A prefix found by its attribute, with a name that no convention would pick.</summary>
public static class OddNamePatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.OddNamePatch.Gatekeeper";

    [HarmonyPrefix]
    public static bool Gatekeeper()
    {
        return PatchSafety.RunPrefix(Key, () => true);
    }
}

/// <summary>A postfix found through a method-level HarmonyPatch attribute.</summary>
public static class MethodLevelPatch
{
    private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.MethodLevelPatch.Postfix";

    [HarmonyPatch]
    public static void Postfix()
    {
        PatchSafety.Run(Key, () => { });
    }
}

/// <summary>A nested patch class.</summary>
public static class Outer
{
    [HarmonyPatch]
    public static class InnerPatch
    {
        private const string Key = "BaseStorage.Core.Tests.PatchGuard.Fixtures.Compliant.Outer+InnerPatch.Postfix";

        public static void Postfix()
        {
            PatchSafety.Run(Key, () => { });
        }
    }
}

/// <summary>Excluded lifecycle and reverse-patch methods, with plain bodies.</summary>
[HarmonyPatch]
public static class LifecyclePatch
{
    public static bool Prepare() => true;

    public static System.Reflection.MethodBase? TargetMethod() => null;

    [HarmonyReversePatch]
    public static void Postfix()
    {
    }
}

/// <summary>Not a patch: no Harmony attribute.</summary>
public static class NotAPatch
{
    public static void Postfix()
    {
    }
}
