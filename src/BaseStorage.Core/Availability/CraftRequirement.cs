namespace BaseStorage.Core.Availability;

using System;

/// <summary>One line of a recipe or piece cost: this many of this item, at this quality rule (contract section 9.2).</summary>
public sealed class CraftRequirement : IEquatable<CraftRequirement>
{
    /// <summary>Creates a requirement that counts every quality (<see cref="QualityMatch.Any"/>, quality 1).</summary>
    /// <param name="prefabName">The item's prefab name, matched ordinally; must not be null, empty or whitespace.</param>
    /// <param name="amount">Total items needed; 0 is allowed and is always met.</param>
    /// <exception cref="ArgumentNullException"><paramref name="prefabName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="prefabName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
    public CraftRequirement(string prefabName, int amount)
        : this(prefabName, amount, QualityMatch.Any, 1)
    {
    }

    /// <summary>Creates a requirement with an explicit quality rule.</summary>
    /// <param name="prefabName">The item's prefab name, matched ordinally; must not be null, empty or whitespace.</param>
    /// <param name="amount">Total items needed; 0 is allowed and is always met.</param>
    /// <param name="qualityMatch">How the quality of a stack is compared; must be a defined value.</param>
    /// <param name="quality">The quality compared against; at least 1, and exactly 1 when <paramref name="qualityMatch"/> is Any.</param>
    /// <exception cref="ArgumentNullException"><paramref name="prefabName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="prefabName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="amount"/> is negative, <paramref name="qualityMatch"/> is not defined, or <paramref name="quality"/> is invalid.
    /// </exception>
    public CraftRequirement(string prefabName, int amount, QualityMatch qualityMatch, int quality)
    {
        RequirePrefabName(prefabName);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Must be at least 0.");
        }

        if (!Enum.IsDefined(typeof(QualityMatch), qualityMatch))
        {
            throw new ArgumentOutOfRangeException(nameof(qualityMatch), "Must be a defined value.");
        }

        if (quality < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Must be at least 1.");
        }

        if (qualityMatch == QualityMatch.Any && quality != 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Must be 1 when the quality match is Any.");
        }

        PrefabName = prefabName;
        Amount = amount;
        QualityMatch = qualityMatch;
        Quality = quality;
    }

    /// <summary>Gets the vanilla amount for a craft or upgrade at a target quality level, repeated for a craft count (contract section 9.3).</summary>
    /// <param name="prefabName">The item's prefab name; must not be null, empty or whitespace.</param>
    /// <param name="amount">The amount at quality level 1; at least 0.</param>
    /// <param name="amountPerLevel">The extra amount per quality level above 1; at least 0.</param>
    /// <param name="qualityLevel">The target quality level; at least 1.</param>
    /// <param name="craftCount">How many crafts are made; at least 1.</param>
    /// <returns>A requirement with <see cref="QualityMatch.Any"/> and quality 1.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefabName"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="prefabName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An amount or level is out of range, or the total exceeds <see cref="int.MaxValue"/>.</exception>
    public static CraftRequirement ForLevel(string prefabName, int amount, int amountPerLevel, int qualityLevel, int craftCount)
    {
        RequirePrefabName(prefabName);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Must be at least 0.");
        }

        if (amountPerLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountPerLevel), "Must be at least 0.");
        }

        if (qualityLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(qualityLevel), "Must be at least 1.");
        }

        if (craftCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(craftCount), "Must be at least 1.");
        }

        long perCraft = qualityLevel <= 1 ? amount : (long)(qualityLevel - 1) * amountPerLevel;
        long total = perCraft * craftCount;
        if (total > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(craftCount), "The total amount exceeds the maximum.");
        }

        return new CraftRequirement(prefabName, (int)total);
    }

    /// <summary>Gets the item's prefab name, matched ordinally against the stacks' prefab names.</summary>
    public string PrefabName { get; }

    /// <summary>Gets the total number of items needed; 0 or more.</summary>
    public int Amount { get; }

    /// <summary>Gets how the quality of a stack is compared.</summary>
    public QualityMatch QualityMatch { get; }

    /// <summary>Gets the quality compared against; at least 1, and 1 when <see cref="QualityMatch"/> is Any.</summary>
    public int Quality { get; }

    /// <summary>Determines whether prefab name, amount, quality rule and quality are all equal.</summary>
    /// <param name="other">The requirement to compare with, or null.</param>
    /// <returns>True when equal; false for null.</returns>
    public bool Equals(CraftRequirement? other) =>
        other is not null
        && string.Equals(PrefabName, other.PrefabName, StringComparison.Ordinal)
        && Amount == other.Amount
        && QualityMatch == other.QualityMatch
        && Quality == other.Quality;

    /// <summary>Determines whether the object is an equal requirement.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as CraftRequirement);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = StringComparer.Ordinal.GetHashCode(PrefabName);
            h = (h * 31) + Amount;
            h = (h * 31) + (int)QualityMatch;
            return (h * 31) + Quality;
        }
    }

    /// <summary>Determines whether two requirements are equal; null is handled on either side.</summary>
    /// <param name="left">The first requirement, or null.</param>
    /// <param name="right">The second requirement, or null.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(CraftRequirement? left, CraftRequirement? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two requirements differ; null is handled on either side.</summary>
    /// <param name="left">The first requirement, or null.</param>
    /// <param name="right">The second requirement, or null.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(CraftRequirement? left, CraftRequirement? right) => !(left == right);

    private static void RequirePrefabName(string prefabName)
    {
        if (prefabName is null)
        {
            throw new ArgumentNullException(nameof(prefabName));
        }

        if (string.IsNullOrWhiteSpace(prefabName))
        {
            throw new ArgumentException("Must not be empty or whitespace.", nameof(prefabName));
        }
    }
}
