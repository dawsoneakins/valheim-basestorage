namespace BaseStorage.Core.HostSync;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseStorage.Core.Settings;

/// <summary>
/// Encodes and decodes the host-sync payload <c>BSC1;KEY=VALUE LF ... @LENGTH LF TEXT</c> (contract host-sync-contracts.md 4,
/// rules H4, H5 and H8). Decoding never throws: anything that is not a canonical payload gives null.
/// </summary>
public static class HostSyncCodec
{
    /// <summary>The payload format tag that every payload starts with.</summary>
    public const string PayloadTag = "BSC1;";

    /// <summary>The longest text (UTF-16 code units) or byte array (bytes) the decoders read; longer input gives null.</summary>
    public const int MaxPayloadLength = 1048576;

    private const char LineFeed = '\n';
    private const char CarriageReturn = '\r';
    private const char Separator = '=';
    private const char LengthMarker = '@';

    // MaxPayloadLength has seven digits, so a longer length text can never match the remainder.
    private const int MaxLengthDigits = 7;

    // Encoding replaces an unpaired surrogate (never produced by a UTF-8 file read); decoding is strict so invalid bytes give null.
    private static readonly UTF8Encoding Utf8Writer = new UTF8Encoding(false, false);
    private static readonly UTF8Encoding Utf8Reader = new UTF8Encoding(false, true);

    /// <summary>Encodes a payload as text.</summary>
    /// <param name="payload">The payload to encode.</param>
    /// <returns>The payload text.</returns>
    /// <exception cref="ArgumentNullException">The payload is null.</exception>
    public static string Encode(HostSyncPayload payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        var builder = new StringBuilder(PayloadTag.Length + 256 + payload.CategoryFileText.Length);
        builder.Append(PayloadTag);
        foreach (KeyValuePair<string, string> pair in payload.Settings)
        {
            builder.Append(pair.Key).Append(Separator).Append(pair.Value).Append(LineFeed);
        }

        builder.Append(LengthMarker).Append(payload.CategoryFileText.Length.ToString(CultureInfo.InvariantCulture)).Append(LineFeed);
        builder.Append(payload.CategoryFileText);
        return builder.ToString();
    }

    /// <summary>Encodes a payload as the UTF-8 bytes of its text, without a byte order mark.</summary>
    /// <param name="payload">The payload to encode.</param>
    /// <returns>The payload bytes.</returns>
    /// <exception cref="ArgumentNullException">The payload is null.</exception>
    public static byte[] EncodeBytes(HostSyncPayload payload)
    {
        return Utf8Writer.GetBytes(Encode(payload));
    }

    /// <summary>Decodes payload text; anything that is not a canonical payload gives null. Never throws.</summary>
    /// <param name="payload">The received text, possibly null.</param>
    /// <returns>The payload, or null.</returns>
    public static HostSyncPayload? Decode(string? payload)
    {
        if (payload is null || payload.Length == 0 || payload.Length > MaxPayloadLength)
        {
            return null;
        }

        if (!payload.StartsWith(PayloadTag, StringComparison.Ordinal) || payload.IndexOf(CarriageReturn) >= 0)
        {
            return null;
        }

        IReadOnlyList<SettingDefinition> rows = SettingsCatalog.HostSynced;
        var pairs = new KeyValuePair<string, string>[rows.Count];
        int position = PayloadTag.Length;
        for (int i = 0; i < rows.Count; i++)
        {
            int lineEnd = payload.IndexOf(LineFeed, position);
            if (lineEnd < 0)
            {
                return null;
            }

            string key = rows[i].Key;
            int valueStart = position + key.Length + 1;
            if (valueStart > lineEnd
                || string.CompareOrdinal(payload, position, key, 0, key.Length) != 0
                || payload[position + key.Length] != Separator)
            {
                return null;
            }

            string value = payload.Substring(valueStart, lineEnd - valueStart);
            if (!HostSyncValueText.IsCanonical(rows[i].Kind, value))
            {
                return null;
            }

            pairs[i] = new KeyValuePair<string, string>(key, value);
            position = lineEnd + 1;
        }

        if (position >= payload.Length || payload[position] != LengthMarker)
        {
            return null;
        }

        int lengthEnd = payload.IndexOf(LineFeed, position);
        if (lengthEnd < 0 || !TryReadLength(payload, position + 1, lengthEnd, out int length))
        {
            return null;
        }

        int textStart = lengthEnd + 1;
        if (payload.Length - textStart != length)
        {
            return null;
        }

        return new HostSyncPayload(pairs, payload.Substring(textStart));
    }

    /// <summary>Decodes payload bytes; null, empty, over-long, invalid UTF-8, a leading byte order mark or a non-payload gives null. Never throws.</summary>
    /// <param name="payload">The received bytes, possibly null.</param>
    /// <returns>The payload, or null.</returns>
    public static HostSyncPayload? Decode(byte[]? payload)
    {
        if (payload is null || payload.Length == 0 || payload.Length > MaxPayloadLength || payload[0] != (byte)PayloadTag[0])
        {
            return null;
        }

        string text;
        try
        {
            text = Utf8Reader.GetString(payload);
        }
        catch (ArgumentException)
        {
            // DecoderFallbackException: the bytes are not valid UTF-8.
            return null;
        }

        return Decode(text);
    }

    /// <summary>Reads a LENGTH of grammar 4.2 ("0" or digits without a leading zero) from text[start..end).</summary>
    private static bool TryReadLength(string text, int start, int end, out int length)
    {
        length = 0;
        int digits = end - start;
        if (digits <= 0 || digits > MaxLengthDigits || (digits > 1 && text[start] == '0'))
        {
            return false;
        }

        for (int i = start; i < end; i++)
        {
            char c = text[i];
            if (c < '0' || c > '9')
            {
                return false;
            }

            length = (length * 10) + (c - '0');
        }

        return true;
    }
}
