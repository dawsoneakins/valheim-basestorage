namespace BaseStorage.Plugin.Categories;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories.Defaults;

/// <summary>Writes the default category file once per process, as soon as the game item database holds items (contract F2).</summary>
internal sealed class FirstRunCategoryFile
{
    private readonly string configFolder;
    private readonly ILogSink sink;
    private bool done;

    /// <summary>Gets the BepInEx config folder that holds the local category file.</summary>
    internal string ConfigFolder => configFolder;

    /// <summary>Creates the first-run step.</summary>
    /// <param name="configFolder">The BepInEx config folder.</param>
    /// <param name="sink">The log channel.</param>
    internal FirstRunCategoryFile(string configFolder, ILogSink sink)
    {
        this.configFolder = configFolder;
        this.sink = sink;
    }

    /// <summary>Tries the first-run step. Never throws.</summary>
    /// <returns>True once the attempt is over (success or failure); false while the item database is not ready.</returns>
    internal bool Tick()
    {
        if (done)
        {
            return true;
        }

        try
        {
            ObjectDB? db = ObjectDB.instance;
            if (db == null || db.m_items == null || db.m_items.Count == 0)
            {
                return false;
            }

            List<CategoryItem> items = ObjectDbCategoryItems.Collect(db);
            if (items.Count == 0)
            {
                return false;
            }

            done = true;
            DefaultCategoryFileWriter.WriteIfMissing(configFolder, items, sink);
        }
        catch (Exception ex)
        {
            done = true;
            try
            {
                sink.Log(LogSeverity.Warning, DefaultCategoryFileMessages.WriteFailed(DefaultCategoryFileWriter.PathFor(configFolder), ex));
            }
            catch (Exception)
            {
                // A failing log sink must not escape into the game.
            }
        }

        return done;
    }
}
