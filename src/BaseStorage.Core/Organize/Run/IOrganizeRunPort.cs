namespace BaseStorage.Core.Organize.Run;

using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// What the run coordinator asks the game for (contract section 3). The Plugin implements it over the loaded world, the
/// reservation driver, the vanilla popup and the one notice helper; the Core tests implement it with a recording fake. The port
/// takes no decision: it reads facts, builds the layer and shows what it is told.
/// </summary>
public interface IOrganizeRunPort
{
    /// <summary>Gets a value that is true when a world exists and a local session is known.</summary>
    bool InWorld { get; }

    /// <summary>Gets the local player's game id; 0 when there is no local player.</summary>
    long HolderId { get; }

    /// <summary>Gets the local player's display label; never blank (the fallback label when the name is unusable).</summary>
    string HolderName { get; }

    /// <summary>Gets the category catalog the plan is made with.</summary>
    CategoryCatalog Catalog { get; }

    /// <summary>Gets the item kind per prefab name; a missing prefab is Other.</summary>
    IReadOnlyDictionary<string, ItemKind> ItemKinds { get; }

    /// <summary>Reads the persisted assignments and the in-use labels of the base's chests, once per run.</summary>
    /// <param name="containers">The member chests of the base.</param>
    /// <returns>The facts of those chests.</returns>
    OrganizeBaseFacts GatherFacts(IReadOnlyList<ContainerSnapshot> containers);

    /// <summary>Creates the game layer the executor writes through, over the chests of the last <see cref="GatherFacts"/>.</summary>
    /// <param name="plan">The plan about to run.</param>
    /// <returns>The layer.</returns>
    IContainerLayer CreateLayer(OrganizePlan plan);

    /// <summary>Shows the confirm popup; the Plugin later calls Confirm or Cancel on the coordinator.</summary>
    /// <param name="header">The popup header, already resolved from the strings table.</param>
    /// <param name="body">The preview body, already resolved from the strings table.</param>
    void ShowPreview(string header, string body);

    /// <summary>Saves the categories a finished run settled on (S-A-003). A chest that cannot be written is skipped and logged.</summary>
    /// <param name="rules">The chests and their categories.</param>
    void SaveCategoryRules(IReadOnlyList<ChestAutoAssignment> rules);

    /// <summary>Shows one MessageHud line whose text comes from the strings table.</summary>
    /// <param name="placement">Where the notice appears.</param>
    /// <param name="key">The strings-table key.</param>
    /// <param name="args">The placeholder values; never null, possibly empty.</param>
    void ShowNotice(OrganizeNoticePlacement placement, string key, object?[] args);

    /// <summary>Captures the local player's inventory, every occupied slot in the game's list order; null when there is no player or a stack cannot be mapped.</summary>
    /// <returns>The snapshot, or null.</returns>
    PlayerInventorySnapshot? CaptureInventory();

    /// <summary>Gets the local player's mod favourites; FavouriteSlots.Empty when there are none or no player.</summary>
    FavouriteSlots Favourites { get; }

    /// <summary>Creates the game layer of one Deposit: the chests of the last <see cref="GatherFacts"/> plus the player inventory as PlayerInventoryId == plan.Player.Id.</summary>
    /// <param name="plan">The Deposit plan about to run.</param>
    /// <returns>The layer.</returns>
    IContainerLayer CreateDepositLayer(DepositPlan plan);
}
