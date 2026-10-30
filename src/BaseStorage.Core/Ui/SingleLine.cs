namespace BaseStorage.Core.Ui;

using System;
using System.Text;

/// <summary>Collapses line breaks so a text fits on one line.</summary>
public static class SingleLine
{
    /// <summary>Replaces each maximal run of line-break characters with one space.</summary>
    /// <param name="text">The text to collapse.</param>
    /// <returns>The text with every run of line-break characters replaced by a single space.</returns>
    public static string Of(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        StringBuilder? builder = null;
        bool inRun = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (IsLineBreak(c))
            {
                if (builder is null)
                {
                    builder = new StringBuilder(text.Length);
                    builder.Append(text, 0, i);
                }

                if (!inRun)
                {
                    builder.Append(' ');
                    inRun = true;
                }
            }
            else
            {
                inRun = false;
                builder?.Append(c);
            }
        }

        return builder is null ? text : builder.ToString();
    }

    private static bool IsLineBreak(char c) =>
        c == 0x000D || c == 0x000A || c == 0x0085 || c == 0x2028 || c == 0x2029;
}
