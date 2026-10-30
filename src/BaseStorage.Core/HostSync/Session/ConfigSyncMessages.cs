namespace BaseStorage.Core.HostSync.Session;

using System;
using System.Globalization;
using BaseStorage.Core.Logging;

/// <summary>
/// The log lines of config sync (contract config-sync-contracts.md 5.1 and 5.2). Every line starts with "Config sync: ".
/// </summary>
public static class ConfigSyncMessages
{
    private const string Prefix = "Config sync: ";

    /// <summary>Gets the line for an outcome, or null for None and Send.</summary>
    /// <param name="outcome">The outcome to format.</param>
    /// <returns>The line, or null.</returns>
    /// <exception cref="ArgumentNullException">The outcome is null.</exception>
    public static string? Format(ConfigSyncOutcome outcome)
    {
        if (outcome is null)
        {
            throw new ArgumentNullException(nameof(outcome));
        }

        string peer = outcome.PeerId.ToString(CultureInfo.InvariantCulture);
        switch (outcome.Action)
        {
            case ConfigSyncAction.Applied:
                return Prefix + "applied host settings from peer " + peer + ", "
                    + outcome.SettingCount.ToString(CultureInfo.InvariantCulture) + " settings, "
                    + outcome.CategoryCount.ToString(CultureInfo.InvariantCulture) + " categories";
            case ConfigSyncAction.Rejected:
                switch (outcome.Reason)
                {
                    case ConfigSyncRejectReason.NotClient:
                        return Prefix + "ignored payload from peer " + peer + ", not connected to a host";
                    case ConfigSyncRejectReason.NoAcceptedHost:
                        return Prefix + "ignored payload from peer " + peer + ", no accepted version handshake with that host";
                    case ConfigSyncRejectReason.Malformed:
                        return Prefix + "payload from peer " + peer + " is malformed, local settings stay active";
                    default:
                        return null;
                }

            default:
                return null;
        }
    }

    /// <summary>Tells whether the line for an outcome is a Warning: true only for Rejected with Malformed.</summary>
    /// <param name="outcome">The outcome to test.</param>
    /// <returns>True for a malformed rejection.</returns>
    /// <exception cref="ArgumentNullException">The outcome is null.</exception>
    public static bool IsWarning(ConfigSyncOutcome outcome)
    {
        if (outcome is null)
        {
            throw new ArgumentNullException(nameof(outcome));
        }

        return outcome.Action == ConfigSyncAction.Rejected && outcome.Reason == ConfigSyncRejectReason.Malformed;
    }

    /// <summary>Gets the policy category of a non-warning line: ConfigSync for Applied, Detail for every other outcome.</summary>
    /// <param name="outcome">The outcome to classify.</param>
    /// <returns>The log category.</returns>
    /// <exception cref="ArgumentNullException">The outcome is null.</exception>
    public static LogCategory CategoryOf(ConfigSyncOutcome outcome)
    {
        if (outcome is null)
        {
            throw new ArgumentNullException(nameof(outcome));
        }

        return outcome.Action == ConfigSyncAction.Applied ? LogCategory.ConfigSync : LogCategory.Detail;
    }

    /// <summary>Gets the line written when the host sends its payload.</summary>
    /// <param name="peerId">The peer the payload was sent to.</param>
    /// <param name="byteCount">The payload length in bytes.</param>
    /// <returns>The line.</returns>
    public static string Sent(long peerId, int byteCount)
    {
        return Prefix + "sent host settings to peer " + peerId.ToString(CultureInfo.InvariantCulture)
            + ", " + byteCount.ToString(CultureInfo.InvariantCulture) + " bytes";
    }

    /// <summary>Gets the warning written when the host payload exceeds the cap and is not sent.</summary>
    /// <param name="peerId">The peer the payload was not sent to.</param>
    /// <returns>The line.</returns>
    public static string TooLarge(long peerId)
    {
        return Prefix + "host settings for peer " + peerId.ToString(CultureInfo.InvariantCulture)
            + " exceed " + HostSyncCodec.MaxPayloadLength.ToString(CultureInfo.InvariantCulture) + " bytes and were not sent";
    }

    /// <summary>Gets the per-setting Debug line for one applied value.</summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The canonical value text.</param>
    /// <returns>The line.</returns>
    /// <exception cref="ArgumentNullException">The key or the value is null.</exception>
    public static string SettingDetail(string key, string value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return Prefix + key + " = " + value + " (host)";
    }

    /// <summary>Gets the Debug line written when the applied host payload is cleared.</summary>
    /// <returns>The line.</returns>
    public static string Cleared()
    {
        return Prefix + "host settings cleared, local settings apply";
    }

    /// <summary>Gets the Debug line written when the local category file is missing or unreadable.</summary>
    /// <param name="path">The category file path.</param>
    /// <returns>The line.</returns>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    public static string CategoryFileUnavailable(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return Prefix + "category file " + path + " is missing or unreadable, using no categories";
    }
}
