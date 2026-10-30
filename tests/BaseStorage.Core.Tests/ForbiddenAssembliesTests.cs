namespace BaseStorage.Core.Tests;

using System;

/// <summary>AC-6: examples and error behaviour for the forbidden-assembly matcher.</summary>
public sealed class ForbiddenAssembliesTests
{
    // GW-U-009
    [Theory]
    [InlineData("UnityEngine", "Unity")]
    [InlineData("UnityEngine.CoreModule", "Unity")]
    [InlineData("UnityEngine.UI", "Unity")]
    [InlineData("UnityEngine.PhysicsModule", "Unity")]
    [InlineData("Unity.TextMeshPro", "Unity")]
    [InlineData("unityengine", "Unity")]
    [InlineData("assembly_valheim", "Valheim")]
    [InlineData("assembly_utils", "Valheim")]
    [InlineData("assembly_guiutils", "Valheim")]
    [InlineData("assembly_valheim_publicized", "Valheim")]
    [InlineData("Assembly_Valheim", "Valheim")]
    [InlineData("BepInEx", "BepInEx")]
    [InlineData("BepInEx.Core", "BepInEx")]
    [InlineData("BepInEx.Preloader", "BepInEx")]
    [InlineData("0Harmony", "Harmony")]
    [InlineData("Harmony", "Harmony")]
    [InlineData("HarmonyX", "Harmony")]
    [InlineData("harmonyx", "Harmony")]
    public void ForbiddenExamplesMapToTheirFamily(string simpleName, string family)
    {
        Assert.Equal(family, ForbiddenAssemblies.FamilyOf(simpleName));
        Assert.True(ForbiddenAssemblies.IsForbidden(simpleName));
    }

    // GW-U-010
    [Theory]
    [InlineData("netstandard")]
    [InlineData("mscorlib")]
    [InlineData("System.Runtime")]
    [InlineData("System.Collections")]
    [InlineData("System.Memory")]
    [InlineData("Microsoft.CSharp")]
    [InlineData("BaseStorage.Core")]
    [InlineData("UnitsNet")]
    [InlineData("MyUnityHelpers")]
    [InlineData("Community.Toolkit")]
    [InlineData("NotHarmony")]
    [InlineData("Assembly.Loader")]
    [InlineData("")]
    public void AllowedExamplesAreNotForbidden(string simpleName)
    {
        Assert.Null(ForbiddenAssemblies.FamilyOf(simpleName));
        Assert.False(ForbiddenAssemblies.IsForbidden(simpleName));
    }

    // GW-U-011
    [Fact]
    public void NullInputThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ForbiddenAssemblies.FamilyOf(null!));
        Assert.Throws<ArgumentNullException>(() => ForbiddenAssemblies.IsForbidden(null!));
    }

    // GW-U-011: prefix on the whole simple name, not a substring.
    [Theory]
    [InlineData("MyUnityEngine")]
    [InlineData("NotBepInEx")]
    [InlineData("MyHarmony")]
    [InlineData("MyValheimTools")]
    public void MatchingIsAPrefixMatchNotASubstringMatch(string simpleName)
    {
        Assert.Null(ForbiddenAssemblies.FamilyOf(simpleName));
    }
}
