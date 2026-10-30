namespace BaseStorage.Core.Tests.ChestCategories;

using BaseStorage.Core.Categories;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Core.Tests.Reservations;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Reservations;

/// <summary>Fixtures of the S-E004-003 tests: the contract 5.1 catalogs and the contract 6.1 driver setup. Not a test row.</summary>
internal static class ChestCategoryFixtures
{
    /// <summary>The 14 shipped category names of spec AC-4.1, in catalog order.</summary>
    internal static readonly string[] DefaultNames =
    [
        "Metals & Ores", "Wood", "Stone & Minerals", "Hides & Leather", "Fibres & Cloth", "Raw Food", "Cooked Food",
        "Meads & Potions", "Seeds & Saplings", "Monster Drops", "Trophies", "Gear", "Ammo", "Misc",
    ];

    /// <summary>A catalog with the three categories Metals &amp; Ores, Wood, Stone (contract 5.1 catalog C3).</summary>
    internal static CategoryCatalog Three() => CategoryFile.Parse("Metals & Ores: CopperOre\nWood: Wood\nStone: Stone").Catalog;

    /// <summary>A catalog with the single category Wood (catalog C1).</summary>
    internal static CategoryCatalog One() => CategoryFile.Parse("Wood: Wood").Catalog;

    /// <summary>A catalog with the 14 default names, each mapping one prefab (catalog C14).</summary>
    internal static CategoryCatalog Fourteen()
    {
        var lines = new List<string>();
        for (int i = 0; i < DefaultNames.Length; i++)
        {
            lines.Add(DefaultNames[i] + ": Prefab" + i);
        }

        return CategoryFile.Parse(string.Join("\n", lines)).Catalog;
    }

    /// <summary>Shorthand for a named assignment.</summary>
    /// <param name="name">The category name.</param>
    /// <returns>The assignment.</returns>
    internal static CategoryAssignment Cat(string name) => CategoryAssignment.ForCategory(name);

    /// <summary>
    /// Builds the contract 6.1 setup: the S-E005-002 fixture (local peer Me, one chest 3141592653:42 owned by Erik, manual clock at 3600 s)
    /// plus a recording storage, a result list and a fresh write driver with a 1500 ms timeout.
    /// </summary>
    /// <param name="isServer">True for the host or single-player setups.</param>
    /// <param name="timeoutMs">The reservation timeout.</param>
    /// <returns>The pieces of the setup.</returns>
    internal static Setup Fresh(bool isServer = false, int timeoutMs = 1500)
    {
        var (game, chest, clock, sink, reservations) = ChestAccessFixtures.Fresh(isServer, timeoutMs);
        var storage = new FakeChestCategoryStorage();
        var results = new List<CategoryWriteResult>();
        var driver = new ChestCategoryWriteDriver<FakeChest>(reservations, storage, clock, timeoutMs, results.Add);
        return new Setup(game, chest, clock, reservations, storage, results, driver);
    }

    /// <summary>The pieces of one driver setup.</summary>
    internal sealed record Setup(
        FakeReservationGame Game,
        FakeChest Chest,
        ManualClock Clock,
        ReservationDriver<FakeChest> Reservations,
        FakeChestCategoryStorage Storage,
        List<CategoryWriteResult> Results,
        ChestCategoryWriteDriver<FakeChest> Driver);
}
