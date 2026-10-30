// Offline Harmony scenarios (contract 4.7.2, 4.7.4). Each namespace is one scenario, read by HarmonyReaderTests.
// They use the HarmonyLib shim, so the metadata is the same shape that the real Plugin would produce.

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.ClassLevel
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class RecipePatch
    {
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.ClassPlusMethod
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Player))]
    internal static class MergedPostfix
    {
        [HarmonyPatch("HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
        public static void Postfix()
        {
        }
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.TwoMethods
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Container))]
    internal static class TwoTargets
    {
        [HarmonyPatch("RPC_RequestOpen", typeof(long), typeof(long))]
        public static void Open()
        {
        }

        [HarmonyPatch("RPC_StackResponse", typeof(long), typeof(bool))]
        public static void Stack()
        {
        }
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.OverrideLater
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Container), "RPC_RequestOpen")]
    internal static class LaterValuesWin
    {
        [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
        public static void Postfix()
        {
        }
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.ArrayAndString
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Player), "HaveRequirements")]
    internal static class ArrayShape
    {
        [HarmonyPatch(new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
        public static void Postfix()
        {
        }
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.NoArgs
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Container), "RPC_RequestOpen")]
    internal static class ImplicitArgs
    {
    }

    [HarmonyPatch(typeof(Container))]
    internal static class StringShape
    {
        [HarmonyPatch("RPC_OpenResponse")]
        public static void Postfix()
        {
        }
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.PrefixPostfix
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class PrefixPatch
    {
    }

    [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class PostfixPatch
    {
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.Classification
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Container), "RPC_RequestOpen", typeof(long), typeof(long))]
    internal abstract class AbstractBasePatch
    {
    }

    internal class DerivedPatch : AbstractBasePatch
    {
    }

    [HarmonyPatch(typeof(Container), "RPC_RequestStack", typeof(long), typeof(long))]
    internal static class StaticPatch
    {
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.Nested
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Player))]
    internal static class NestedArgs
    {
        [HarmonyPatch("HaveRequirements", typeof(Piece), typeof(Player.RequirementMode))]
        public static void Postfix()
        {
        }
    }

    [HarmonyPatch(typeof(Container), "RPC_RequestOpen", typeof(long[]), typeof(long))]
    internal static class ArrayArgs
    {
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.Unsupported
{
    using System.Reflection;
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Player), MethodType.Getter)]
    internal static class MethodTypeCtor
    {
    }

    [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class TargetMethodDeclared
    {
        public static MethodBase TargetMethod() => null!;
    }

    [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class TargetMethodAttributeDeclared
    {
        [HarmonyTargetMethod]
        public static MethodBase Pick() => null!;
    }

    [HarmonyPatch("HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class NoDeclaringType
    {
    }

    [HarmonyPatch(typeof(Player))]
    internal static class NoMethodName
    {
    }

    [HarmonyPatch(typeof(Player), "HaveRequirements")]
    internal static class OverloadedWithoutArgs
    {
    }
}

namespace BaseStorage.PatchTargets.Tests.Fixtures.Patches.OutsideGame
{
    using BaseStorage.PatchTargets.Tests.Fixtures.Game;
    using HarmonyLib;

    [HarmonyPatch(typeof(Player), "HaveRequirements", typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
    internal static class DeclaringTypeNotGame
    {
    }
}
