namespace BaseStorage.Core.Tests.Compendium;

using System;
using System.Linq;
using BaseStorage.Core.Compendium;

/// <summary>The pages made from the game's data: their facts, their links and their search.</summary>
public sealed class CompendiumBuilderTests
{
    private static CompendiumData Data()
    {
        var data = new CompendiumData();
        data.Items.Add(new ItemRecord { Id = "Wood", Name = "Wood", Description = "Useful stuff", Category = ItemCategory.Material, Weight = 2f, MaxStack = 50 });
        data.Items.Add(new ItemRecord { Id = "Copper", Name = "Copper", Category = ItemCategory.Material, Teleportable = false });
        data.Items.Add(new ItemRecord { Id = "LeatherScraps", Name = "Leather scraps", Category = ItemCategory.Material });
        var club = new ItemRecord { Id = "Club", Name = "Club", Category = ItemCategory.Weapon, Durability = 100f, Weight = 1.5f };
        club.Damage.Add(new DamageAmount { Type = CompendiumDamage.Blunt, Amount = 12f });
        club.Damage.Add(new DamageAmount { Type = CompendiumDamage.Fire, Amount = 0f });
        data.Items.Add(club);
        data.Items.Add(new ItemRecord { Id = "Hidden", Name = string.Empty });

        var recipe = new RecipeRecord { ResultId = "Club", Amount = 1, Station = "Workbench", StationLevel = 2 };
        recipe.Ingredients.Add(new IngredientRecord { ItemId = "Wood", Amount = 10, AmountPerLevel = 5 });
        recipe.Ingredients.Add(new IngredientRecord { ItemId = "Unknown", Amount = 1 });
        data.Recipes.Add(recipe);

        var boar = new CreatureRecord { Id = "Boar", Name = "Boar", Faction = "Animals", Health = 10f };
        boar.Drops.Add(new CreatureDropRecord { ItemId = "LeatherScraps", Min = 1, Max = 2, Chance = 0.5f });
        boar.Responses.Add(new DamageResponseEntry { Type = CompendiumDamage.Fire, Response = DamageResponse.Weak });
        boar.Responses.Add(new DamageResponseEntry { Type = CompendiumDamage.Slash, Response = DamageResponse.Normal });
        data.Creatures.Add(boar);
        data.Spawns.Add(new SpawnRecord { CreatureId = "Boar", Biome = CompendiumBiome.Meadows, Day = true });
        data.Spawns.Add(new SpawnRecord { CreatureId = "Boar", Biome = CompendiumBiome.Meadows, Night = true });
        data.Spawns.Add(new SpawnRecord { CreatureId = "NoSuchCreature", Biome = CompendiumBiome.Meadows, Day = true });

        var tree = new ResourceRecord { Name = "Beech" };
        tree.Biomes.Add(CompendiumBiome.Meadows);
        tree.ItemIds.Add("Wood");
        tree.ItemIds.Add("NoSuchItem");
        data.Resources.Add(tree);

        var bench = new PieceRecord { Id = "piece_bench", Name = "Bench", Category = "Furniture", Comfort = 1, Station = "Workbench" };
        bench.Cost.Add(new IngredientRecord { ItemId = "Wood", Amount = 4 });
        data.Pieces.Add(bench);
        return data;
    }

    private static CompendiumKnowledge Everything(CompendiumData data)
    {
        var knowledge = new CompendiumKnowledge();
        knowledge.Items.UnionWith(data.Items.Select(i => i.Id));
        knowledge.Creatures.UnionWith(data.Creatures.Select(c => c.Id));
        knowledge.Pieces.UnionWith(data.Pieces.Select(p => p.Id));
        knowledge.Stations.Add("Workbench");
        foreach (CompendiumBiome biome in Enum.GetValues(typeof(CompendiumBiome)))
        {
            knowledge.Biomes.Add(biome);
        }

        return knowledge;
    }

    private static CompendiumBook Book() => CompendiumBuilder.Build(Data(), Everything(Data()));

    private static CompendiumPage Page(string id) => Book().Find(id) ?? throw new InvalidOperationException(id + " is missing");

