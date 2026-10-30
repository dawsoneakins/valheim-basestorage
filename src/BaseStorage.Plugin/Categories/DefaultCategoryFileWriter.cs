namespace BaseStorage.Plugin.Categories;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.Logging;

/// <summary>
/// First-run writer for the default category file (spec AC-7, AC-8; contract section 6). BCL and Core types only, so this file is
/// also compiled into BaseStorage.Core.Tests (ADR-GWIN-028). Never overwrites an existing file and never throws.
/// </summary>
internal static class DefaultCategoryFileWriter
{
    /// <summary>The full path of the category file inside the config folder, built with Path.Combine.</summary>
    /// <param name="configFolder">The BepInEx config folder.</param>
    /// <returns>The path of the category file.</returns>
    internal static string PathFor(string configFolder)
    {
        return Path.Combine(configFolder, DefaultCategoryFile.FileName);
    }

    /// <summary>Writes the category file when it is missing, using the real file system write.</summary>
    /// <param name="configFolder">The BepInEx config folder. Created when missing.</param>
    /// <param name="items">The items to list in the file.</param>
    /// <param name="sink">The log channel for the Info, Debug and Warning lines.</param>
    internal static void WriteIfMissing(string configFolder, IReadOnlyCollection<CategoryItem> items, ILogSink sink)
    {
        WriteIfMissing(configFolder, items, sink, (stream, bytes) => stream.Write(bytes, 0, bytes.Length));
    }

    /// <summary>Writes the category file when it is missing, through the given byte writer (the test seam).</summary>
    /// <param name="configFolder">The BepInEx config folder. Created when missing.</param>
    /// <param name="items">The items to list in the file.</param>
    /// <param name="sink">The log channel for the Info, Debug and Warning lines.</param>
    /// <param name="writeBytes">Writes the full byte array to the open stream.</param>
    internal static void WriteIfMissing(
        string configFolder,
        IReadOnlyCollection<CategoryItem> items,
        ILogSink sink,
        Action<Stream, byte[]> writeBytes)
    {
        string path = string.Empty;
        bool incomplete = false;
        try
        {
            path = PathFor(configFolder);
            if (File.Exists(path))
            {
                sink.Log(LogSeverity.Debug, DefaultCategoryFileMessages.KeptExisting(path));
                return;
            }

            byte[] bytes = new UTF8Encoding(false).GetBytes(DefaultCategoryFile.Generate(items));
            Directory.CreateDirectory(configFolder);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                incomplete = true;
                writeBytes(stream, bytes);
                stream.Flush();
            }

            incomplete = false;
            LogPolicy.Write(sink, LogCategory.Load, DefaultCategoryFileMessages.Written(path, items.Count));
        }
        catch (Exception ex)
        {
            if (incomplete)
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception)
                {
                    // Best effort: the partial file is ours, but a failed delete must not escape.
                }
            }

            try
            {
                sink.Log(LogSeverity.Warning, DefaultCategoryFileMessages.WriteFailed(path, ex));
            }
            catch (Exception)
            {
                // A failing log sink must not escape into the game.
            }
        }
    }
}
