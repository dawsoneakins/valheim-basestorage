namespace HarmonyLib;

using System;

/// <summary>
/// Test-only stand-in for the Harmony attribute (ADR-GWIN-015). It declares the 7 supported constructor shapes plus the
/// unsupported <c>(Type, MethodType)</c> one. No Harmony package is referenced.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class HarmonyPatch : Attribute
{
    public HarmonyPatch()
    {
    }

    public HarmonyPatch(Type declaringType)
    {
    }

    public HarmonyPatch(Type declaringType, string methodName)
    {
    }

    public HarmonyPatch(Type declaringType, string methodName, params Type[] argumentTypes)
    {
    }

    public HarmonyPatch(string methodName)
    {
    }

    public HarmonyPatch(string methodName, params Type[] argumentTypes)
    {
    }

    public HarmonyPatch(Type[] argumentTypes)
    {
    }

    public HarmonyPatch(Type declaringType, MethodType methodType)
    {
    }
}

/// <summary>Marks a method that returns the target method. Unsupported by the check (BST2022).</summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class HarmonyTargetMethod : Attribute
{
}

/// <summary>The method kind argument of the unsupported constructor.</summary>
internal enum MethodType
{
    Normal,
    Getter,
    Setter,
    Constructor,
    StaticConstructor,
}
