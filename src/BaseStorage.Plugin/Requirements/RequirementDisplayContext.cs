namespace BaseStorage.Plugin.Requirements;

/// <summary>Which requirement display, if any, is being set up.</summary>
internal enum RequirementDisplayContext
{
    /// <summary>No requirement display is being set up; the requirement stays vanilla.</summary>
    None = 0,

    /// <summary>Inside the crafting panel's requirement list.</summary>
    CraftingPanel = 1,

    /// <summary>Inside the build tooltip for a non-null piece.</summary>
    BuildTooltip = 2,
}
