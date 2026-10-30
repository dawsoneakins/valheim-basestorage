namespace BaseStorage.Core.Tests.PatchGuard.Fixtures.FakeHarmony;

using System;

// Plain stand-ins for the HarmonyLib attributes (ADR-GWIN-021). The guard matches by namespace plus name, so only the
// AttributeNamespace rule differs between the fixture run and the real Plugin run. Never executed, only scanned.

/// <summary>Stand-in for HarmonyLib.HarmonyPatch.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyPatch : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyPrefix.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyPrefix : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyPostfix.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyPostfix : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyTranspiler.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyTranspiler : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyFinalizer.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyFinalizer : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyILManipulator.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyILManipulator : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyPrepare.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyPrepare : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyTargetMethod.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyTargetMethod : Attribute
{
}

/// <summary>Stand-in for HarmonyLib.HarmonyReversePatch.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class HarmonyReversePatch : Attribute
{
}
