namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.Shared;

/// <summary>
/// AC-13 (contract 6.1 and 6.2): the 31 new constants and the 2 HarmonyPatched ids are the contract literals. Read by reflection,
/// so no ValheimDir is needed and a missing constant fails with its own name. The real-dll check is GW-U-109.
/// </summary>
public sealed class PatchTargetNamesFavouritesTests
{
    /// <summary>The 31 contract 6.1 constants, by owner class, constant name and literal value.</summary>
    private static readonly (string Owner, string Const, string Value)[] NewConstants =
    [
        ("Player", "FieldCustomData", "m_customData"),
        ("Player", "FieldCustomDataType", "System.Collections.Generic.Dictionary`2[System.String,System.String]"),
        ("Player", "FieldLocalPlayer", "m_localPlayer"),
        ("Player", "FieldLocalPlayerType", "Player"),
        ("Inventory", "GetHeight", "GetHeight"),
        ("Inventory", "GetItemAt", "GetItemAt"),
        ("Inventory", "GetWidth", "GetWidth"),
        ("InventoryGui", "FieldDragItem", "m_dragItem"),
        ("InventoryGui", "FieldDragItemType", "ItemDrop+ItemData"),
        ("InventoryGui", "FieldPlayerGrid", "m_playerGrid"),
        ("InventoryGui", "FieldPlayerGridType", "InventoryGrid"),
        ("InventoryGui", "GetInstance", "get_instance"),
        ("InventoryGrid", "GetButtonPos", "GetButtonPos"),
        ("InventoryGrid", "GetElement", "GetElement"),
        ("InventoryGrid", "GetInventory", "GetInventory"),
        ("InventoryGrid", "OnLeftDown", "OnLeftDown"),
        ("InventoryGrid", "OnLeftDownParametersHandler", "UIInputHandler"),
        ("InventoryGrid", "TypeName", "InventoryGrid"),
        ("InventoryGrid", "UpdateGui", "UpdateGui"),
        ("InventoryGrid", "UpdateGuiParametersPlayerDragItem", "Player,ItemDrop+ItemData"),
        ("InventoryElement", "FieldIcon", "m_icon"),
        ("InventoryElement", "FieldIconType", "UnityEngine.UI.Image"),
        ("InventoryElement", "FieldQueued", "m_queued"),
        ("InventoryElement", "FieldQueuedType", "UnityEngine.UI.Image"),
        ("InventoryElement", "TypeName", "InventoryElement"),
        ("ItemData", "FieldShared", "m_shared"),
        ("ItemData", "FieldSharedType", "ItemDrop+ItemData+SharedData"),
        ("ItemData", "TypeName", "ItemDrop+ItemData"),
        ("SharedData", "FieldName", "m_name"),
        ("SharedData", "FieldNameType", "System.String"),
        ("SharedData", "TypeName", "ItemDrop+ItemData+SharedData"),
    ];

    // GW-U-719
    [Fact]
    public void EachNewConstantIsTheContractLiteral()
    {
        var actual = NewConstants
            .Select(row => (row.Owner, row.Const, Value: ReadConstant(row.Owner, row.Const)))
            .ToArray();

        Assert.Equal(NewConstants, actual);
    }