    private static CompendiumLine[] Section(string pageId, string heading) =>
        Page(pageId).Sections.Single(s => s.Heading == heading).Lines.ToArray();

    [Fact]
    public void ANullDataSetIsRefused() => Assert.Throws<ArgumentNullException>(() => CompendiumBuilder.Build(null!, new CompendiumKnowledge()));

    [Fact]
    public void NullKnowledgeIsRefused() => Assert.Throws<ArgumentNullException>(() => CompendiumBuilder.Build(new CompendiumData(), null!));

    [Fact]
    public void AVisitedBiomeHasAPageEvenWithNoData()
    {
        var knowledge = new CompendiumKnowledge();
        foreach (CompendiumBiome biome in Enum.GetValues(typeof(CompendiumBiome)))
        {
            knowledge.Biomes.Add(biome);
        }

        CompendiumBook book = CompendiumBuilder.Build(new CompendiumData(), knowledge);

        Assert.Equal(Enum.GetValues(typeof(CompendiumBiome)).Length, book.Count);
        foreach (CompendiumBiome biome in Enum.GetValues(typeof(CompendiumBiome)))
        {
            CompendiumPage page = book.Find(CompendiumBuilder.BiomePage(biome))!;
            Assert.NotEmpty(page.Title);
            Assert.NotEmpty(page.Description);
        }
    }

    [Fact]
    public void AnItemWithNoNameGetsNoPage() => Assert.Null(Book().Find(CompendiumBuilder.ItemPage("Hidden")));

    [Fact]
    public void ZeroStatsAndEmptySectionsAreLeftOut()
    {
        CompendiumLine[] stats = Section(CompendiumBuilder.ItemPage("Club"), "Stats");

        Assert.Contains(stats, l => l.Text.Contains("Blunt", StringComparison.Ordinal));
        Assert.DoesNotContain(stats, l => l.Text.Contains("Fire", StringComparison.Ordinal));
        Assert.DoesNotContain(Page(CompendiumBuilder.ItemPage("Copper")).Sections, s => s.Heading == "How to make");
    }

