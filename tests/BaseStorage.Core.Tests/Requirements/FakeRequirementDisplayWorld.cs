namespace BaseStorage.Core.Tests.Requirements;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Availability;
using BaseStorage.Plugin.Requirements;

/// <summary>
/// Test double for <see cref="IRequirementDisplayWorld"/> (S-E003-005). Returns a configurable snapshot (default fixture D from both
/// port methods), records every call with its arguments, and can return null or throw <see cref="InvalidOperationException"/>. Not a test class.
/// </summary>
internal sealed class FakeRequirementDisplayWorld : IRequirementDisplayWorld
{
    private readonly List<string> calls = new List<string>();

    /// <summary>Creates a fake that returns fixture D.</summary>
    internal FakeRequirementDisplayWorld()
        : this(RequirementDisplayFixtures.D())
    {
    }

    /// <summary>Creates a fake that returns the given snapshot (or null when it is null).</summary>
    /// <param name="snapshot">The snapshot both port methods return.</param>
    internal FakeRequirementDisplayWorld(BaseSnapshot? snapshot)
    {
        Snapshot = snapshot;
    }

    /// <summary>The snapshot both port methods return unless a flag says otherwise.</summary>
    internal BaseSnapshot? Snapshot { get; set; }

    /// <summary>When true, both port methods return null.</summary>
    internal bool ReturnsNull { get; set; }

    /// <summary>When true, <see cref="ReadCraftingPanel"/> throws <see cref="InvalidOperationException"/> after it is recorded.</summary>
    internal bool ThrowsFromCraftingPanel { get; set; }

    /// <summary>When true, <see cref="ReadBuildTooltip"/> throws <see cref="InvalidOperationException"/> after it is recorded.</summary>
    internal bool ThrowsFromBuildTooltip { get; set; }

    /// <summary>Every call in order, as text: <c>ReadCraftingPanel(q, m)</c> or <c>ReadBuildTooltip()</c>.</summary>
    internal IReadOnlyList<string> Calls => calls;

    /// <summary>Records the call and returns the snapshot, or throws when configured to.</summary>
    /// <param name="qualityLevel">The quality level the session forwarded.</param>
    /// <param name="multiplier">The craft multiplier the session forwarded.</param>
    /// <returns>The configured snapshot, or null.</returns>
    public BaseSnapshot? ReadCraftingPanel(int qualityLevel, int multiplier)
    {
        calls.Add("ReadCraftingPanel(" + qualityLevel + ", " + multiplier + ")");
        if (ThrowsFromCraftingPanel)
        {
            throw new InvalidOperationException("fake crafting panel failure");
        }

        return ReturnsNull ? null : Snapshot;
    }

    /// <summary>Records the call and returns the snapshot, or throws when configured to.</summary>
    /// <returns>The configured snapshot, or null.</returns>
    public BaseSnapshot? ReadBuildTooltip()
    {
        calls.Add("ReadBuildTooltip()");
        if (ThrowsFromBuildTooltip)
        {
            throw new InvalidOperationException("fake build tooltip failure");
        }

        return ReturnsNull ? null : Snapshot;
    }
}
