namespace BaseStorage.Core.Ui;

using System;
using BaseStorage.Core.Organize;

/// <summary>
/// One skipped chest as the end-of-run summary needs it (ui-strings contract 10.2): the reason kind and, for an in-use skip,
/// the player's name when it is known. The executor builds entries from the plan's skipped chests, or directly for chests it
/// skips at run time.
/// </summary>
public readonly struct OrganizeSkipEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="kind">The reason kind; a defined <see cref="SkipReason"/> value.</param>
    /// <param name="playerName">The player's name for an in-use skip with a known name; null, empty or blank means unknown.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined <see cref="SkipReason"/>.</exception>
    public OrganizeSkipEntry(SkipReason kind, string? playerName)
    {
        if (kind < SkipReason.Keep || kind > SkipReason.InvalidContents)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "The value is not a defined SkipReason.");
        }

        Kind = kind;
        PlayerName = string.IsNullOrWhiteSpace(playerName) ? null : playerName;
    }

    /// <summary>Gets the reason kind of the skip.</summary>
    public SkipReason Kind { get; }

    /// <summary>Gets the player's name for an in-use skip with a known name; otherwise null.</summary>
    public string? PlayerName { get; }

    /// <summary>Copies the kind and player name of a planned skip.</summary>
    /// <param name="skipped">The plan's skipped chest; not null.</param>
    /// <returns>An entry with the same <see cref="SkippedChest.Kind"/> and <see cref="SkippedChest.PlayerName"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="skipped"/> is null.</exception>
    public static OrganizeSkipEntry From(SkippedChest skipped)
    {
        if (skipped is null)
        {
            throw new ArgumentNullException(nameof(skipped));
        }

        return new OrganizeSkipEntry(skipped.Kind, skipped.PlayerName);
    }
}
