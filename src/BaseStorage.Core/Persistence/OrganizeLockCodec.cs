namespace BaseStorage.Core.Persistence;

using System;
using System.Globalization;

/// <summary>Encodes and decodes the value stored under the organize lock key.</summary>
public static class OrganizeLockCodec
{
    private const string Tag = "L1;";

    /// <summary>Encodes a record as L1, holder id, expiry ticks and holder name, separated by semicolons.</summary>
    /// <param name="value">The record to encode.</param>
    /// <returns>The encoded text.</returns>
    public static string Encode(OrganizeLockRecord value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return Tag
            + value.HolderId.ToString(CultureInfo.InvariantCulture)
            + ";"
            + value.ExpiresAt.Ticks.ToString(CultureInfo.InvariantCulture)
            + ";"
            + value.HolderName;
    }

    /// <summary>Decodes text produced by Encode; anything else gives no value.</summary>
    /// <param name="encoded">The stored text, possibly null.</param>
    /// <returns>The record, or null for no value.</returns>
    public static OrganizeLockRecord? Decode(string? encoded)
    {
        if (encoded is null || !encoded.StartsWith(Tag, StringComparison.Ordinal))
        {
            return null;
        }

        int idStart = Tag.Length;
        int idEnd = encoded.IndexOf(';', idStart);
        if (idEnd < 0)
        {
            return null;
        }

        int ticksStart = idEnd + 1;
        int ticksEnd = encoded.IndexOf(';', ticksStart);
        if (ticksEnd < 0)
        {
            return null;
        }

        if (!WireNumbers.TryParseInt64(encoded, idStart, idEnd - idStart, out long holderId)
            || !WireNumbers.TryParseInt64(encoded, ticksStart, ticksEnd - ticksStart, out long ticks))
        {
            return null;
        }

        string name = encoded.Substring(ticksEnd + 1);
        if (holderId == 0 || !NameRule.IsValid(name))
        {
            return null;
        }

        return new OrganizeLockRecord(holderId, name, TimeSpan.FromTicks(ticks));
    }
}
