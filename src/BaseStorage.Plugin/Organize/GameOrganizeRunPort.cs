namespace BaseStorage.Plugin.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Categories.Defaults;
using BaseStorage.Core.ChestAccess;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Deposit;
using BaseStorage.Plugin.Favourites;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;

/// <summary>
/// The game port of the run coordinator (contract section 5): reads the local player, the persisted assignments and the in-use
/// labels of the base's chests, builds the game layer over them, and shows the preview and the notices it is told to show. It takes
/// no decision of its own. Main thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal sealed class GameOrganizeRunPort : IOrganizeRunPort
{
    private static readonly Dictionary<string, ItemKind> NoKinds = new Dictionary<string, ItemKind>(StringComparer.Ordinal);

    private readonly PluginWorld world;
    private readonly ReservationDriver<Container> reservations;
    private readonly List<Container> scratch = new List<Container>();
    private readonly Dictionary<ContainerId, Container> containers = new Dictionary<ContainerId, Container>();
    private Dictionary<string, ItemKind>? kinds;

    /// <summary>Creates the port over the plugin world and the reservation driver.</summary>
    /// <param name="world">The plugin world.</param>
    /// <param name="reservations">The reservation driver.</param>
    internal GameOrganizeRunPort(PluginWorld world, ReservationDriver<Container> reservations)
    {
        this.world = world;
        this.reservations = reservations;
    }

    /// <summary>Gets a value that is true when a world exists and a local session is known.</summary>
    public bool InWorld => ZNet.instance != null && ZDOMan.GetSessionID() != 0;

    /// <summary>Gets the local player id from the player profile; 0 when there is none.</summary>
    public long HolderId
    {
        get
        {
            PlayerProfile? profile = Profile();
            return profile == null ? 0L : profile.GetPlayerID();
        }
    }

    /// <summary>Gets the local player's display label, or the fallback label when the name is unusable.</summary>
    public string HolderName
    {
        get
        {
            PlayerProfile? profile = Profile();
            return ChestAccessRules.HolderLabel(profile == null ? null : profile.GetName(), StringTable.English);
        }
    }

    /// <summary>Gets the catalog the plan is made with: the configured one, the same source as the Category button and the tooltip.</summary>
    public CategoryCatalog Catalog => PluginCategoryCatalog.Current;

    /// <summary>Gets the item kind per prefab name, built once from the item database when it holds items.</summary>
    public IReadOnlyDictionary<string, ItemKind> ItemKinds
    {
        get
        {
            Dictionary<string, ItemKind>? built = kinds;
            if (built != null)
            {
                return built;
            }

            ObjectDB db = ObjectDB.instance;
            if (db == null || db.m_items == null || db.m_items.Count == 0)
            {
                return NoKinds;
            }

            List<CategoryItem> items = ObjectDbCategoryItems.Collect(db);
            built = new Dictionary<string, ItemKind>(items.Count, StringComparer.Ordinal);
            for (int i = 0; i < items.Count; i++)
            {
                built[items[i].PrefabName] = items[i].Kind;
            }

            kinds = built;
            return built;
        }
    }

    /// <summary>Maps the base's snapshots to their loaded containers and reads each chest's assignment and in-use label.</summary>
    /// <param name="containers">The member chests of the base.</param>
    /// <returns>The facts.</returns>
    public OrganizeBaseFacts GatherFacts(IReadOnlyList<ContainerSnapshot> containers)
    {
        this.containers.Clear();
        world.Containers.CopyItemsTo(scratch);
        for (int i = 0; i < scratch.Count; i++)
        {
            Container container = scratch[i];
            if (container == null || container.m_nview == null || !container.m_nview.IsValid())
            {
                continue;
            }

            ZDO zdo = container.m_nview.GetZDO();
            if (zdo == null)
            {
                continue;
            }

            ZDOID uid = zdo.m_uid;
            this.containers[new ContainerId(uid.UserID, uid.ID)] = container;
        }

        scratch.Clear();

        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        var inUseBy = new Dictionary<ContainerId, string>();
        for (int i = 0; i < containers.Count; i++)
        {
            ContainerId id = containers[i].Id;
            if (!this.containers.TryGetValue(id, out Container? container))
            {
                continue;
            }

            CategoryAssignment? assignment = ChestCategoryWriter.Read(container);
            if (assignment != null)
            {
                assignments[id] = assignment;
            }

            ChestInUse? inUse = reservations.QueryInUse(container);
            if (inUse != null)
            {
                inUseBy[id] = inUse.HolderName;
            }
        }

        return new OrganizeBaseFacts(assignments, inUseBy);
    }

    /// <summary>Creates the game layer over the containers mapped by the last <see cref="GatherFacts"/>.</summary>
    /// <param name="plan">The plan about to run.</param>
    /// <returns>The layer.</returns>
    public IContainerLayer CreateLayer(OrganizePlan plan) =>
        new GameOrganizeContainerLayer(plan, new Dictionary<ContainerId, Container>(containers), reservations, world.Snapshots);

    /// <summary>Captures the local player's inventory in the game's list order.</summary>
    /// <returns>The snapshot, or null when there is no player or a stack cannot be mapped.</returns>
    public PlayerInventorySnapshot? CaptureInventory() =>
        PlayerInventoryCapture.Capture(Player.m_localPlayer, PlayerInventoryCapture.InventoryId());

    /// <summary>Gets the local player's favourite slots; empty when there are none or no player.</summary>
    public FavouriteSlots Favourites => FavouriteSession.ForLocalPlayer()?.Current ?? FavouriteSlots.Empty;

    /// <summary>Creates the game layer of one Deposit over the containers mapped by the last <see cref="GatherFacts"/>.</summary>
    /// <param name="plan">The Deposit plan about to run.</param>
    /// <returns>The layer.</returns>
    public IContainerLayer CreateDepositLayer(DepositPlan plan) =>
        new GameDepositContainerLayer(plan, new Dictionary<ContainerId, Container>(containers), reservations, world.Snapshots, Player.m_localPlayer);

    /// <summary>Shows the confirm popup; cancels the run when no popup can be shown (fail closed).</summary>
    /// <param name="header">The popup header.</param>
    /// <param name="body">The preview body.</param>
    public void ShowPreview(string header, string body)
    {
        if (!OrganizePreviewPopup.Show(header, body))
        {
            OrganizeRunner.Cancel();
        }
    }

    /// <summary>Saves the categories Organize chose, through the chest category writer (S-A-003).</summary>
    /// <param name="rules">The chests and their categories.</param>
    public void SaveCategoryRules(IReadOnlyList<ChestAutoAssignment> rules)
    {
        for (int i = 0; i < rules.Count; i++)
        {
            ChestAutoAssignment rule = rules[i];
            if (rule.Assignment != null && containers.TryGetValue(rule.Id, out Container? container))
            {
                ChestCategoryWriter.TrySetQuiet(container, rule.Id, rule.Assignment);
            }
        }
    }

    /// <summary>Shows one notice through the one notice helper.</summary>
    /// <param name="placement">Where the notice appears.</param>
    /// <param name="key">The strings-table key.</param>
    /// <param name="args">The placeholder values.</param>
    public void ShowNotice(OrganizeNoticePlacement placement, string key, object?[] args)
    {
        Notices.Show(placement == OrganizeNoticePlacement.TopLeft ? NoticePlacement.TopLeft : NoticePlacement.Center, key, args);
    }

    private static PlayerProfile? Profile()
    {
        Game game = Game.instance;
        return game == null ? null : game.GetPlayerProfile();
    }
}
