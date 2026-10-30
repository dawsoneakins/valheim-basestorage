namespace BaseStorage.Core.Persistence;

/// <summary>Canonical ASCII-only number parsing for the wire formats.</summary>
internal static class WireNumbers
{
    /// <summary>Parses INT64: zero, or an optional minus, a non-zero digit and more digits. Never throws.</summary>
    /// <remarks>long.MinValue and long.MaxValue parse; one beyond either bound is rejected (covered by OrganizeLockCodecTests).</remarks>
    /// <param name="text">The text holding the field.</param>
    /// <param name="start">The index of the first character.</param>
    /// <param name="length">The field length.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns>True when the field is a canonical INT64.</returns>
    internal static bool TryParseInt64(string text, int start, int length, out long value)
    {
        value = 0;
        if (length <= 0 || length > 20)
        {
            return false;
        }

        int i = start;
        int end = start + length;
        bool negative = false;
        if (text[i] == '-')
        {
            negative = true;
            i++;
            if (i == end)
            {
                return false;
            }
        }

        if (text[i] == '0')
        {
            // Only the bare "0" is canonical; "-0" and leading zeros are not.
            return !negative && length == 1;
        }

        long result = 0;
        for (; i < end; i++)
        {
            char c = text[i];
            if (c < '0' || c > '9')
            {
                return false;
            }

            int digit = c - '0';
            if (result < (long.MinValue + digit) / 10)
            {
                return false;
            }

            result = (result * 10) - digit;
        }

        if (!negative)
        {
            if (result == long.MinValue)
            {
                return false;
            }

            result = -result;
        }

        value = result;
        return true;
    }

    /// <summary>Parses SLOTNUM: 0 to 255 in ASCII digits without leading zeros. Never throws.</summary>
    /// <param name="text">The text holding the field.</param>
    /// <param name="start">The index of the first character.</param>
    /// <param name="length">The field length.</param>
    /// <param name="value">The parsed value.</param>
    /// <returns>True when the field is a canonical SLOTNUM.</returns>
    internal static bool TryParseSlotNumber(string text, int start, int length, out int value)
    {
        value = 0;
        if (length < 1 || length > 3)
        {
            return false;
        }

        if (text[start] == '0')
        {
            return length == 1;
        }

        int result = 0;
        for (int i = start; i < start + length; i++)
        {
            char c = text[i];
            if (c < '0' || c > '9')
            {
                return false;
            }

            result = (result * 10) + (c - '0');
        }

        if (result > 255)
        {
            return false;
        }

        value = result;
        return true;
    }
}
