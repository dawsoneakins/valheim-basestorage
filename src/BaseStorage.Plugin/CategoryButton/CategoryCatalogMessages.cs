namespace BaseStorage.Plugin.CategoryButton;

using System;

/// <summary>The Debug log lines of the runtime catalog load (contract S-E004-004 rule CB9). Log text, not user-facing text. Game-free.</summary>
internal static class CategoryCatalogMessages
{
    /// <summary>The line written when the category file is missing or defines no category, so the shipped defaults are used.</summary>
    /// <param name="path">The category file path.</param>
    /// <returns>The log line.</returns>
    internal static string Fallback(string path) =>
        "Category file " + path + " is missing or defines no category; the Category button cycles the shipped defaults.";

    /// <summary>The line written when the category file could not be read or parsed.</summary>
    /// <param name="path">The category file path.</param>
    /// <param name="error">The failure.</param>
    /// <returns>The log line.</returns>
    internal static string ReadFailed(string path, Exception error) =>
        "Category file " + path + " could not be read (" + (error == null ? string.Empty : error.GetType().Name) + "); the Category button cycles the shipped defaults.";
}
