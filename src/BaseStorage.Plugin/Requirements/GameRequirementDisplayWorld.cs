namespace BaseStorage.Plugin.Requirements;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>
/// The game-bound world port of the requirement display (contract requirement-display 5.2 and 5.3): it reads, it never
/// writes. Main thread only (W2).
/// </summary>
internal sealed class GameRequirementDisplayWorld : IRequirementDisplayWorld
{
    private readonly GameCraftGame craftGame;
    private readonly GameCraftWorld craftWorld;
    private int buildMemoFrame = -1;
    private BaseSnapshot? buildMemo;

    /// <summary>Creates the port over the craft side used read-only.</summary>
    /// <param name="craftGame">The craft game side.</param>
    /// <param name="craftWorld">The craft world.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    internal GameRequirementDisplayWorld(GameCraftGame craftGame, GameCraftWorld craftWorld)
    {
        if (craftGame == null)
        {
            throw new ArgumentNullException(nameof(craftGame));
        }

        if (craftWorld == null)
        {
            throw new ArgumentNullException(nameof(craftWorld));
        }

        this.craftGame = craftGame;
        this.craftWorld = craftWorld;
    }

    /// <inheritdoc />
    public BaseSnapshot? ReadCraftingPanel(int qualityLevel, int multiplier)
    {
        Player player = Player.m_localPlayer;
        InventoryGui gui = InventoryGui.instance;
        if (player == null || gui == null)
        {
            return null;
        }

        Recipe recipe = gui.m_selectedRecipe.Recipe;
        if (recipe == null)
        {
            return null;
        }

        CraftGateReason reason = craftGame.EvaluateGate(player, recipe, false, qualityLevel, multiplier, out CraftingStation? station, out _);
        if (reason != CraftGateReason.UsesBase || station == null)
        {
            return null;
        }

        return craftGame.GetSnapshot(station);
    }

    /// <inheritdoc />
    public BaseSnapshot? ReadBuildTooltip()
    {
        int frame = Time.frameCount;
        if (buildMemoFrame == frame)
        {
            return buildMemo;
        }

        BaseSnapshot? snapshot = BuildTooltipSnapshot();
        buildMemoFrame = frame;
        buildMemo = snapshot;
        return snapshot;
    }

    private BaseSnapshot? BuildTooltipSnapshot()
    {
        BaseStorageSettings? settings = BaseStoragePlugin.Settings;
        if (settings == null || !settings.CraftFromBase)
        {
            return null;
        }

        Player player = Player.m_localPlayer;
        if (player == null || player.NoCostCheat())
        {
            return null;
        }

        PluginWorld? pluginWorld = PluginWorld.Current;
        if (pluginWorld == null)
        {
            return null;
        }

        Vector3 p = player.transform.position;
        BaseResolution resolution = pluginWorld.Cache.Resolve(
            BaseStart.FromPosition(new WorldPosition(p.x, p.y, p.z)),
            new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
        if (!resolution.IsInBase)
        {
            return null;
        }

        ContainerSnapshot? inventory = craftWorld.ReadInventory();
        if (inventory == null)
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
            Container? chest = craftWorld.FindChest(id);
            CategoryAssignment? assignment = chest != null ? ChestCategoryWriter.Read(chest) : null;
            if (assignment != null)
            {
                assignments[id] = assignment;
            }
        }

        try
        {
            return new BaseSnapshot(inventory, members, assignments, EligibilityOptions.FromSettings(settings));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
