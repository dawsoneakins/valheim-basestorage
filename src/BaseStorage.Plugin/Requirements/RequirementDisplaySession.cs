namespace BaseStorage.Plugin.Requirements;

using System;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Ui;

/// <summary>Holds the display context and snapshot of the caller that is setting up requirements, and resolves each line.</summary>
internal sealed class RequirementDisplaySession
{
    private readonly IRequirementDisplayWorld world;

    /// <summary>Creates a session.</summary>
    /// <param name="world">The world port.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    internal RequirementDisplaySession(IRequirementDisplayWorld world)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        this.world = world;
    }

    /// <summary>Gets the current context.</summary>
    internal RequirementDisplayContext Context { get; private set; }

    /// <summary>Gets the snapshot, null whenever the context is none.</summary>
    internal BaseSnapshot? Snapshot { get; private set; }

    /// <summary>Enters the crafting panel context.</summary>
    /// <param name="qualityLevel">The quality level.</param>
    /// <param name="multiplier">The multiplier.</param>
    internal void EnterCraftingPanel(int qualityLevel, int multiplier)
    {
        Context = RequirementDisplayContext.CraftingPanel;
        Snapshot = null;
        if (qualityLevel >= 1 && multiplier >= 1)
        {
            Snapshot = world.ReadCraftingPanel(qualityLevel, multiplier);
        }
    }

    /// <summary>Enters the build tooltip context.</summary>
    internal void EnterBuildTooltip()
    {
        Context = RequirementDisplayContext.BuildTooltip;
        Snapshot = null;
        Snapshot = world.ReadBuildTooltip();
    }

    /// <summary>Leaves a context; a no-op unless it is the current one.</summary>
    /// <param name="context">The context being left.</param>
    internal void Exit(RequirementDisplayContext context)
    {
        if (context != RequirementDisplayContext.None && context == Context)
        {
            Context = RequirementDisplayContext.None;
            Snapshot = null;
        }
    }

    /// <summary>Resolves the display of one requirement line.</summary>
    /// <param name="prefabName">The item prefab name.</param>
    /// <param name="need">The needed amount.</param>
    /// <param name="vanillaText">The text vanilla wrote.</param>
    /// <returns>The display, or null to leave vanilla untouched.</returns>
    internal RequirementDisplay? Resolve(string? prefabName, long need, string? vanillaText)
    {
        BaseSnapshot? snapshot = Snapshot;
        if (Context == RequirementDisplayContext.None
            || snapshot == null
            || string.IsNullOrWhiteSpace(prefabName)
            || need < 1
            || need > int.MaxValue
            || vanillaText == null)
        {
            return null;
        }

        RequirementAvailability a = BaseAvailability.Compute(snapshot, new[] { new CraftRequirement(prefabName!, (int)need) }).Requirements[0];
        return RequirementDisplayFormatter.Format(a.InInventory, a.InContainers, (int)need, vanillaText);
    }
}
