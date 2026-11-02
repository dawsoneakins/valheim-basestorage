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
        Rows(new Dictionary<ContainerId, CategoryAssignment>(), chests);

    private static IReadOnlyList<BaseMenuRow> Rows(Dictionary<ContainerId, CategoryAssignment> assignments, params ContainerSnapshot[] chests) =>
        BaseMenuBuilder.Rows(chests, assignments, new WorldPosition(0f, 0f, 0f), null, StringTable.English);

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
    public void TheExactNameComesFirstEvenWhenThereIsLess()
    {
        BaseMenuRow row = Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Copper", 2), S("CopperOre", 9))), "copper", NameOf));

        Assert.Equal(new[] { "Copper", "Copper ore" }, row.Hits.Select(hit => hit.Name));
    }

    [Fact]
    public void AmongEqualMatchesTheLargestHitComesFirst()
    {
        BaseMenuRow row = Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Copper", 2), S("CopperOre", 9))), "cop", NameOf));

        Assert.Equal(new[] { "Copper ore", "Copper" }, row.Hits.Select(hit => hit.Name));
    }

    [Fact]
    public void ABetterMatchBeatsAChestHoldingMore()
    {
        IReadOnlyList<BaseMenuRow> found = BaseSearch.Apply(Rows(C(1, 1f, 0f, 4, S("FineWood", 40)), C(2, 2f, 0f, 4, S("Wood", 2))), "wood", NameOf);

        Assert.Equal(new[] { Id(2), Id(1) }, found.Select(row => row.Chest.Id));
    }

    [Theory]
    [InlineData("ore copper")]
    [InlineData("ORE   cop")]
    public void SeveralWordsMatchInAnyOrder(string query) =>
        Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("CopperOre", 5), S("Copper", 5))), query, NameOf)[0].Hits);

    [Fact]
    public void AnItemIsFoundByItsCategoryAfterItsName()
    {
        string CategoryOf(string prefab) => prefab == "Wood" ? "Timber" : "Metals & Ores";
        IReadOnlyList<BaseMenuRow> found = BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Wood", 9)), C(2, 0f, 0f, 4, S("Copper", 1))), "metals", NameOf, CategoryOf);

        Assert.Equal(Id(2), Assert.Single(found).Chest.Id);
    }

    [Fact]
    public void AChestIsFoundByItsCategoryEvenWhenEmpty()
    {
        IReadOnlyList<BaseMenuRow> found = BaseSearch.Apply(
            Rows(
                Assign((1, CategoryAssignment.ForCategory("Raw Food")), (2, CategoryAssignment.ForCategory("Cooked Food")), (3, CategoryAssignment.ForCategory("Wood"))),
                C(1, 1f, 0f, 4),
                C(2, 2f, 0f, 4, S("Wood", 5)),
                C(3, 3f, 0f, 4, S("Wood", 5))),
            "food",
            NameOf);

        Assert.Equal(new[] { Id(2), Id(1) }, found.Select(row => row.Chest.Id));
        Assert.All(found, row => Assert.Empty(row.Hits));
    }

    [Fact]
    public void AChestOfTheCategoryComesBeforeAStrayItemOfThatCategory()
    {
        string CategoryOf(string prefab) => "Raw Food";
        IReadOnlyList<BaseMenuRow> found = BaseSearch.Apply(
            Rows(
                Assign((2, CategoryAssignment.ForCategory("Raw Food"))),
                C(1, 1f, 0f, 4, S("Honey", 50)),
                C(2, 2f, 0f, 4)),
            "food",
            NameOf,
            CategoryOf);

        Assert.Equal(new[] { Id(2), Id(1) }, found.Select(row => row.Chest.Id));
    }

    [Fact]
    public void AnAutoChestIsNotFoundByItsLabel() =>
        Assert.Empty(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4)), "auto", NameOf));

    [Fact]
    public void AKeepChestIsFoundByItsLabel() =>
        Assert.Single(BaseSearch.Apply(Rows(Assign((1, CategoryAssignment.Keep)), C(1, 0f, 0f, 4)), "keep", NameOf));

    [Fact]
    public void AHitNamesItsPrefabForTheIcon() =>
        Assert.Equal("CopperOre", BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("CopperOre", 5))), "ore", NameOf)[0].Hits[0].Prefab);

    [Fact]
    public void ItemsListsEachFoundItemOnce()
    {
        IReadOnlyList<BaseMenuRow> found = BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Copper", 5)), C(2, 0f, 0f, 4, S("Copper", 5), S("CopperOre", 1))), "copper", NameOf);

        Assert.Equal(new[] { "Copper", "Copper ore" }, BaseSearch.Items(found));
    }

    [Theory]
    [InlineData("Copper ore", "copper", "[Copper] ore")]
    [InlineData("Copper ore", "ore cop", "[Cop]per [ore]")]
    [InlineData("Copper ore", "per o", "Cop[per o]re")]
    [InlineData("Wood", "metals", "Wood")]
    public void MarkWrapsTheMatchedParts(string name, string query, string expected) =>
        Assert.Equal(expected, BaseSearch.Mark(name, query, "[", "]"));

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
    public void TheHitsOfAnItemInManyStacksAddUp()
    {
        BaseSearchHit hit = Assert.Single(BaseSearch.Apply(Rows(C(1, 0f, 0f, 4, S("Wood", 5), S("Wood", 7), S("Wood", 1))), "wood", NameOf)[0].Hits);

        Assert.Equal(13, hit.Count);
    }

    [Fact]
    public void AnOrdinaryRowHasNoHits() =>
        Assert.Empty(Rows(C(1, 0f, 0f, 4, S("Wood", 5)))[0].Hits);
}
