namespace BaseStorage.Core.Tests;

using System;

/// <summary>
/// Test-side matcher for the forbidden assembly families (contracts/build-layout.yml section 3).
/// Matching is ordinal, case-insensitive and prefix-based on the whole simple name.
/// </summary>
public static class ForbiddenAssemblies
{
    private static readonly (string Family, string[] Prefixes)[] Families =
    [
        ("Unity", ["Unity"]),
        ("Valheim", ["assembly_", "Valheim"]),
        ("BepInEx", ["BepInEx"]),
        ("Harmony", ["0Harmony", "Harmony"]),
    ];

    /// <summary>Returns the family name for a forbidden simple name, or null when the name is allowed.</summary>
    public static string? FamilyOf(string simpleName)
    {
        if (simpleName is null)
        {
            throw new ArgumentNullException(nameof(simpleName));
        }

        foreach ((string family, string[] prefixes) in Families)
        {
            foreach (string prefix in prefixes)
            {
                if (simpleName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return family;
                }
            }
        }

        return null;
    }

    /// <summary>True when <see cref="FamilyOf"/> returns a family.</summary>
    public static bool IsForbidden(string simpleName) => FamilyOf(simpleName) is not null;
}
