namespace BaseStorage.Core.Persistence;

using System;

/// <summary>Encodes and decodes the value stored under the chest category key.</summary>
public static class CategoryCodec
{
    private const string KeepText = "k";
    private const string NamedPrefix = "c:";

    /// <summary>Encodes an assignment as k or c: followed by the name.</summary>
    /// <param name="value">The assignment to encode.</param>
    /// <returns>The encoded text.</returns>
    public static string Encode(CategoryAssignment value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return value.IsKeep ? KeepText : NamedPrefix + value.CategoryName;
    }

    /// <summary>Decodes text produced by Encode; anything else gives no value.</summary>
    /// <param name="encoded">The stored text, possibly null.</param>
    /// <returns>The assignment, or null for no value.</returns>
    public static CategoryAssignment? Decode(string? encoded)
    {
        if (encoded is null)
        {
            return null;
        }

        if (string.Equals(encoded, KeepText, StringComparison.Ordinal))
        {
            return CategoryAssignment.Keep;
        }

        if (!encoded.StartsWith(NamedPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        string name = encoded.Substring(NamedPrefix.Length);
        return NameRule.IsValid(name) ? CategoryAssignment.ForCategory(name) : null;
    }
}
