namespace BaseStorage.Plugin.Hover;

using BaseStorage.Core.Bases.Inspection;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.ChestNames;
using BaseStorage.Plugin.World;

/// <summary>
/// The text that the two hover patches append (ADR-GWIN-032). It only gathers the game's values and asks <see cref="HoverInspector"/> for the line;
/// every base read goes through the inspector and the cache. Runs on the main thread, once per hovered frame (W4: Unity null checks use <c>== null</c>).
/// </summary>
internal static class HoverLines
{
    private static HoverInspector? inspector;
    private static BaseCache? inspectorCache;

    /// <summary>Appends the chest line to the vanilla hover text of a container. Returns the text unchanged when BaseStorage does not manage the chest.</summary>
    /// <param name="container">The container being hovered.</param>
    /// <param name="text">The vanilla hover text.</param>
    /// <returns>The hover text with the chest line added, or <paramref name="text"/> unchanged.</returns>
    internal static string AppendChest(Container container, string text)
    {
        PluginWorld? world = PluginWorld.Current;
        BaseStorageSettings? settings = BaseStoragePlugin.Settings;
        if (world == null || settings == null || container == null)
        {
            return text;
        }

        ZNetView view = container.m_nview;
        if (view == null || !world.Containers.Contains(view))
        {
            return text;
        }

        ZDO zdo = view.GetZDO();
        if (zdo == null)
        {
            return text;
        }

        ZDOID uid = zdo.m_uid;
        var id = new ContainerId(uid.UserID, uid.ID);
        var at = container.transform.position;
        string encoded = zdo.GetString(PersistenceKeys.ChestCategory, string.Empty);
        string line = Inspector(world).ChestLine(
            id,
            new WorldPosition(at.x, at.y, at.z),
            encoded,
            new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
        string? appended = HoverInspector.Append(text, line);
        string result = appended == null ? text : appended;
        string name = ChestNames.Read(container);
        return name.Length == 0 ? result : result + "\n" + name;
    }

    /// <summary>Appends the station line to the vanilla hover text of a crafting station. Returns the text unchanged when the station is not registered.</summary>
    /// <param name="station">The crafting station being hovered.</param>
    /// <param name="text">The vanilla hover text.</param>
    /// <returns>The hover text with the station line added, or <paramref name="text"/> unchanged.</returns>
    internal static string AppendStation(CraftingStation station, string text)
    {
        PluginWorld? world = PluginWorld.Current;
        BaseStorageSettings? settings = BaseStoragePlugin.Settings;
        if (world == null || settings == null || station == null)
        {
            return text;
        }

        ZNetView view = station.m_nview;
        if (view == null || !world.Stations.Contains(station))
        {
            return text;
        }

        ZDO zdo = view.GetZDO();
        if (zdo == null)
        {
            return text;
        }

        ZDOID uid = zdo.m_uid;
        string line = Inspector(world).StationLine(
            new StationId(uid.UserID, uid.ID),
            new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
        string? appended = HoverInspector.Append(text, line);
        return appended == null ? text : appended;
    }

    private static HoverInspector Inspector(PluginWorld world)
    {
        HoverInspector? current = inspector;
        if (current == null || !ReferenceEquals(inspectorCache, world.Cache))
        {
            current = new HoverInspector(world.Cache);
            inspector = current;
            inspectorCache = world.Cache;
        }

        return current;
    }
}
