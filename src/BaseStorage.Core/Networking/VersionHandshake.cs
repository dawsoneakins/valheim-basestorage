namespace BaseStorage.Core.Networking;

using System;

/// <summary>
/// Encodes and decodes the version handshake payload <c>BSV1;MAJOR.MINOR.PATCH</c> (contract version-handshake-contracts.md 3,
/// rules V4 and V5). Decoding never throws: anything that is not a payload gives null.
/// </summary>
public static class VersionHandshake
{
    /// <summary>The payload format tag that every payload starts with.</summary>
    public const string PayloadTag = "BSV1;";

    // "0.0.0" is the shortest version (5 characters); three nine-digit parts and two dots is the longest (29).
    private const int MinPayloadLength = 5 + 5;
    private const int MaxPayloadLength = 5 + 29;

    private static readonly string Local = PayloadTag + ModVersion.Current;

    /// <summary>Gets the payload this build sends: the tag followed by <see cref="ModVersion.Current"/>.</summary>
    public static string LocalPayload => Local;

    /// <summary>Encodes a version as a payload: the tag followed by the version.</summary>
    /// <param name="version">A well-formed version (see <see cref="ModVersion.IsWellFormed"/>).</param>
    /// <returns>The payload text.</returns>
    /// <exception cref="ArgumentNullException">The version is null.</exception>
    /// <exception cref="ArgumentException">The version is not well formed.</exception>
    public static string EncodePayload(string version) => PayloadTag + ModVersion.Require(version, nameof(version));

    /// <summary>Encodes a version as the UTF-8 (ASCII) bytes of its payload, without a byte order mark.</summary>
    /// <param name="version">A well-formed version (see <see cref="ModVersion.IsWellFormed"/>).</param>
    /// <returns>The payload bytes.</returns>
    /// <exception cref="ArgumentNullException">The version is null.</exception>
    /// <exception cref="ArgumentException">The version is not well formed.</exception>
    public static byte[] EncodePayloadBytes(string version)
    {
        string payload = EncodePayload(version);
        var bytes = new byte[payload.Length];
        for (int i = 0; i < payload.Length; i++)
        {
            // The grammar is ASCII only, so every character fits one byte and UTF-8 equals ASCII here.
            bytes[i] = (byte)payload[i];
        }

        return bytes;
    }

    /// <summary>Decodes a payload text; anything that is not a payload gives null. Never throws.</summary>
    /// <param name="payload">The received text, possibly null.</param>
    /// <returns>The well-formed version inside the payload, or null.</returns>
    public static string? DecodePayload(string? payload)
    {
        if (payload is null || payload.Length < MinPayloadLength || payload.Length > MaxPayloadLength)
        {
            return null;
        }

        if (!payload.StartsWith(PayloadTag, StringComparison.Ordinal))
        {
            return null;
        }

        string version = payload.Substring(PayloadTag.Length);
        return ModVersion.IsWellFormed(version) ? version : null;
    }

    /// <summary>Decodes payload bytes; null, empty, over-long, non-ASCII or otherwise malformed bytes give null. Never throws.</summary>
    /// <param name="payload">The received bytes, possibly null.</param>
    /// <returns>The well-formed version inside the payload, or null.</returns>
    public static string? DecodePayload(byte[]? payload)
    {
        if (payload is null || payload.Length < MinPayloadLength || payload.Length > MaxPayloadLength)
        {
            return null;
        }

        var chars = new char[payload.Length];
        for (int i = 0; i < payload.Length; i++)
        {
            byte b = payload[i];
            if (b >= 0x80)
            {
                return null;
            }

            chars[i] = (char)b;
        }

        return DecodePayload(new string(chars));
    }
}
