namespace BaseStorage.Core.Tests.Menu;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Menu;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>Finding an item across the base's chests.</summary>
public sealed class BaseSearchTests
{
    private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
    {
        ["CopperOre"] = "Copper ore",
        ["Copper"] = "Copper",
        ["Wood"] = "Wood",
    };

    private static string NameOf(string prefab) => Names.TryGetValue(prefab, out string? name) ? name : prefab;

    private static IReadOnlyList<BaseMenuRow> Rows(params ContainerSnapshot[] chests) =>
        BaseMenuBuilder.Rows(chests, new Dictionary<ContainerId, CategoryAssignment>(), new WorldPosition(0f, 0f, 0f), null, StringTable.English);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTextKeepsEveryRow(string query)
    {
        IReadOnlyList<BaseMenuRow> rows = Rows(C(1, 0f, 0f, 4, S("Wood", 5)), C(2, 0f, 0f, 4));

        Assert.Same(rows, BaseSearch.Apply(rows, query, NameOf));
    }

    [Theory]
    [InlineData("copper ore")]
    [InlineData("COPPER")]
    [InlineData("  ore  ")]
    public void AnyPartOfTheNameMatchesInAnyCase(string query) =>
        Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("CopperOre", 5)), C(2, 0f, 0f, 4, S("Wood", 5))), query, NameOf));

    [Fact]
    public void ChestsWithoutTheItemAreDropped() =>
        Assert.Empty(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Wood", 5))), "stone", NameOf));

    [Fact]
    public void StacksOfOneItemCountAsOneHit()
    {
        BaseMenuRow row = Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Wood", 5), S("Wood", 7))), "wood", NameOf));

        BaseSearchHit hit = Assert.Single(row.Hits);
        Assert.Equal("Wood", hit.Name);
        Assert.Equal(12, hit.Count);
    }

    [Fact]
    public void TheLargestHitComesFirst()
    {
        BaseMenuRow row = Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Copper", 2), S("CopperOre", 9))), "copper", NameOf));

        Assert.Equal(new[] { "Copper ore", "Copper" }, row.Hits.Select(hit => hit.Name));
    }

    [Fact]
    public void TheChestHoldingMostComesFirst()
    {
        IReadOnlyList<BaseMenuRow> found = BaseSearch.Apply(Rows(C(1, 1f, 0f, 4, S("Wood", 2)), C(2, 9f, 0f, 4, S("Wood", 40))), "wood", NameOf);

        Assert.Equal(new[] { Id(2), Id(1) }, found.Select(row => row.Chest.Id));
    }

    [Fact]
    public void ChestsHoldingTheSameAmountKeepTheirOrder()
    {
        IReadOnlyList<BaseMenuRow> rows = Rows(C(1, 1f, 0f, 4, S("Wood", 5)), C(2, 2f, 0f, 4, S("Wood", 5)), C(3, 3f, 0f, 4, S("Wood", 5)));

        Assert.Equal(rows.Select(row => row.Chest.Id), BaseSearch.Apply(rows, "wood", NameOf).Select(row => row.Chest.Id));
    }

    [Fact]
    public void TheTotalAddsEveryChest()
    {
        IReadOnlyList<BaseMenuRow> found = BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Wood", 5)), C(2, 0f, 0f, 4, S("Wood", 7))), "wood", NameOf);

        Assert.Equal(12, BaseSearch.Total(found));
    }

    [Fact]
    public void AnItemWithoutADisplayNameIsFoundByItsPrefab() =>
        Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("ModdedThing", 1))), "modded", NameOf));

    [Fact]
    public void AnOrdinaryRowHasNoHits() =>
        Assert.Empty(Rows(C(1, 0f, 0f, 4, S("Wood", 5)))[0].Hits);
}
