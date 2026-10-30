namespace BaseStorage.Core.Bases.Membership;

using System;
using BaseStorage.Core.Settings;

/// <summary>The two settings that eligibility depends on, captured as plain values so eligibility never reads a settings source.</summary>
public readonly struct EligibilityOptions : IEquatable<EligibilityOptions>
{
    /// <summary>Creates options from explicit values.</summary>
    /// <param name="excludeVehicles">The ExcludeVehicles setting: true excludes containers flagged Vehicle.</param>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable setting: true lets CraftAvailability use Keep containers.</param>
    public EligibilityOptions(bool excludeVehicles, bool keepChestsCraftable)
    {
        ExcludeVehicles = excludeVehicles;
        KeepChestsCraftable = keepChestsCraftable;
    }

    /// <summary>Gets a value indicating whether containers flagged Vehicle are excluded.</summary>
    public bool ExcludeVehicles { get; }

    /// <summary>Gets a value indicating whether Keep containers are eligible for CraftAvailability.</summary>
    public bool KeepChestsCraftable { get; }

    /// <summary>Reads ExcludeVehicles and KeepChestsCraftable once from <paramref name="settings"/>.</summary>
    /// <param name="settings">The settings read path; must not be null.</param>
    /// <returns>Options holding the two values read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
    public static EligibilityOptions FromSettings(BaseStorageSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        return new EligibilityOptions(settings.ExcludeVehicles, settings.KeepChestsCraftable);
    }

    /// <summary>Determines whether both values are equal.</summary>
    /// <param name="other">The options to compare with.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(EligibilityOptions other) =>
        ExcludeVehicles == other.ExcludeVehicles && KeepChestsCraftable == other.KeepChestsCraftable;

    /// <summary>Determines whether the object is equal options.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => obj is EligibilityOptions other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => (ExcludeVehicles ? 1 : 0) | (KeepChestsCraftable ? 2 : 0);

    /// <summary>Determines whether two options are equal.</summary>
    /// <param name="left">The first options.</param>
    /// <param name="right">The second options.</param>
    /// <returns>True when equal.</returns>
    public static bool operator ==(EligibilityOptions left, EligibilityOptions right) => left.Equals(right);

    /// <summary>Determines whether two options differ.</summary>
    /// <param name="left">The first options.</param>
    /// <param name="right">The second options.</param>
    /// <returns>True when different.</returns>
    public static bool operator !=(EligibilityOptions left, EligibilityOptions right) => !left.Equals(right);
}
