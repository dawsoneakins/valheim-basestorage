namespace BaseStorage.Core.BaseMove;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Compendium;
using BaseStorage.Core.Snapshots;

/// <summary>One base in the world, as the All bases list shows it.</summary>
public sealed class BaseEntry
{
    /// <summary>Gets or sets the base's lowest station id, which identifies it.</summary>
    public ContainerId Station { get; set; }

    /// <summary>Gets or sets the saved name, empty when it has none.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the biome the identifying station stands in, or null when it isn't a listed biome.</summary>
    public CompendiumBiome? Biome { get; set; }

    /// <summary>Gets or sets the X position of the identifying station.</summary>
    public float X { get; set; }

    /// <summary>Gets or sets the Z position of the identifying station.</summary>
    public float Z { get; set; }

    /// <summary>Gets or sets the number of chests.</summary>
    public int Chests { get; set; }

    /// <summary>Gets or sets the number of used chest slots.</summary>
    public int UsedSlots { get; set; }

    /// <summary>Gets or sets the number of chest slots.</summary>
    public int TotalSlots { get; set; }

    /// <summary>Gets or sets every station in the base.</summary>
    public IReadOnlyList<ContainerId> Stations { get; set; } = Array.Empty<ContainerId>();

    /// <summary>Gets or sets the name to show: the saved name, or one made up for an unnamed base. It isn't sent or saved.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets how full the chests are, from 0 to 100.</summary>
    public int FillPercent => TotalSlots <= 0 ? 0 : Math.Min(100, UsedSlots * 100 / TotalSlots);
}

/// <summary>A chest as the directory needs it.</summary>
public sealed class DirectoryChest
{
    /// <summary>Initializes a new instance of the <see cref="DirectoryChest"/> class.</summary>
    /// <param name="placement">A snapshot holding the chest's id and position.</param>
    /// <param name="usedSlots">The used slots.</param>
    /// <param name="totalSlots">The slots.</param>
    public DirectoryChest(ContainerSnapshot placement, int usedSlots, int totalSlots)
    {
        Placement = placement;
        UsedSlots = usedSlots;
        TotalSlots = totalSlots;
    }

    /// <summary>Gets the snapshot holding the chest's id and position.</summary>
    public ContainerSnapshot Placement { get; }

    /// <summary>Gets the used slots.</summary>
    public int UsedSlots { get; }

    /// <summary>Gets the slots.</summary>
    public int TotalSlots { get; }
}

/// <summary>Finds every base in the world from its stations and chests. A station with no chest in range isn't a base, so a campsite workbench doesn't clutter the list.</summary>
public static class BaseDirectory
{
    /// <summary>Finds the bases.</summary>
    /// <param name="stations">Every station, with its saved base name.</param>
    /// <param name="chests">Every chest.</param>
    /// <param name="maxStations">The largest base.</param>
    /// <returns>The bases, by identifying station.</returns>
    public static List<BaseEntry> Find(IReadOnlyList<KeyValuePair<StationSnapshot, string>> stations, IReadOnlyList<DirectoryChest> chests, int maxStations)
    {
        if (stations == null)
        {
            throw new ArgumentNullException(nameof(stations));
        }

        if (chests == null)
        {
            throw new ArgumentNullException(nameof(chests));
        }

        var all = stations.Select(s => s.Key).ToList();
        var names = new Dictionary<StationId, string>();
        foreach (KeyValuePair<StationSnapshot, string> station in stations)
        {
            names[station.Key.Id] = station.Value;
        }

        var placements = chests.Select(c => c.Placement).ToList();
        var byId = new Dictionary<ContainerId, DirectoryChest>();
        foreach (DirectoryChest chest in chests)
        {
            byId[chest.Placement.Id] = chest;
        }

        var seen = new HashSet<StationId>();
        var entries = new List<BaseEntry>();
        foreach (StationSnapshot start in all.OrderBy(s => s.Id.UserId).ThenBy(s => s.Id.Id))
        {
            if (seen.Contains(start.Id))
            {
                continue;
            }

            IReadOnlyList<StationSnapshot> members = StationGraph.FindBase(all, start.Id, maxStations).Stations;
            foreach (StationSnapshot member in members)
            {
                seen.Add(member.Id);
            }

            IReadOnlyList<ContainerSnapshot> inside = BaseMembership.FindMembers(members, placements);
            if (inside.Count == 0)
            {
                continue;
            }

            StationSnapshot lead = members.OrderBy(s => s.Id.UserId).ThenBy(s => s.Id.Id).First();
            entries.Add(new BaseEntry
            {
                Station = new ContainerId(lead.Id.UserId, lead.Id.Id),
                Name = members.Select(m => names[m.Id]).FirstOrDefault(n => n.Length > 0) ?? string.Empty,
                X = lead.Position.X,
                Z = lead.Position.Z,
                Chests = inside.Count,
                UsedSlots = inside.Sum(c => byId[c.Id].UsedSlots),
                TotalSlots = inside.Sum(c => byId[c.Id].TotalSlots),
                Stations = members.Select(m => new ContainerId(m.Id.UserId, m.Id.Id)).ToList(),
            });
        }

        return entries;
    }

    /// <summary>Gets a list ready to show: nearest first, with made-up names for unnamed bases, numbered per biome in a stable order.</summary>
    /// <param name="entries">The bases.</param>
    /// <param name="x">The player's X position.</param>
    /// <param name="z">The player's Z position.</param>
    /// <param name="fallbackName">Makes an unnamed base's name from its biome and number.</param>
    /// <returns>The list.</returns>
    public static List<BaseEntry> ForPlayer(IReadOnlyList<BaseEntry> entries, float x, float z, Func<CompendiumBiome?, int, string> fallbackName)
    {
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        if (fallbackName == null)
        {
            throw new ArgumentNullException(nameof(fallbackName));
        }

        var numbers = new Dictionary<int, int>();
        foreach (BaseEntry entry in entries.OrderBy(e => e.Station.UserId).ThenBy(e => e.Station.Id))
        {
            if (entry.Name.Length > 0)
            {
                entry.DisplayName = entry.Name;
                continue;
            }

            int key = entry.Biome.HasValue ? (int)entry.Biome.Value : -1;
            numbers.TryGetValue(key, out int number);
            numbers[key] = ++number;
            entry.DisplayName = fallbackName(entry.Biome, number);
        }

        return entries.OrderBy(e => DistanceSquaredTo(e, x, z)).ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Measures from a point to a base, squared, which is all that ordering needs.</summary>
    /// <param name="entry">The base.</param>
    /// <param name="x">The X position.</param>
    /// <param name="z">The Z position.</param>
    /// <returns>The squared distance.</returns>
    public static float DistanceSquaredTo(BaseEntry entry, float x, float z)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        float dx = entry.X - x;
        float dz = entry.Z - z;
        return (dx * dx) + (dz * dz);
    }
}
