namespace BaseStorage.Core.ChestNames;

using System.Text;

/// <summary>
/// The rules of a chest name. A name comes from another player and is shown in hover text and world labels, so it is cleaned on every read as
/// well as every write. Pure.
/// </summary>
public static class ChestNameRules
{
    /// <summary>The longest name, in characters.</summary>
    public const int MaxLength = 32;

    /// <summary>Cleans a name: line breaks and control characters become spaces, rich text marks are dropped, and it is trimmed and capped.</summary>
    /// <param name="raw">The name, which may be null.</param>
    /// <returns>The clean name; empty when nothing is left.</returns>
    public static string Clean(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var clean = new StringBuilder(raw!.Length);
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (c == '<' || c == '>')
            {
                continue;
            }

            clean.Append(char.IsControl(c) || c == '\u2028' || c == '\u2029' ? ' ' : c);
        }

        var text = clean.ToString().Trim();
        if (text.Length <= MaxLength)
        {
            return text;
        }

        // A surrogate pair must not be cut in half.
        var length = char.IsHighSurrogate(text[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
        return text.Substring(0, length).TrimEnd();
    }
}
