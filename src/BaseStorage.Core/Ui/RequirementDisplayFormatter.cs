namespace BaseStorage.Core.Ui;

using System;
using System.Globalization;

/// <summary>Formats a requirement line as <c>total/need</c> when base chests contribute, short enough to fit the requirement slot.</summary>
public static class RequirementDisplayFormatter
{
    /// <summary>Decides the text and colour of one requirement line.</summary>
    /// <param name="have">The inventory amount.</param>
    /// <param name="chest">The base-chest amount.</param>
    /// <param name="need">The required amount.</param>
    /// <param name="vanillaText">The text the game wrote; returned unchanged when no chest contributes.</param>
    /// <returns>The display.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An amount is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="vanillaText"/> is null.</exception>
    public static RequirementDisplay Format(int have, int chest, int need, string vanillaText)
    {
        Validate(have, chest, need);
        if (vanillaText == null)
        {
            throw new ArgumentNullException(nameof(vanillaText));
        }

        bool isShort = IsShort(have, chest, need);
        if (chest == 0)
        {
            return new RequirementDisplay(vanillaText, isShort, false);
        }

        string text = ((long)have + chest).ToString(CultureInfo.InvariantCulture)
            + "/" + need.ToString(CultureInfo.InvariantCulture);
        return new RequirementDisplay(text, isShort, true);
    }

    /// <summary>Tells whether inventory plus chests is less than the need.</summary>
    /// <param name="have">The inventory amount.</param>
    /// <param name="chest">The base-chest amount.</param>
    /// <param name="need">The required amount.</param>
    /// <returns>True when short; exact equality is not short.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An amount is negative.</exception>
    public static bool IsShort(int have, int chest, int need)
    {
        Validate(have, chest, need);
        return (long)have + chest < need;
    }

    private static void Validate(int have, int chest, int need)
    {
        if (have < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(have), "Must be at least 0.");
        }

        if (chest < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chest), "Must be at least 0.");
        }

        if (need < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(need), "Must be at least 0.");
        }
    }
}
