namespace BaseStorage.Core.Tests.BaseMove;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Bases;
using BaseStorage.Core.Compendium;
using BaseStorage.Core.Snapshots;

/// <summary>Finding every base in the world, and listing them for a player.</summary>
public sealed class BaseDirectoryTests
{
    private static KeyValuePair<StationSnapshot, string> Station(uint id, float x, string name = "") =>
        new KeyValuePair<StationSnapshot, string>(new StationSnapshot(new StationId(1L, id), new WorldPosition(x, 0f, 0f), 20f), name);

    private static DirectoryChest Chest(uint id, float x, int used, int total) =>
        new DirectoryChest(new ContainerSnapshot(new ContainerId(2L, id), new WorldPosition(x, 0f, 0f), 1, 1, Array.Empty<ItemStackSnapshot>(), ContainerFlags.None), used, total);

    private static string Fallback(CompendiumBiome? biome, int number) => (biome.HasValue ? biome.Value.ToString() : "Unknown") + " " + number;

    [Fact]
    public void FarApartStationsAreSeparateBasesWithTheirOwnTotals()
    {
        var entries = BaseDirectory.Find(
            new[] { Station(1, 0f, "Home"), Station(2, 5000f) },
            new[] { Chest(1, 1f, 10, 32), Chest(2, 2f, 6, 32), Chest(3, 5001f, 1, 8) },
            20);

        Assert.Equal(2, entries.Count);
        BaseEntry home = entries.Single(e => e.Name == "Home");
        Assert.Equal(2, home.Chests);
        Assert.Equal(16, home.UsedSlots);
        Assert.Equal(64, home.TotalSlots);
        Assert.Equal(25, home.FillPercent);
        Assert.Equal(1, entries.Single(e => e.Name.Length == 0).Chests);
    }

    [Fact]
    public void AStationWithNoChestIsNotABase() =>
        Assert.Empty(BaseDirectory.Find(new[] { Station(1, 0f, "Camp") }, Array.Empty<DirectoryChest>(), 20));

    [Fact]
    public void LinkedStationsAreOneBaseTheNameOfAnyOfThemCountsAndTheLowestIdLeads()
    {
        var entries = BaseDirectory.Find(new[] { Station(2, 10f, "Keep"), Station(1, 0f) }, new[] { Chest(1, 1f, 0, 8) }, 20);

        BaseEntry entry = Assert.Single(entries);
        Assert.Equal("Keep", entry.Name);
        Assert.Equal(new ContainerId(1L, 1u), entry.Station);
        Assert.Equal(2, entry.Stations.Count);
    }

    [Fact]
    public void ANullInputIsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => BaseDirectory.Find(null!, Array.Empty<DirectoryChest>(), 20));
        Assert.Throws<ArgumentNullException>(() => BaseDirectory.Find(Array.Empty<KeyValuePair<StationSnapshot, string>>(), null!, 20));
        Assert.Throws<ArgumentNullException>(() => BaseDirectory.ForPlayer(null!, 0f, 0f, Fallback));
        Assert.Throws<ArgumentNullException>(() => BaseDirectory.ForPlayer(Array.Empty<BaseEntry>(), 0f, 0f, null!));
    }

    [Fact]
    public void TheListIsNearestFirst()
    {
        var entries = new[]
        {
            new BaseEntry { Station = new ContainerId(1L, 1u), Name = "Far", X = 900f },
            new BaseEntry { Station = new ContainerId(1L, 2u), Name = "Near", X = 10f },
        };

        Assert.Equal(new[] { "Near", "Far" }, BaseDirectory.ForPlayer(entries, 0f, 0f, Fallback).Select(e => e.DisplayName));
    }

    [Fact]
    public void UnnamedBasesAreNumberedPerBiomeAndKeepTheirNumberWhereverThePlayerStands()
    {
        BaseEntry[] Entries() => new[]
        {
            new BaseEntry { Station = new ContainerId(1L, 1u), Biome = CompendiumBiome.Swamp, X = 900f },
            new BaseEntry { Station = new ContainerId(1L, 2u), Biome = CompendiumBiome.Swamp, X = 10f },
            new BaseEntry { Station = new ContainerId(1L, 3u), Biome = CompendiumBiome.Plains, X = 20f },
            new BaseEntry { Station = new ContainerId(1L, 4u), Biome = null, X = 30f },
        };

        BaseEntry[] here = Entries();
        BaseDirectory.ForPlayer(here, 0f, 0f, Fallback);
        BaseEntry[] there = Entries();
        BaseDirectory.ForPlayer(there, 900f, 0f, Fallback);

        Assert.Equal(new[] { "Swamp 1", "Swamp 2", "Plains 1", "Unknown 1" }, here.Select(e => e.DisplayName));
        Assert.Equal(here.Select(e => e.DisplayName), there.Select(e => e.DisplayName));
    }

    [Fact]
    public void ABaseWithNoSlotsReadsAsEmptyRatherThanFailing() => Assert.Equal(0, new BaseEntry().FillPercent);

    [Fact]
    public void BasesSurviveTheWire()
    {
        var sent = new MoveMessage
        {
            Kind = MoveMessageKind.ListReply,
            DestinationStation = new ContainerId(5L, 6u),
            Bases = new[]
            {
                new BaseEntry
                {
                    Station = new ContainerId(-7L, 8u),
                    Name = "Swamp; Keep|1,2:3",
                    Biome = CompendiumBiome.Swamp,
                    X = -12.4f,
                    Z = 90.6f,
                    Chests = 3,
                    UsedSlots = 4,
                    TotalSlots = 5,
                    Stations = new[] { new ContainerId(-7L, 8u), new ContainerId(9L, 10u) },
                },
                new BaseEntry { Station = new ContainerId(1L, 2u), Stations = new[] { new ContainerId(1L, 2u) } },
            },
        };

        MoveMessage back = MoveMessage.Decode(sent.Encode())!;

        Assert.Equal(new ContainerId(5L, 6u), back.DestinationStation);
        Assert.Equal(2, back.Bases.Count);
        BaseEntry first = back.Bases[0];
        Assert.Equal("Swamp; Keep|1,2:3", first.Name);
        Assert.Equal(CompendiumBiome.Swamp, first.Biome);
        Assert.Equal(new ContainerId(-7L, 8u), first.Station);
        Assert.Equal(-12f, first.X);
        Assert.Equal(91f, first.Z);
        Assert.Equal(3, first.Chests);
        Assert.Equal(4, first.UsedSlots);
        Assert.Equal(5, first.TotalSlots);
        Assert.Equal(new[] { new ContainerId(-7L, 8u), new ContainerId(9L, 10u) }, first.Stations);
        Assert.Null(back.Bases[1].Biome);
        Assert.Equal(string.Empty, back.Bases[1].Name);
    }
}