    // GW-U-719
    [Fact]
    public void HarmonyPatchedHoldsTheTwoFavouritePatchTargetsAmongExactlyEleven()
    {
        // ADR-GWIN-055: the union with the S-E002-004 World and S-E002-005 Hover ids; ADR-GWIN-059: plus the S-E005-002 Reservations ids; ADR-GWIN-104: plus the S-E004-004 Category button postfix.
        Assert.Equal(
            new[]
            {
                new HarmonyEntry("ContainerAwake", "Container.Awake()"),
                new HarmonyEntry("ContainerGetHoverText", "Container.GetHoverText()"),
                new HarmonyEntry("ContainerInteract", "Container.Interact(Humanoid,System.Boolean,System.Boolean)"),
                new HarmonyEntry("ContainerRpcOpenResponse", "Container.RPC_OpenResponse(System.Int64,System.Boolean)"),
                new HarmonyEntry("CraftingStationGetHoverText", "CraftingStation.GetHoverText()"),
                new HarmonyEntry("CraftingStationOnDestroy", "CraftingStation.OnDestroy()"),
                new HarmonyEntry("CraftingStationStart", "CraftingStation.Start()"),
                new HarmonyEntry("FejdStartupShowConnectError", "FejdStartup.ShowConnectError(ZNet+ConnectionStatus)"),
                new HarmonyEntry("HudSetupPieceInfo", "Hud.SetupPieceInfo(Piece)"),
                new HarmonyEntry("InventoryGridOnLeftDown", "InventoryGrid.OnLeftDown(UIInputHandler)"),
                new HarmonyEntry("InventoryGridUpdateGui", "InventoryGrid.UpdateGui(Player,ItemDrop+ItemData)"),
                new HarmonyEntry("InventoryGuiDoCrafting", "InventoryGui.DoCrafting(Player)"),
                new HarmonyEntry("InventoryGuiSetupRequirement", "InventoryGui.SetupRequirement(UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32)"),
                new HarmonyEntry("InventoryGuiSetupRequirementList", "InventoryGui.SetupRequirementList(System.Int32,Player,System.Boolean,System.Int32)"),
                new HarmonyEntry("InventoryGuiUpdateContainer", "InventoryGui.UpdateContainer(Player)"),
                // Edited by S-E004-012 (ADR-GWIN-136, contract 6): plus the StationActions UpdateRecipe postfix id.
                new HarmonyEntry("InventoryGuiUpdateRecipe", "InventoryGui.UpdateRecipe(Player,System.Single)"),
                new HarmonyEntry("ItemDataGetTooltip", "ItemDrop+ItemData.GetTooltip(ItemDrop+ItemData,System.Int32,System.Boolean,System.Single,System.Int32,System.Boolean)"),
                new HarmonyEntry("PlayerConsumeResources", "Player.ConsumeResources(Piece+Requirement[],System.Int32,System.Int32,System.Int32)"),
                new HarmonyEntry("PlayerHaveRequirementItems", "Player.HaveRequirementItems(Recipe,System.Boolean,System.Int32,System.Int32)"),
                // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1654): the three building ids of contract 8.1 join (eighteen in all).
                new HarmonyEntry("PlayerHaveRequirementsPiece", "Player.HaveRequirements(Piece,Player+RequirementMode)"),
                new HarmonyEntry("PlayerPlacePiece", "Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)"),
                new HarmonyEntry("PlayerTryPlacePiece", "Player.TryPlacePiece(Piece)"),
                // Edited by S-E010-002 (organize-console-command 6.2): the console registration id (24 in all).
                new HarmonyEntry("TerminalInitTerminal", "Terminal.InitTerminal()"),
                new HarmonyEntry("ZNetOnNewConnection", "ZNet.OnNewConnection(ZNetPeer)"),
                new HarmonyEntry("ZNetViewOnDestroy", "ZNetView.OnDestroy()"),
            },
            TargetList.HarmonyPatchedEntries(typeof(PatchTargetNames)));
    }

    // GW-U-719
    [Fact]
    public void TheRealListStillReportsNoShapeViolation()
    {
        Assert.Empty(ShapeRules.Check(typeof(PatchTargetNames)));
    }

    // GW-U-719
    [Fact]
    public void NoOwnerTypeNameIsDeclaredTwice()
    {
        string[] typeNames = TargetList.Classify(typeof(PatchTargetNames))
            .Where(entry => entry.Kind == TargetKind.TypeEntry)
            .Select(entry => entry.Value)
            .ToArray();

        Assert.Empty(typeNames.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key));
    }

    /// <summary>Reads one literal constant of an owner class, or returns a marker when it is absent.</summary>
    private static string ReadConstant(string owner, string name)
    {
        Type? type = typeof(PatchTargetNames).GetNestedType(owner, BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? field = type?.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        if (field is null || !field.IsLiteral || field.FieldType != typeof(string))
        {
            return "<missing>";
        }

        return (string)field.GetRawConstantValue()!;
    }
}
