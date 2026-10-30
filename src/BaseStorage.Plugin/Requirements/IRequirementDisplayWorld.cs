namespace BaseStorage.Plugin.Requirements;

using BaseStorage.Core.Availability;

/// <summary>The game reads the requirement display needs; both return null instead of throwing for game data.</summary>
internal interface IRequirementDisplayWorld
{
    /// <summary>Reads the snapshot when the craft-from-base gate is open for the panel's selected recipe.</summary>
    /// <param name="qualityLevel">The quality level.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>The snapshot, or null.</returns>
    BaseSnapshot? ReadCraftingPanel(int qualityLevel, int multiplier);

    /// <summary>Reads the snapshot of the base containing the local player.</summary>
    /// <returns>The snapshot, or null.</returns>
    BaseSnapshot? ReadBuildTooltip();
}
