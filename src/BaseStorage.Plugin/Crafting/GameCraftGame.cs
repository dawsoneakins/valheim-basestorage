namespace BaseStorage.Plugin.Crafting;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>
/// The game-bound side of a craft (contract craft-from-base 7.1): the gate, the selection and requirement reads, the base
/// snapshot, the pre-flight, the abandonment check, the commit and the notice. Main thread only (W2).
/// </summary>
internal sealed class GameCraftGame : ICraftGame
{
    private readonly GameCraftWorld world;
    private readonly List<RecipeLine> lineScratch = new List<RecipeLine>();
    private int memoFrame = -1;
    private CraftingStation? memoStation;
    private BaseSnapshot? memoSnapshot;

    /// <summary>Creates the game side over the craft world.</summary>
    /// <param name="world">The world port.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    internal GameCraftGame(GameCraftWorld world)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        this.world = world;
    }

    /// <summary>Drops the per-frame snapshot memo.</summary>
    internal void DropMemo()
    {
        memoFrame = -1;
        memoStation = null;
        memoSnapshot = null;
    }

    /// <summary>Evaluates the gate of contract 5.1; the first failing row wins.</summary>
    /// <param name="player">The player.</param>
    /// <param name="recipe">The recipe.</param>
    /// <param name="discover">The discover argument of the materials check.</param>
    /// <param name="qualityLevel">The quality level.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <param name="station">The current station when the gate is open.</param>
    /// <param name="requirements">The requirement list when the gate is open.</param>
    /// <returns>The reason; <see cref="CraftGateReason.UsesBase"/> when the gate is open.</returns>
    internal CraftGateReason EvaluateGate(
        Player player,
        Recipe recipe,
        bool discover,
        int qualityLevel,
        int multiplier,
        out CraftingStation? station,
        out IReadOnlyList<CraftRequirement>? requirements)
    {
        station = null;
        requirements = null;
        BaseStorageSettings? settings = BaseStoragePlugin.Settings;
        if (settings == null || !settings.CraftFromBase)
        {
            return CraftGateReason.SettingOff;
        }

        if (discover)
        {
            return CraftGateReason.Discover;
        }

        if (player == null || player != Player.m_localPlayer)
        {
            return CraftGateReason.NotLocalPlayer;
        }

        if (player.NoCostCheat() || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)))
        {
            return CraftGateReason.NoCost;
        }

        PluginWorld? pluginWorld = PluginWorld.Current;
        CraftingStation current = player.GetCurrentCraftingStation();
        if (pluginWorld == null || current == null || !pluginWorld.Stations.Contains(current) || current.m_nview == null || !current.m_nview.IsValid())
        {
            return CraftGateReason.NoStation;
        }

        ZDO zdo = current.m_nview.GetZDO();
        if (zdo == null)
        {
            return CraftGateReason.NoStation;
        }

        if (current.m_upgrader)
        {
            return CraftGateReason.UpgraderStation;
        }

        if (recipe == null || recipe.m_requireOnlyOneIngredient)
        {
            return CraftGateReason.OnlyOneIngredient;
        }

        ZDOID uid = zdo.m_uid;
        BaseResolution resolution = pluginWorld.Cache.Resolve(
            BaseStart.FromStation(new StationId(uid.UserID, uid.ID)),
            new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
        if (!resolution.IsInBase)
        {
            return CraftGateReason.NotInBase;
        }

        IReadOnlyList<CraftRequirement>? built = multiplier < 1 ? null : CraftRequirementLines.Build(ReadLines(recipe, qualityLevel), multiplier);
        if (built == null || GetSnapshot(current, resolution) == null)
        {
            return CraftGateReason.Unreadable;
        }

        station = current;
        requirements = built;
        return CraftGateReason.UsesBase;
    }

    /// <summary>Gets the base snapshot of a station for this frame (contract 5.2), memoised per frame and station.</summary>
    /// <param name="station">The station.</param>
    /// <returns>The snapshot, or null when unreadable.</returns>
    internal BaseSnapshot? GetSnapshot(CraftingStation station) => GetSnapshot(station, null);

    /// <summary>Reads the selection of the crafting panel exactly as the game's crafting method reads it.</summary>
    /// <param name="gui">The panel.</param>
    /// <param name="station">The current station.</param>
    /// <param name="selection">The selection when readable.</param>
    /// <returns>True when a recipe is selected.</returns>
    internal bool TryReadSelection(InventoryGui gui, CraftingStation station, out CraftSelection selection)
    {
        selection = default;
        if (gui == null || station == null || gui.m_craftRecipe == null)
        {
            return false;
        }

        ItemDrop.ItemData? upgrade = gui.m_craftUpgradeItem;
        int quality = upgrade != null ? upgrade.m_quality + 1 : 1;
        int multiplier = gui.m_multiCrafting ? gui.m_multiCraftAmount : 1;
        if (quality < 1 || multiplier < 1)
        {
            return false;
        }

        selection = new CraftSelection(gui.m_craftRecipe, upgrade, station, quality, multiplier);
        return true;
    }

    /// <inheritdoc />
    public bool IsStillSelected(CraftSelection selection)
    {
        Player player = Player.m_localPlayer;
        InventoryGui? gui = InventoryGui.instance;
        if (player == null || gui == null)
        {
            return false;
        }

        if (!InventoryGui.IsVisible() || !gui.InCraftTab() || ZDOMan.GetSessionID() == 0L)
        {
            return false;
        }

        CraftingStation station = player.GetCurrentCraftingStation();
        return TryReadSelection(gui, station, out CraftSelection now) && now == selection;
    }

    /// <inheritdoc />
    public bool PreFlight(CraftSelection selection)
    {
        Player player = Player.m_localPlayer;
        Recipe recipe = (Recipe)selection.Recipe;
        if (player == null || recipe == null || recipe.m_item == null || recipe.m_item.m_itemData == null || recipe.m_item.m_itemData.m_shared == null)
        {
            return false;
        }

        CraftingStation station = player.GetCurrentCraftingStation();
        if (station == null || !ReferenceEquals(station, selection.Station))
        {
            return false;
        }

        ItemDrop.ItemData.SharedData shared = recipe.m_item.m_itemData.m_shared;
        if (selection.QualityLevel > shared.m_maxQuality)
        {
            return false;
        }

        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            return false;
        }

        ItemDrop.ItemData? upgrade = selection.UpgradeItem as ItemDrop.ItemData;
        if (upgrade != null && !inventory.ContainsItem(upgrade))
        {
            return false;
        }

        if (upgrade == null)
        {
            bool bonusPossible = station.m_craftingSkill != Skills.SkillType.None && shared.m_maxStackSize > 1;
            long bonus = 0L;
            InventoryGui? gui = InventoryGui.instance;
            if (bonusPossible && gui != null)
            {
                bonus = (long)selection.Multiplier * gui.m_craftBonusAmount;
            }

            long maxAmount = Math.Min((long)recipe.m_amount * selection.Multiplier + bonus, int.MaxValue);
            if (!inventory.CanAddItem(recipe.m_item.gameObject, (int)maxAmount))
            {
                return false;
            }
        }

        if (!player.RequiredCraftingStation(recipe, selection.QualityLevel, true))
        {
            return false;
        }

        string dlc = shared.m_dlc;
        return string.IsNullOrEmpty(dlc) || (DLCMan.instance != null && DLCMan.instance.IsDLCInstalled(dlc));
    }

    /// <inheritdoc />
    public void Commit(CraftSelection selection)
    {
        InventoryGui gui = InventoryGui.instance;
        Player player = Player.m_localPlayer;
        if (gui == null || player == null)
        {
            return;
        }

        gui.DoCrafting(player);
    }

    /// <inheritdoc />
    public int ReturnTaken() => world.ReturnAllTaken();

    /// <inheritdoc />
    public void ForgetTaken() => world.ForgetTaken();

    /// <inheritdoc />
    public void ShowNotice(string key) =>
        Notices.Show(FeedbackCatalog.PlacementOf(key) == OrganizeNoticePlacement.TopLeft ? NoticePlacement.TopLeft : NoticePlacement.Center, key);

    private IReadOnlyList<RecipeLine> ReadLines(Recipe recipe, int qualityLevel)
    {
        lineScratch.Clear();
        Piece.Requirement[] resources = recipe.m_resources;
        if (resources != null)
        {
            for (int i = 0; i < resources.Length; i++)
            {
                Piece.Requirement requirement = resources[i];
                if (requirement == null)
                {
                    continue;
                }

                string? name = requirement.m_resItem != null ? requirement.m_resItem.gameObject.name : null;
                lineScratch.Add(new RecipeLine(name, requirement.GetAmount(qualityLevel), requirement.m_upgraderResource));
            }
        }

        return lineScratch;
    }

    private BaseSnapshot? GetSnapshot(CraftingStation station, BaseResolution? known)
    {
        int frame = Time.frameCount;
        if (memoSnapshot != null && memoFrame == frame && ReferenceEquals(memoStation, station))
        {
            return memoSnapshot;
        }

        BaseStorageSettings? settings = BaseStoragePlugin.Settings;
        PluginWorld? pluginWorld = PluginWorld.Current;
        if (settings == null || pluginWorld == null || station == null || station.m_nview == null || !station.m_nview.IsValid())
        {
            return null;
        }

        BaseResolution resolution = known != null ? known : ResolveFor(pluginWorld, settings, station);
        ContainerSnapshot? inventory = world.ReadInventory();
        if (!resolution.IsInBase || inventory == null)
        {
            return null;
        }

        IReadOnlyList<ContainerSnapshot> fresh = pluginWorld.Snapshots.GetContainers();
        var byId = new Dictionary<ContainerId, ContainerSnapshot>(fresh.Count);
        for (int i = 0; i < fresh.Count; i++)
        {
            byId[fresh[i].Id] = fresh[i];
        }

        var members = new List<ContainerSnapshot>(resolution.Containers.Count);
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        for (int i = 0; i < resolution.Containers.Count; i++)
        {
            ContainerId id = resolution.Containers[i].Id;
            if (!byId.TryGetValue(id, out ContainerSnapshot? current))
            {
                continue;
            }

            members.Add(current);
            Container? chest = world.FindChest(id);
            CategoryAssignment? assignment = chest != null ? ChestCategoryWriter.Read(chest) : null;
            if (assignment != null)
            {
                assignments[id] = assignment;
            }
        }

        BaseSnapshot snapshot;
        try
        {
            snapshot = new BaseSnapshot(inventory, members, assignments, EligibilityOptions.FromSettings(settings));
        }
        catch (ArgumentException)
        {
            return null;
        }

        memoFrame = frame;
        memoStation = station;
        memoSnapshot = snapshot;
        return snapshot;
    }

    private static BaseResolution ResolveFor(PluginWorld pluginWorld, BaseStorageSettings settings, CraftingStation station)
    {
        ZDO zdo = station.m_nview.GetZDO();
        if (zdo == null)
        {
            return BaseResolution.NotInBase;
        }

        ZDOID uid = zdo.m_uid;
        return pluginWorld.Cache.Resolve(
            BaseStart.FromStation(new StationId(uid.UserID, uid.ID)),
            new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
    }
}
