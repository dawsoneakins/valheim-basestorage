namespace BaseStorage.Plugin.CategoryButton;

using System;
using System.IO;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.HostSync.Session;
using BaseStorage.Core.Logging;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.HostSync;

/// <summary>
/// The catalog the category cycle walks at runtime (contract S-E004-004 rule CB9, ADR-GWIN-107): the local category file parsed once on
/// first use, or the shipped default catalog when the file is missing, unreadable or defines no category. A host-synced catalog replaces
/// this source in S-E006-004: while a host payload is applied on a client, the host's catalog (read through
/// <see cref="PluginConfigSync.Categories"/>, config-sync contract N11) is used when it defines a category. Main thread only.
/// </summary>
internal static class PluginCategoryCatalog
{
    private static string? configFolder;
    private static ILogSink? sink;
    private static CategoryCatalog? current;

    /// <summary>Gets the runtime catalog: the host's while a host payload is applied, otherwise the local one, loaded on the first read after <see cref="Initialize"/>.</summary>
    internal static CategoryCatalog Current
    {
        get
        {
            CategoryCatalog? host = HostCatalog();
            if (host != null)
            {
                return host;
            }

            CategoryCatalog? loaded = current;
            if (loaded == null)
            {
                loaded = Load();
                current = loaded;
            }

            return loaded;
        }
    }

    /// <summary>Stores the config folder and the log channel at boot. Nothing is read here: the file is written later by the first-run step.</summary>
    /// <param name="configFolder">The BepInEx config folder.</param>
    /// <param name="sink">The log channel.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static void Initialize(string configFolder, ILogSink sink)
    {
        if (configFolder == null)
        {
            throw new ArgumentNullException(nameof(configFolder));
        }

        if (sink == null)
        {
            throw new ArgumentNullException(nameof(sink));
        }

        PluginCategoryCatalog.configFolder = configFolder;
        PluginCategoryCatalog.sink = sink;
        current = null;
    }

    private static CategoryCatalog? HostCatalog()
    {
        ConfigSyncSession? sync = PluginConfigSync.Current;
        if (sync == null || !sync.Settings.HasHost)
        {
            return null;
        }

        CategoryCatalog catalog = PluginConfigSync.Categories;
        return catalog.CategoryNames.Count > 0 ? catalog : null;
    }

    private static CategoryCatalog Load()
    {
        string? folder = configFolder;
        if (folder == null)
        {
            return DefaultCategories.Catalog;
        }

        string path = DefaultCategoryFileWriter.PathFor(folder);
        try
        {
            if (File.Exists(path))
            {
                CategoryParseResult parsed = CategoryFile.Parse(File.ReadAllText(path));
                if (parsed.Catalog.CategoryNames.Count > 0)
                {
                    return parsed.Catalog;
                }
            }

            LogPolicy.Write(sink, LogCategory.Detail, CategoryCatalogMessages.Fallback(path));
        }
        catch (Exception ex)
        {
            LogPolicy.Write(sink, LogCategory.Detail, CategoryCatalogMessages.ReadFailed(path, ex));
        }

        return DefaultCategories.Catalog;
    }
}
