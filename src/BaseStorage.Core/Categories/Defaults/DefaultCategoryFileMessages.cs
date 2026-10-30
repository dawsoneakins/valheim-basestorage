namespace BaseStorage.Core.Categories.Defaults;

using System;
using System.Globalization;
using BaseStorage.Core.Patching;

/// <summary>
/// The first-run log lines of the default category file (contract section 5). Pure: no prefix, no trailing period and no newline.
/// </summary>
public static class DefaultCategoryFileMessages
{
    /// <summary>The Info line written after the category file was written.</summary>
    /// <param name="path">The full path of the written file. Shaped to a single line.</param>
    /// <param name="itemCount">The number of items passed to the generator. Never negative.</param>
    /// <returns>The Info line text.</returns>
    public static string Written(string path, int itemCount)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount));
        }

        return "Wrote default category file '" + SingleLineText.Of(path) + "' with " + itemCount.ToString(CultureInfo.InvariantCulture) + " item(s)";
    }

    /// <summary>The Debug line written when the category file already exists and is left as it is.</summary>
    /// <param name="path">The full path of the existing file. Shaped to a single line.</param>
    /// <returns>The Debug line text.</returns>
    public static string KeptExisting(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return "Category file '" + SingleLineText.Of(path) + "' already exists; left unchanged";
    }

    /// <summary>The Warning line written when the first-run step failed. The game keeps loading.</summary>
    /// <param name="path">The full path of the file that could not be written. Shaped to a single line.</param>
    /// <param name="error">The exception that stopped the write.</param>
    /// <returns>The Warning line text.</returns>
    public static string WriteFailed(string path, Exception error)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        return "Could not write default category file '" + SingleLineText.Of(path) + "': " + SingleLineText.DescribeWithInner(error);
    }
}
