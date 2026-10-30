namespace BaseStorage.Core.HostSync;

using System;
using System.Globalization;
using BaseStorage.Core.Settings;

/// <summary>
/// The canonical wire text of a setting value per <see cref="SettingKind"/> (contract host-sync-contracts.md 3, rule H2): Bool
/// <c>true</c>/<c>false</c>, Int invariant decimal, ItemList re-formatted through <see cref="ItemListText"/>, Hotkey as is; never a
/// control character. Pure and never throwing.
/// </summary>
public static class HostSyncValueText
{
    private const string True = "true";
    private const string False = "false";

    /// <summary>Canonicalises text for a kind.</summary>
    /// <param name="kind">The value kind.</param>
    /// <param name="text">The text to canonicalise; null gives false.</param>
    /// <param name="canonical">The canonical text when the method returns true; an empty string otherwise.</param>
    /// <returns>True when the text has a canonical form for the kind.</returns>
    public static bool TryCanonicalize(SettingKind kind, string? text, out string canonical)
    {
        canonical = string.Empty;
        if (text is null)
        {
            return false;
        }

        switch (kind)
        {
            case SettingKind.Bool:
                if (string.Equals(text, True, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = True;
                    return true;
                }

                if (string.Equals(text, False, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = False;
                    return true;
                }

                return false;

            case SettingKind.Int:
                if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
                {
                    return false;
                }

                canonical = value.ToString(CultureInfo.InvariantCulture);
                return true;

            case SettingKind.ItemList:
                if (HasControlCharacter(text))
                {
                    return false;
                }

                canonical = ItemListText.Format(ItemListText.Parse(text));
                return true;

            case SettingKind.Hotkey:
                if (HasControlCharacter(text))
                {
                    return false;
                }

                canonical = text;
                return true;

            default:
                return false;
        }
    }

    /// <summary>Determines whether text is already canonical for a kind.</summary>
    /// <param name="kind">The value kind.</param>
    /// <param name="text">The text to check; null gives false.</param>
    /// <returns>True when <see cref="TryCanonicalize"/> succeeds and returns the same string.</returns>
    public static bool IsCanonical(SettingKind kind, string? text)
    {
        return TryCanonicalize(kind, text, out string canonical) && string.Equals(canonical, text, StringComparison.Ordinal);
    }

    private static bool HasControlCharacter(string text)
    {
        foreach (char c in text)
        {
            if (c < ' ' || c == '\u007F')
            {
                return true;
            }
        }

        return false;
    }
}
