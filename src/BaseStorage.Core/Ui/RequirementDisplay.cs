namespace BaseStorage.Core.Ui;

using System;

/// <summary>The text and colour decision for one requirement line.</summary>
public readonly struct RequirementDisplay : IEquatable<RequirementDisplay>
{
    /// <summary>Creates a display.</summary>
    /// <param name="text">The text to show.</param>
    /// <param name="isShort">True when inventory plus chests is less than the need.</param>
    /// <param name="showsChests">True when the chest amount is greater than 0.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public RequirementDisplay(string text, bool isShort, bool showsChests)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        Text = text;
        IsShort = isShort;
        ShowsChests = showsChests;
    }

    /// <summary>Gets the text to show on the requirement element.</summary>
    /// <returns>The text.</returns>
    public string Text { get; }

    /// <summary>Gets a value indicating whether inventory plus chests is less than the need, so the vanilla red applies.</summary>
    /// <returns>True when short.</returns>
    public bool IsShort { get; }

    /// <summary>Gets a value indicating whether the chest amount is greater than 0 (the text is the combined form).</summary>
    /// <returns>True when a chest contributes.</returns>
    public bool ShowsChests { get; }

    /// <summary>Compares two displays for equality.</summary>
    /// <param name="left">The first display.</param>
    /// <param name="right">The second display.</param>
    /// <returns>True when equal.</returns>
    public static bool operator
        ==(RequirementDisplay left, RequirementDisplay right) => left.Equals(right);

    /// <summary>Compares two displays for inequality.</summary>
    /// <param name="left">The first display.</param>
    /// <param name="right">The second display.</param>
    /// <returns>True when different.</returns>
    public static bool operator
        !=(RequirementDisplay left, RequirementDisplay right) => !left.Equals(right);

    /// <summary>Compares with another display.</summary>
    /// <param name="other">The other display.</param>
    /// <returns>True when text (ordinal), shortness and chest flag are equal.</returns>
    public bool Equals(RequirementDisplay other) =>
        string.Equals(Text, other.Text, StringComparison.Ordinal) && IsShort == other.IsShort && ShowsChests == other.ShowsChests;

    /// <summary>Determines whether the object is an equal display.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj) => obj is RequirementDisplay other && Equals(other);

    /// <summary>Gets a hash code consistent with equality.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (Text == null ? 0 : StringComparer.Ordinal.GetHashCode(Text));
            hash = (hash * 31) + (IsShort ? 1 : 0);
            hash = (hash * 31) + (ShowsChests ? 1 : 0);
            return hash;
        }
    }
}
