namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using System.Text;
using BaseStorage.Core.Ui;

/// <summary>Builds every line a move shows from the string table. Pure.</summary>
public static class MoveText
{
    private const int MaxNamesShown = 3;

    /// <summary>Builds the preview's body.</summary>
    /// <param name="summary">The plan's summary. Not null.</param>
    /// <param name="strings">The string table. Not null.</param>
    /// <returns>The body.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static string Preview(MoveSummary summary, StringTable strings)
    {
        Check(summary, strings);
        var text = new StringBuilder(strings.Format(StringKeys.MovePreviewMoved, summary.MovedStacks));
        if (summary.StayedStacks > 0)
        {
            text.Append(' ').Append(strings.Format(StringKeys.MovePreviewStayed, summary.StayedStacks, Names(summary.StayedPrefabs)));
        }

        if (summary.NoRoomStacks > 0)
        {
            text.Append(' ').Append(strings.Format(StringKeys.MovePreviewNoRoom, summary.NoRoomStacks));
        }

        if (summary.SkippedChests > 0)
        {
            text.Append(' ').Append(strings.Format(StringKeys.MovePreviewSkipped, summary.SkippedChests));
        }

        return text.ToString();
    }

    /// <summary>Builds the final line of a finished move.</summary>
    /// <param name="summary">The plan's summary. Not null.</param>
    /// <param name="destinationName">The destination's name.</param>
    /// <param name="strings">The string table. Not null.</param>
    /// <returns>The line.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static string Done(MoveSummary summary, string destinationName, StringTable strings)
    {
        Check(summary, strings);
        var text = new StringBuilder(strings.Format(StringKeys.MoveDoneMoved, summary.MovedStacks, destinationName));
        if (summary.StayedStacks > 0)
        {
            text.Append(strings.Format(StringKeys.MoveDoneStayed, summary.StayedStacks, Names(summary.StayedPrefabs)));
        }

        if (summary.NoRoomStacks > 0)
        {
            text.Append(strings.Format(StringKeys.MoveDoneNoRoom, summary.NoRoomStacks));
        }

        if (summary.SkippedChests > 0)
        {
            text.Append(strings.Format(StringKeys.MoveDoneSkipped, summary.SkippedChests));
        }

        return text.ToString();
    }

    /// <summary>Builds the line of a move that did not happen.</summary>
    /// <param name="refusal">The reason.</param>
    /// <param name="playerName">The player the reason names, or null.</param>
    /// <param name="strings">The string table. Not null.</param>
    /// <returns>The line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="strings"/> is null.</exception>
    public static string Refused(MoveRefusal refusal, string? playerName, StringTable strings)
    {
        if (strings is null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        switch (refusal)
        {
            case MoveRefusal.Busy:
                return strings.Get(StringKeys.MoveRefusedBusy);
            case MoveRefusal.NoSuchBase:
                return strings.Get(StringKeys.MoveRefusedNoSuchBase);
            case MoveRefusal.PlayerAtBase:
                return strings.Format(StringKeys.MoveRefusedPlayerAtBase, playerName ?? string.Empty);
            case MoveRefusal.BasesTooNear:
                return strings.Get(StringKeys.MoveRefusedSameBase);
            case MoveRefusal.Changed:
                return strings.Get(StringKeys.MoveRefusedChanged);
            case MoveRefusal.TimedOut:
                return strings.Get(StringKeys.MoveRefusedTimedOut);
            default:
                return strings.Get(StringKeys.MoveRefusedFailed);
        }
    }

    private static void Check(MoveSummary summary, StringTable strings)
    {
        if (summary is null)
        {
            throw new ArgumentNullException(nameof(summary));
        }

        if (strings is null)
        {
            throw new ArgumentNullException(nameof(strings));
        }
    }

    private static string Names(IReadOnlyList<string> names)
    {
        var shown = new List<string>(MaxNamesShown + 1);
        for (var i = 0; i < names.Count && i < MaxNamesShown; i++)
        {
            shown.Add(names[i]);
        }

        if (names.Count > MaxNamesShown)
        {
            shown.Add("...");
        }

        return string.Join(", ", shown);
    }
}
