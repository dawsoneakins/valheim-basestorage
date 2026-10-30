namespace BaseStorage.Core.Persistence;

using System;

/// <summary>Name rule N shared by category names and lock holder names.</summary>
internal static class NameRule
{
    /// <summary>Throws when the name breaks the rule.</summary>
    /// <param name="name">The name to check.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    internal static void Require(string? name, string paramName)
    {
        if (name is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (!IsValid(name))
        {
            throw new ArgumentException("The name must not be blank or contain control characters.", paramName);
        }
    }

    /// <summary>Returns true when the non-null name satisfies the rule.</summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True when valid.</returns>
    internal static bool IsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        foreach (char c in name)
        {
            if (char.IsControl(c))
            {
                return false;
            }
        }

        return true;
    }
}