    [Fact]
    public void AnItemThatCannotUseAPortalSaysSo() =>
        Assert.Contains(Section(CompendiumBuilder.ItemPage("Copper"), "Stats"), l => l.Text.Contains("portal", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void ARecipeNamesItsStationAndLinksKnownIngredientsOnly()
    {
        CompendiumLine[] crafting = Section(CompendiumBuilder.ItemPage("Club"), "How to make");

        Assert.Contains("Workbench", crafting[0].Text, StringComparison.Ordinal);
        Assert.Contains("2", crafting[0].Text, StringComparison.Ordinal);
        Assert.Equal(CompendiumBuilder.ItemPage("Wood"), crafting[1].Link);
        Assert.Contains("+5", crafting[1].Text, StringComparison.Ordinal);
        Assert.Null(crafting[2].Link);
    }

    [Fact]
    public void AnIngredientLinksBackToWhatItMakesAndBuilds()
    {
        CompendiumLine[] usedIn = Section(CompendiumBuilder.ItemPage("Wood"), "Used in");

        Assert.Contains(usedIn, l => l.Link == CompendiumBuilder.ItemPage("Club"));
        Assert.Contains(usedIn, l => l.Link == CompendiumBuilder.PiecePage("piece_bench"));
    }

    [Fact]
    public void ADropLinksBothWaysWithItsChance()
    {
        CompendiumLine drop = Assert.Single(Section(CompendiumBuilder.CreaturePage("Boar"), "Drops"));
        CompendiumLine source = Assert.Single(Section(CompendiumBuilder.ItemPage("LeatherScraps"), "Dropped by"));

        Assert.Equal(CompendiumBuilder.ItemPage("LeatherScraps"), drop.Link);
        Assert.Contains("1-2", drop.Text, StringComparison.Ordinal);
        Assert.Contains("50%", drop.Text, StringComparison.Ordinal);
        Assert.Equal(CompendiumBuilder.CreaturePage("Boar"), source.Link);
    }

    [Fact]
    public void OnlyDamageThatDiffersFromNormalIsListed()
    {
        CompendiumLine line = Assert.Single(Section(CompendiumBuilder.CreaturePage("Boar"), "Damage taken"));

        Assert.Contains("Fire", line.Text, StringComparison.Ordinal);
        Assert.Contains("Weak", line.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void DayAndNightSpawnsInABiomeJoinIntoOneLine()
    {
        CompendiumLine line = Assert.Single(Section(CompendiumBuilder.CreaturePage("Boar"), "Appears in"));

        Assert.Equal(CompendiumBuilder.BiomePage(CompendiumBiome.Meadows), line.Link);
        Assert.Contains("Day and night", line.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ABiomeListsItsCreaturesAndResourcesLeavingOutUnknownOnes()
    {
        string meadows = CompendiumBuilder.BiomePage(CompendiumBiome.Meadows);

        Assert.Equal(CompendiumBuilder.CreaturePage("Boar"), Assert.Single(Section(meadows, "Creatures")).Link);
        Assert.Equal(CompendiumBuilder.ItemPage("Wood"), Assert.Single(Section(meadows, "Resources")).Link);
        Assert.DoesNotContain(Page(meadows).Sections, section => section.Heading == "Places");
    }

    [Fact]
    public void AResourceNamesItsSourceAndBiome() =>
        Assert.Equal("Beech (Meadows)", Assert.Single(Section(CompendiumBuilder.ItemPage("Wood"), "Gathered from")).Text);

    [Fact]
    public void APieceListsItsCostAndComfort()
    {
        string bench = CompendiumBuilder.PiecePage("piece_bench");

        Assert.Equal(CompendiumBuilder.ItemPage("Wood"), Assert.Single(Section(bench, "Cost")).Link);
        Assert.Contains(Section(bench, "Stats"), l => l.Text.Contains("Comfort", StringComparison.Ordinal));
        Assert.Equal("Furniture", Page(bench).Group);
    }

    [Fact]
    public void EveryLinkInTheBookLeadsToAPage()
    {
        CompendiumBook book = Book();

        foreach (CompendiumKind kind in Enum.GetValues(typeof(CompendiumKind)))
        {
            foreach (CompendiumPage page in book.Search(kind, null))
            {
                foreach (CompendiumLine line in page.Sections.SelectMany(s => s.Lines).Where(l => l.Link != null))
                {
                    Assert.NotNull(book.Find(line.Link));
                }
            }
        }
    }

    [Fact]
    public void ADuplicateIdKeepsTheFirstPage()
    {
        CompendiumData data = Data();
        data.Items.Add(new ItemRecord { Id = "Wood", Name = "Second wood" });

        Assert.Equal("Wood", CompendiumBuilder.Build(data, Everything(data)).Find(CompendiumBuilder.ItemPage("Wood"))!.Title);
    }

    [Fact]
    public void SearchIsCaseInsensitiveNeedsEveryWordAndStaysInItsKind()
    {
        CompendiumBook book = Book();

        Assert.Equal("Wood", Assert.Single(book.Search(CompendiumKind.Item, "USEFUL stuff")).Title);
        Assert.Empty(book.Search(CompendiumKind.Item, "useful boar"));
        Assert.Empty(book.Search(CompendiumKind.Creature, "wood"));
    }

    [Fact]
    public void SearchPutsTitlesThatStartWithTheQueryFirst()
    {
        CompendiumData data = Data();
        data.Items.Add(new ItemRecord { Id = "AncientBark", Name = "Ancient bark", Description = "Old wood" });

        Assert.Equal(new[] { "Wood", "Ancient bark" }, CompendiumBuilder.Build(data, Everything(data)).Search(CompendiumKind.Item, "wood").Select(p => p.Title));
    }

    [Fact]
    public void BiomesKeepJourneyOrderUntilSearched()
    {
        var titles = Book().Search(CompendiumKind.Biome, string.Empty).Select(p => p.Title).ToArray();

        Assert.Equal("Meadows", titles[0]);
        Assert.Equal("Black Forest", titles[1]);
    }

    [Fact]
    public void WithNothingDiscoveredTheBookIsEmpty()
    {
        CompendiumBook book = CompendiumBuilder.Build(Data(), new CompendiumKnowledge());

        Assert.Equal(0, book.Count);
        foreach (CompendiumKind kind in Enum.GetValues(typeof(CompendiumKind)))
        {
            Assert.Empty(book.Search(kind, null));
        }
    }

    [Fact]
    public void AnUndiscoveredThingCannotBeSearchedFor()
    {
        CompendiumData data = Data();
        CompendiumKnowledge knowledge = Everything(data);
        knowledge.Items.Remove("Copper");

        CompendiumBook book = CompendiumBuilder.Build(data, knowledge);

        Assert.Null(book.Find(CompendiumBuilder.ItemPage("Copper")));
        Assert.Empty(book.Search(CompendiumKind.Item, "copper"));
    }

    [Fact]
    public void ARecipeHidesUndiscoveredIngredientsAndStationsButStillCountsThem()
    {
        CompendiumData data = Data();
        CompendiumKnowledge knowledge = Everything(data);
        knowledge.Items.Remove("Wood");
        knowledge.Stations.Clear();

        CompendiumLine[] lines = CompendiumBuilder.Build(data, knowledge).Find(CompendiumBuilder.ItemPage("Club"))!
            .Sections.Single(section => section.Heading == "How to make").Lines.ToArray();

        Assert.DoesNotContain("Workbench", lines[0].Text, StringComparison.Ordinal);
        Assert.Contains("undiscovered", lines[0].Text, StringComparison.Ordinal);
        Assert.StartsWith("Undiscovered item x10", lines[1].Text, StringComparison.Ordinal);
        Assert.Null(lines[1].Link);
    }

    [Fact]
    public void NoPageNamesAnUndiscoveredThing()
    {
        CompendiumData data = Data();
        CompendiumKnowledge knowledge = Everything(data);
        knowledge.Items.Remove("Club");
        knowledge.Items.Remove("LeatherScraps");
        knowledge.Pieces.Clear();
        knowledge.Creatures.Clear();
        knowledge.Items.Add("Wood");

        CompendiumBook book = CompendiumBuilder.Build(data, knowledge);
        string everyLine = string.Join(
            "\n",
            Enum.GetValues(typeof(CompendiumKind)).Cast<CompendiumKind>().SelectMany(kind => book.Search(kind, null))
                .SelectMany(page => page.Sections).SelectMany(section => section.Lines).Select(line => line.Text));

        Assert.DoesNotContain("Club", everyLine, StringComparison.Ordinal);
        Assert.DoesNotContain("Leather", everyLine, StringComparison.Ordinal);
        Assert.DoesNotContain("Bench", everyLine, StringComparison.Ordinal);
        Assert.DoesNotContain("Boar", everyLine, StringComparison.Ordinal);
        Assert.DoesNotContain(book.Find(CompendiumBuilder.ItemPage("Wood"))!.Sections, section => section.Heading == "Used in");
    }

    [Fact]
    public void BiomesTheyHaveNotVisitedAreLeftOutOfEveryLine()
    {
        CompendiumData data = Data();
        data.Resources[0].Biomes.Add(CompendiumBiome.Plains);
        data.Spawns.Add(new SpawnRecord { CreatureId = "Boar", Biome = CompendiumBiome.Plains, Day = true });
        CompendiumKnowledge knowledge = Everything(data);
        knowledge.Biomes.Remove(CompendiumBiome.Plains);

        CompendiumBook book = CompendiumBuilder.Build(data, knowledge);

        Assert.Null(book.Find(CompendiumBuilder.BiomePage(CompendiumBiome.Plains)));
        Assert.Equal("Beech (Meadows)", Assert.Single(book.Find(CompendiumBuilder.ItemPage("Wood"))!.Sections.Single(s => s.Heading == "Gathered from").Lines).Text);
        Assert.Single(book.Find(CompendiumBuilder.CreaturePage("Boar"))!.Sections.Single(s => s.Heading == "Appears in").Lines);
    }

    [Fact]
    public void ASourceOnlyInUnvisitedBiomesIsNotListed()
    {
        CompendiumData data = Data();
        CompendiumKnowledge knowledge = Everything(data);
        knowledge.Biomes.Remove(CompendiumBiome.Meadows);

        CompendiumBook book = CompendiumBuilder.Build(data, knowledge);

        Assert.DoesNotContain(book.Find(CompendiumBuilder.ItemPage("Wood"))!.Sections, s => s.Heading == "Gathered from");
    }

    [Fact]
    public void AKnownTrophyRevealsItsCreature()
    {
        CompendiumData data = Data();
        data.Items.Add(new ItemRecord { Id = "TrophyBoar", Name = "Boar trophy", Category = ItemCategory.Trophy });
        data.Creatures[0].Drops.Add(new CreatureDropRecord { ItemId = "TrophyBoar", Min = 1, Max = 1, Chance = 0.1f });
        var knowledge = new CompendiumKnowledge();
        knowledge.Items.Add("TrophyBoar");

        Assert.NotNull(CompendiumBuilder.Build(data, knowledge).Find(CompendiumBuilder.CreaturePage("Boar")));
    }

    [Fact]
    public void KnowingAnOrdinaryDropDoesNotRevealItsCreature()
    {
        var knowledge = new CompendiumKnowledge();
        knowledge.Items.Add("LeatherScraps");

        Assert.Null(CompendiumBuilder.Build(Data(), knowledge).Find(CompendiumBuilder.CreaturePage("Boar")));
    }

    [Fact]
    public void SightingsSurviveSavingAndFillTheKnowledge()
    {
        var sightings = new CompendiumSightings(null);
        Assert.True(sightings.AddCreature("Boar"));
        Assert.False(sightings.AddCreature("Boar"));
        Assert.True(sightings.AddBiome(CompendiumBiome.Swamp));
        Assert.False(sightings.AddBiome(CompendiumBiome.Swamp));
        var knowledge = new CompendiumKnowledge();

        new CompendiumSightings(sightings.ToSaved()).CopyTo(knowledge);

        Assert.Contains("Boar", knowledge.Creatures);
        Assert.Contains(CompendiumBiome.Swamp, knowledge.Biomes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a;b")]
    public void ABadCreatureNameIsNotRecorded(string? name) => Assert.False(new CompendiumSightings(null).AddCreature(name));

    [Fact]
    public void DamagedSavedSightingsAreIgnoredRatherThanFailing()
    {
        var knowledge = new CompendiumKnowledge();

        new CompendiumSightings(";;biome:Nowhere;biome:99;Boar;").CopyTo(knowledge);

        Assert.Empty(knowledge.Biomes);
        Assert.Equal("Boar", Assert.Single(knowledge.Creatures));
    }

    [Fact]
    public void BiomeDescriptionsGiveNothingAway()
    {
        var knowledge = new CompendiumKnowledge();
        foreach (CompendiumBiome biome in Enum.GetValues(typeof(CompendiumBiome)))
        {
            knowledge.Biomes.Add(biome);
        }

        CompendiumBook book = CompendiumBuilder.Build(new CompendiumData(), knowledge);
        string[] spoilers = { "copper", "tin", "iron", "silver", "greydwarf", "fuling", "deathsquito", "seeker", "giant" };

        foreach (CompendiumPage page in book.Search(CompendiumKind.Biome, null))
        {
            foreach (string spoiler in spoilers)
            {
                Assert.DoesNotContain(spoiler, page.Description, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("Pickable_Barley1", "Barley")]
    [InlineData("MineRock_Copper", "Copper")]
    [InlineData("FirTree_small", "Fir Tree small")]
    [InlineData("Beech1", "Beech")]
    [InlineData("Crypt3", "Crypt")]
    [InlineData("Rock (Clone)", "Rock")]
    [InlineData("123", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void PrettyMakesPrefabNamesReadable(string? prefab, string expected) =>
        Assert.Equal(expected, CompendiumBuilder.Pretty(prefab));
}
