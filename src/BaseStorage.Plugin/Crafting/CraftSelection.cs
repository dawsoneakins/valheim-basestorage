namespace BaseStorage.Plugin.Crafting;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// The crafting selection of the game at one moment (contract craft-from-base 3.2). The recipe, upgrade item and station are
/// opaque references; equality compares them by reference only, so no engine equality override can interfere.
/// </summary>
internal readonly struct CraftSelection : IEquatable<CraftSelection>
{
    /// <summary>Creates a selection.</summary>
    /// <param name="recipe">The recipe instance; not null.</param>
    /// <param name="upgradeItem">The item being upgraded, or null for a new item.</param>
    /// <param name="station">The crafting station instance; not null.</param>
    /// <param name="qualityLevel">The quality level to craft; at least 1.</param>
    /// <param name="multiplier">The craft multiplier; at least 1.</param>
    internal CraftSelection(object recipe, object? upgradeItem, object station, int qualityLevel, int multiplier)
    {
        if (recipe is null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        if (station is null)
        {
            throw new ArgumentNullException(nameof(station));
        }

        if (qualityLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(qualityLevel));
        }

        if (multiplier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }

        Recipe = recipe;
        UpgradeItem = upgradeItem;
        Station = station;
        QualityLevel = qualityLevel;
        Multiplier = multiplier;
    }

    /// <summary>Gets the recipe instance.</summary>
    internal object Recipe { get; }

    /// <summary>Gets the item being upgraded, or null for a new item.</summary>
    internal object? UpgradeItem { get; }

    /// <summary>Gets the crafting station instance.</summary>
    internal object Station { get; }

    /// <summary>Gets the quality level.</summary>
    internal int QualityLevel { get; }

    /// <summary>Gets the craft multiplier.</summary>
    internal int Multiplier { get; }

    /// <summary>Tells whether two selections are the same.</summary>
    /// <param name="left">The first selection.</param>
    /// <param name="right">The second selection.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(CraftSelection left, CraftSelection right) => left.Equals(right);

    /// <summary>Tells whether two selections differ.</summary>
    /// <param name="left">The first selection.</param>
    /// <param name="right">The second selection.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(CraftSelection left, CraftSelection right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(CraftSelection other) =>
        ReferenceEquals(Recipe, other.Recipe)
        && ReferenceEquals(UpgradeItem, other.UpgradeItem)
        && ReferenceEquals(Station, other.Station)
        && QualityLevel == other.QualityLevel
        && Multiplier == other.Multiplier;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CraftSelection other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Recipe is null ? 0 : RuntimeHelpers.GetHashCode(Recipe);
            hash = (hash * 31) + (UpgradeItem is null ? 0 : RuntimeHelpers.GetHashCode(UpgradeItem));
            hash = (hash * 31) + (Station is null ? 0 : RuntimeHelpers.GetHashCode(Station));
            hash = (hash * 31) + QualityLevel;
            return (hash * 31) + Multiplier;
        }
    }
}
