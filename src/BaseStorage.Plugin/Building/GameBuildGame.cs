namespace BaseStorage.Plugin.Building;

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
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>
/// The game-bound side of a build (contract build-from-base 5): the gate, the requirement and snapshot reads, the piece
/// preconditions, the inventory rule of the placement guard, the pre-flight, the abandonment check, the commit and the notice.
/// Main thread only (W2).
/// </summary>
internal sealed class GameBuildGame : IBuildGame
{
    private readonly GameCraftWorld world;
    private readonly List<RecipeLine> lineScratch = new List<RecipeLine>();
    private int gateFrame = -1;
    private BuildGateReason gateReason;
    private BaseResolution? gateResolution;
    private int snapshotFrame = -1;
    private BaseSnapshot? snapshotMemo;

    /// <summary>Creates the game side over the build world.</summary>
    /// <param name="world">The world port.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    internal GameBuildGame(GameCraftWorld world)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        this.world = world;
    }

    /// <summary>Drops the per-frame gate and snapshot memos.</summary>
    internal void DropMemo()
    {
        gateFrame = -1;
        gateResolution = null;
        snapshotFrame = -1;
        snapshotMemo = null;
    }

    /// <summary>Drops the per-frame snapshot memo only.</summary>
    internal void DropSnapshotMemo()
    {
        snapshotFrame = -1;
        snapshotMemo = null;
    }

    /// <summary>Evaluates the gate of contract 5.1; the first failing row wins.</summary>
    /// <param name="player">The player.</param>
    /// <param name="piece">The piece.</param>
    /// <param name="checkMode">True for the piece check, which evaluates row B6; false for the placement call.</param>
    /// <param name="mode">The requirement mode of the piece check.</param>
    /// <param name="requirements">The requirement list when the gate is open.</param>
    /// <returns>The reason; <see cref="BuildGateReason.UsesBase"/> when the gate is open.</returns>
    internal BuildGateReason EvaluateGate(Player player, Piece piece, bool checkMode, Player.RequirementMode mode, out IReadOnlyList<CraftRequirement>? requirements)
    {
        requirements = null;
        BaseStorageSettings? settings = BaseStoragePlugin.Settings;
        if (settings == null || !settings.CraftFromBase)
        {
            return BuildGateReason.SettingOff;
        }

        if (player == null || player != Player.m_localPlayer)
        {
            return BuildGateReason.NotLocalPlayer;
        }

        BuildGateReason frameReason = EvaluateFrameRows(player, settings);
        if (frameReason != BuildGateReason.UsesBase)
        {
            return frameReason;
        }

        if (checkMode && mode != Player.RequirementMode.CanBuild)
        {
            return BuildGateReason.Mode;
        }

        if (piece == null)
        {
            return BuildGateReason.Unreadable;
        }

        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
        {
            return BuildGateReason.FreeBuild;
        }

        Piece.Requirement[] resources = piece.m_resources;
        if (resources != null)
        {
            for (int i = 0; i < resources.Length; i++)
            {
                Piece.Requirement requirement = resources[i];
                if (requirement != null && requirement.m_resItem != null && requirement.m_amount > 0 && requirement.m_upgraderResource)
                {
                    return BuildGateReason.UpgraderResource;
                }
            }
        }

        IReadOnlyList<CraftRequirement>? built = CraftRequirementLines.Build(ReadLines(piece), 1);
        if (built == null || world.ReadInventory() == null)
        {
            return BuildGateReason.Unreadable;
        }

        requirements = built;
        return BuildGateReason.UsesBase;
    }

    /// <summary>Tells whether the station and DLC halves of the vanilla piece check hold (contract 5.2, PH1 and PH2).</summary>
    /// <param name="player">The player.</param>
    /// <param name="piece">The piece.</param>
    /// <returns>True when both hold.</returns>
    internal bool PreconditionsHold(Player player, Piece piece)
    {
        if (player == null || piece == null)
        {
            return false;
        }

        CraftingStation required = piece.m_craftingStation;
        if (required != null
            && CraftingStation.HaveBuildStationInRange(required.m_name, player.transform.position) == null
            && !(ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench)))
        {
            return false;
        }

        string dlc = piece.m_dlc;
        return string.IsNullOrEmpty(dlc) || (DLCMan.instance != null && DLCMan.instance.IsDLCInstalled(dlc));
    }

    /// <summary>Tells whether the inventory alone covers the piece by the game's own rule (contract 5.5).</summary>
    /// <param name="player">The player.</param>
    /// <param name="piece">The piece.</param>
    /// <returns>True when every requirement line is covered by the inventory.</returns>
    internal bool InventoryCovers(Player player, Piece piece)
    {
        if (player == null || piece == null)
        {
            return false;
        }

        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            return false;
        }

        Piece.Requirement[] resources = piece.m_resources;
        if (resources == null)
        {
            return true;
        }

        for (int i = 0; i < resources.Length; i++)
        {
            Piece.Requirement requirement = resources[i];
            if (requirement == null || requirement.m_resItem == null || requirement.m_amount <= 0)
            {
                continue;
            }

            if (inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Gets the base snapshot for this frame (contract 5.3), memoised per frame.</summary>
    /// <returns>The snapshot, or null when unreadable.</returns>
    internal BaseSnapshot? GetSnapshot()
    {
        int frame = Time.frameCount;
        if (snapshotMemo != null && snapshotFrame == frame)
        {
            return snapshotMemo;
        }

        BaseStorageSettings? settings = BaseStoragePlugin.Settings;
        PluginWorld? pluginWorld = PluginWorld.Current;
        BaseResolution? resolution = gateFrame == frame ? gateResolution : null;
        if (settings == null || pluginWorld == null || resolution == null || !resolution.IsInBase)
        {
            return null;
        }

        ContainerSnapshot? inventory = world.ReadInventory();
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

        snapshotFrame = frame;
        snapshotMemo = snapshot;
        return snapshot;
    }

    /// <inheritdoc />
    public bool IsStillSelected(BuildSelection selection)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || !player.InPlaceMode())
        {
            return false;
        }

        if (!ReferenceEquals(player.GetSelectedPiece(), selection.Piece))
        {
            return false;
        }

        if (InventoryGui.IsVisible() || Hud.IsPieceSelectionVisible())
        {
            return false;
        }

        return ZDOMan.GetSessionID() != 0L;
    }

    /// <inheritdoc />
    public bool PreFlight(BuildSelection selection)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || player.m_placementGhost == null)
        {
            return false;
        }

        if (!PreconditionsHold(player, (Piece)selection.Piece))
        {
            return false;
        }

        player.UpdatePlacementGhost(false);
        return player.m_placementStatus == Player.PlacementStatus.Valid;
    }

    /// <inheritdoc />
    public void Commit(BuildSelection selection)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        bool placed = player.TryPlacePiece((Piece)selection.Piece);
        if (!placed)
        {
            return;
        }

        ItemDrop.ItemData rightItem = player.GetRightItem();
        if (Hud.instance != null)
        {
            Hud.instance.m_buildUi.AddRecentPiece(player.GetSelectedPiece());
        }

        player.m_lastToolUseTime = Time.time;
        player.UseStamina(player.GetBuildStamina());
        ApplySkill(player);
        if (rightItem != null && rightItem.m_shared.m_useDurability)
        {
            rightItem.m_durability -= player.GetPlaceDurability(rightItem) * Game.m_durabilityRate;
        }

        if (rightItem != null)
        {
            rightItem.m_shared.m_buildEffect.Create(player.transform.position, Quaternion.identity, null, 1f, -1, player.GetZDOID());
        }
    }

    /// <inheritdoc />
    public int ReturnTaken() => world.ReturnAllTaken();

    /// <inheritdoc />
    public void ForgetTaken() => world.ForgetTaken();

    /// <inheritdoc />
    public void ShowNotice(string key) =>
        Notices.Show(FeedbackCatalog.PlacementOf(key) == OrganizeNoticePlacement.TopLeft ? NoticePlacement.TopLeft : NoticePlacement.Center, key);

    private static void ApplySkill(Player player)
    {
        if (player.m_buildPieces == null)
        {
            return;
        }

        Skills.SkillType skill = player.m_buildPieces.m_skill;
        if (skill == Skills.SkillType.None)
        {
            return;
        }

        bool crafting = skill == Skills.SkillType.Crafting;
        if (crafting && Game.instance != null)
        {
            Game.instance.IncrementPlayerStat(PlayerStatType.BuiltPieces, 1f, false);
        }

        if (player.m_buildRemoveDebt > 0)
        {
            player.m_buildRemoveDebt--;
            return;
        }

        player.RaiseSkill(skill, 1f);
        if (crafting && Game.instance != null)
        {
            Game.instance.IncrementPlayerStat(PlayerStatType.BuiltPiecesNoDebt, 1f, false);
        }
    }

    private BuildGateReason EvaluateFrameRows(Player player, BaseStorageSettings settings)
    {
        int frame = Time.frameCount;
        if (gateFrame == frame)
        {
            return gateReason;
        }

        BuildGateReason reason = BuildGateReason.UsesBase;
        BaseResolution? resolution = null;
        PluginWorld? pluginWorld = PluginWorld.Current;
        if (player.NoCostCheat())
        {
            reason = BuildGateReason.NoCost;
        }
        else if (PluginCrafting.Current != null && PluginCrafting.Current.Phase != CraftPhase.Idle)
        {
            reason = BuildGateReason.CraftPending;
        }
        else if (pluginWorld == null)
        {
            reason = BuildGateReason.NotInBase;
        }
        else
        {
            Vector3 p = player.transform.position;
            resolution = pluginWorld.Cache.Resolve(
                BaseStart.FromPosition(new WorldPosition(p.x, p.y, p.z)),
                new BaseCacheOptions(settings.BaseCacheSeconds, settings.MaxBaseStations));
            if (!resolution.IsInBase)
            {
                reason = BuildGateReason.NotInBase;
            }
        }

        gateFrame = frame;
        gateReason = reason;
        gateResolution = resolution;
        snapshotFrame = -1;
        snapshotMemo = null;
        return reason;
    }

    private IReadOnlyList<RecipeLine> ReadLines(Piece piece)
    {
        lineScratch.Clear();
        Piece.Requirement[] resources = piece.m_resources;
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
                lineScratch.Add(new RecipeLine(name, requirement.m_amount, requirement.m_upgraderResource));
            }
        }

        return lineScratch;
    }
}
