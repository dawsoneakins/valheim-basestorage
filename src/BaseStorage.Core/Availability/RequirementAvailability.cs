namespace BaseStorage.Core.Availability;

using System;

/// <summary>The answer for one requirement: how many matching items the player inventory and the counted containers hold (contract section 9.4).</summary>
public sealed class RequirementAvailability : IEquatable<RequirementAvailability>
{
    /// <summary>Creates the availability of one requirement.</summary>
    /// <param name="requirement">The requirement answered for; must not be null.</param>
    /// <param name="inInventory">Matching items in the player inventory; at least 0.</param>
    /// <param name="inContainers">Matching items in the counted containers; at least 0.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requirement"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="inInventory"/> or <paramref name="inContainers"/> is negative.</exception>
    public RequirementAvailability(CraftRequirement requirement, int inInventory, int inContainers)
    {
        Requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
        if (inInventory < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inInventory), "Must be at least 0.");
        }

        if (inContainers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inContainers), "Must be at least 0.");
        }

        InInventory = inInventory;
        InContainers = inContainers;
    }

    /// <summary>Gets the requirement this availability answers.</summary>
    public CraftRequirement Requirement { get; }

    /// <summary>Gets the matching items in the player inventory.</summary>
    public int InInventory { get; }

    /// <summary>Gets the matching items in the counted containers.</summary>
    public int InContainers { get; }

    /// <summary>Gets the inventory and container counts together, saturating at <see cref="int.MaxValue"/> (derived).</summary>
    public int Total => (int)Math.Min((long)InInventory + InContainers, int.MaxValue);

    /// <summary>Gets a value indicating whether <see cref="Total"/> reaches the requirement's amount (derived).</summary>
    public bool IsMet => Total >= Requirement.Amount;

    /// <summary>Gets how many more items are needed, never below 0 (derived).</summary>
    public int Missing => Math.Max(0, Requirement.Amount - Total);

    /// <summary>Determines whether the requirement and both counts are equal; the derived values are not compared separately.</summary>
    /// <param name="other">The availability to compare with, or null.</param>
    /// <returns>True when equal; false for null.</returns>
    public bool Equals(RequirementAvailability? other) =>
        other is not null && Requirement == other.Requirement && InInventory == other.InInventory && InContainers == other.InContainers;

    /// <summary>Determines whether the object is an equal availability.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => Equals(obj as RequirementAvailability);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = Requirement.GetHashCode();
            h = (h * 31) + InInventory;
            return (h * 31) + InContainers;
        }
    }

    /// <summary>Determines whether two availabilities are equal; null is handled on either side.</summary>
    /// <param name="left">The first availability, or null.</param>
    /// <param name="right">The second availability, or null.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(RequirementAvailability? left, RequirementAvailability? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two availabilities differ; null is handled on either side.</summary>
    /// <param name="left">The first availability, or null.</param>
    /// <param name="right">The second availability, or null.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(RequirementAvailability? left, RequirementAvailability? right) => !(left == right);
}
