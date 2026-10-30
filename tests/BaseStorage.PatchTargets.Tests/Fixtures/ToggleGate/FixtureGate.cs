namespace BaseStorage.PatchTargets.Tests.Fixtures.ToggleGate;

using System;

/// <summary>The fixture gate of contract craft-from-base-toggle 7.3: a static bool that stands for CraftFromBaseGate.IsOpen.</summary>
internal static class FixtureGate
{
    /// <summary>Gets or sets the value that <see cref="IsOpen"/> returns.</summary>
    internal static bool Value { get; set; }

    /// <summary>Returns the fixture gate value.</summary>
    internal static bool IsOpen() => Value;
}

/// <summary>The fixture marker of an E3 patch class (stands for CraftFromBasePatchAttribute, contract craft-from-base-toggle 2.2).</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class FixtureE3PatchAttribute : Attribute
{
}

/// <summary>The fixture work of a patch body: Touch stands for the work and Run stands for PatchSafety (contract 7.3).</summary>
internal static class FixtureWork
{
    /// <summary>Stands for the work of a patch body.</summary>
    internal static bool Touch() => true;

    /// <summary>Stands for PatchSafety.Run: calls the body.</summary>
    internal static bool Run(Func<bool> body) => body();
}
