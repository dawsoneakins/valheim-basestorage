namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Fixtures;
using BaseStorage.Shared;

/// <summary>
/// AC-2 and AC-3: the shape of PatchTargetNames (contract 4.5). Unconditional: no game install and no ValheimDir.
/// </summary>
public sealed class PatchTargetShapeTests
{
    // GW-U-092
    [Fact]
    public void ClassificationOfTheRealListMatchesTheFixtureTable()
    {
        TargetEntry[] expected =
        [
            new("BuildUi", "AddRecentPiece", TargetKind.MethodNameOnly, "AddRecentPiece", null),
            new("BuildUi", "TypeName", TargetKind.TypeEntry, "BuildUi", null),
            new("Character", "GetZdoid", TargetKind.MethodNameOnly, "GetZDOID", null),
            new("Character", "TypeName", TargetKind.TypeEntry, "Character", null),
            new("Chat", "HasFocus", TargetKind.MethodNameOnly, "HasFocus", null),
            new("Chat", "TypeName", TargetKind.TypeEntry, "Chat", null),
            new("ConnectionStatus", "FieldErrorVersion", TargetKind.FieldEntry, "ErrorVersion", null),
            new("ConnectionStatus", "FieldErrorVersionType", TargetKind.FieldTypeCompanion, "ZNet+ConnectionStatus", "FieldErrorVersion"),
            new("ConnectionStatus", "TypeName", TargetKind.TypeEntry, "ZNet+ConnectionStatus", null),
            new("Console", "IsVisible", TargetKind.MethodNameOnly, "IsVisible", null),
            new("Console", "TypeName", TargetKind.TypeEntry, "Console", null),
            // Edited by S-E010-002 (organize-console-command 6.2): the ConsoleCommand, ConsoleEvent and ConsoleEventArgs rows (1, 1, 5).
            new("ConsoleCommand", "TypeName", TargetKind.TypeEntry, "Terminal+ConsoleCommand", null),
            new("ConsoleEvent", "TypeName", TargetKind.TypeEntry, "Terminal+ConsoleEvent", null),
            new("ConsoleEventArgs", "FieldArgs", TargetKind.FieldEntry, "Args", null),
            new("ConsoleEventArgs", "FieldArgsType", TargetKind.FieldTypeCompanion, "System.String[]", "FieldArgs"),
            new("ConsoleEventArgs", "FieldContext", TargetKind.FieldEntry, "Context", null),
            new("ConsoleEventArgs", "FieldContextType", TargetKind.FieldTypeCompanion, "Terminal", "FieldContext"),
            new("ConsoleEventArgs", "TypeName", TargetKind.TypeEntry, "Terminal+ConsoleEventArgs", null),
            new("Container", "Awake", TargetKind.MethodOverloads, "Awake", null),
            new("Container", "AwakeParametersNone", TargetKind.OverloadCompanion, "", "Awake"),
            new("Container", "CheckAccess", TargetKind.MethodNameOnly, "CheckAccess", null),
            new("Container", "CheckForChanges", TargetKind.MethodNameOnly, "CheckForChanges", null),
            new("Container", "FieldCheckGuardStone", TargetKind.FieldEntry, "m_checkGuardStone", null),
            new("Container", "FieldCheckGuardStoneType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldCheckGuardStone"),
            new("Container", "FieldHeight", TargetKind.FieldEntry, "m_height", null),
            new("Container", "FieldHeightType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldHeight"),
            new("Container", "FieldInUse", TargetKind.FieldEntry, "m_inUse", null),
            new("Container", "FieldInUseType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldInUse"),
            new("Container", "FieldInventory", TargetKind.FieldEntry, "m_inventory", null),
            new("Container", "FieldInventoryType", TargetKind.FieldTypeCompanion, "Inventory", "FieldInventory"),
            new("Container", "FieldNview", TargetKind.FieldEntry, "m_nview", null),
            new("Container", "FieldNviewType", TargetKind.FieldTypeCompanion, "ZNetView", "FieldNview"),
            new("Container", "FieldPiece", TargetKind.FieldEntry, "m_piece", null),
            new("Container", "FieldPieceType", TargetKind.FieldTypeCompanion, "Piece", "FieldPiece"),
            new("Container", "FieldPrivacy", TargetKind.FieldEntry, "m_privacy", null),
            new("Container", "FieldPrivacyType", TargetKind.FieldTypeCompanion, "Container+PrivacySetting", "FieldPrivacy"),
            new("Container", "FieldWagon", TargetKind.FieldEntry, "m_wagon", null),
            new("Container", "FieldWagonType", TargetKind.FieldTypeCompanion, "Vagon", "FieldWagon"),
            new("Container", "FieldWidth", TargetKind.FieldEntry, "m_width", null),
            new("Container", "FieldWidthType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldWidth"),
            new("Container", "GetHoverText", TargetKind.MethodOverloads, "GetHoverText", null),
            new("Container", "GetHoverTextParametersNone", TargetKind.OverloadCompanion, "", "GetHoverText"),
            new("Container", "GetInventory", TargetKind.MethodNameOnly, "GetInventory", null),
            new("Container", "Interact", TargetKind.MethodOverloads, "Interact", null),
            new("Container", "InteractParametersCharacterHoldAlt", TargetKind.OverloadCompanion, "Humanoid,System.Boolean,System.Boolean", "Interact"),
            new("Container", "IsInUse", TargetKind.MethodNameOnly, "IsInUse", null),
            new("Container", "Load", TargetKind.MethodNameOnly, "Load", null),
            new("Container", "OnContainerChanged", TargetKind.MethodNameOnly, "OnContainerChanged", null),
            new("Container", "RpcOpenResponse", TargetKind.MethodOverloads, "RPC_OpenResponse", null),
            new("Container", "RpcOpenResponseParametersUidGranted", TargetKind.OverloadCompanion, "System.Int64,System.Boolean", "RpcOpenResponse"),
            new("Container", "RpcRequestOpen", TargetKind.MethodNameOnly, "RPC_RequestOpen", null),
            new("Container", "RpcRequestStack", TargetKind.MethodNameOnly, "RPC_RequestStack", null),
            new("Container", "RpcStackResponse", TargetKind.MethodNameOnly, "RPC_StackResponse", null),
            new("Container", "Save", TargetKind.MethodNameOnly, "Save", null),
            new("Container", "SetInUse", TargetKind.MethodNameOnly, "SetInUse", null),
            new("Container", "TypeName", TargetKind.TypeEntry, "Container", null),
            new("Container", "UpdateUseVisual", TargetKind.MethodNameOnly, "UpdateUseVisual", null),
            new("CookingStation", "AddFuelRpc", TargetKind.MethodNameOnly, "RPC_AddFuel", null),
            new("CookingStation", "AddItemRpc", TargetKind.MethodNameOnly, "RPC_AddItem", null),
            new("CookingStation", "Awake", TargetKind.MethodOverloads, "Awake", null),
            new("CookingStation", "AwakeParametersNone", TargetKind.OverloadCompanion, "", "Awake"),
            new("CookingStation", "GetHoverText", TargetKind.MethodOverloads, "GetHoverText", null),
            new("CookingStation", "GetHoverTextParametersNone", TargetKind.OverloadCompanion, "", "GetHoverText"),
            new("CookingStation", "Interact", TargetKind.MethodOverloads, "Interact", null),
            new("CookingStation", "InteractParametersUserHoldAlt", TargetKind.OverloadCompanion, "Humanoid,System.Boolean,System.Boolean", "Interact"),
            new("CookingStation", "TypeName", TargetKind.TypeEntry, "CookingStation", null),
            new("CraftingStation", "FieldCraftingSkill", TargetKind.FieldEntry, "m_craftingSkill", null),
            new("CraftingStation", "FieldCraftingSkillType", TargetKind.FieldTypeCompanion, "Skills+SkillType", "FieldCraftingSkill"),
            new("CraftingStation", "FieldNview", TargetKind.FieldEntry, "m_nview", null),
            new("CraftingStation", "FieldNviewType", TargetKind.FieldTypeCompanion, "ZNetView", "FieldNview"),
            new("CraftingStation", "FieldRangeBuild", TargetKind.FieldEntry, "m_rangeBuild", null),
            new("CraftingStation", "FieldRangeBuildType", TargetKind.FieldTypeCompanion, "System.Single", "FieldRangeBuild"),
            new("CraftingStation", "FieldUpgrader", TargetKind.FieldEntry, "m_upgrader", null),
            new("CraftingStation", "FieldUpgraderType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldUpgrader"),
            new("CraftingStation", "GetHoverText", TargetKind.MethodOverloads, "GetHoverText", null),
            new("CraftingStation", "GetHoverTextParametersNone", TargetKind.OverloadCompanion, "", "GetHoverText"),
            new("CraftingStation", "GetStationBuildRange", TargetKind.MethodNameOnly, "GetStationBuildRange", null),
            new("CraftingStation", "HaveBuildStationInRange", TargetKind.MethodNameOnly, "HaveBuildStationInRange", null),
            new("CraftingStation", "OnDestroy", TargetKind.MethodOverloads, "OnDestroy", null),
            new("CraftingStation", "OnDestroyParametersNone", TargetKind.OverloadCompanion, "", "OnDestroy"),
            new("CraftingStation", "Start", TargetKind.MethodOverloads, "Start", null),
            new("CraftingStation", "StartParametersNone", TargetKind.OverloadCompanion, "", "Start"),
            new("CraftingStation", "TypeName", TargetKind.TypeEntry, "CraftingStation", null),
            new("DLCMan", "GetInstance", TargetKind.MethodNameOnly, "get_instance", null),
            new("DLCMan", "IsDlcInstalled", TargetKind.MethodOverloads, "IsDLCInstalled", null),
            new("DLCMan", "IsDlcInstalledParametersName", TargetKind.OverloadCompanion, "System.String", "IsDlcInstalled"),
            new("DLCMan", "TypeName", TargetKind.TypeEntry, "DLCMan", null),
            new("EffectList", "Create", TargetKind.MethodNameOnly, "Create", null),
            new("EffectList", "TypeName", TargetKind.TypeEntry, "EffectList", null),
            new("FejdStartup", "FieldConnectionFailedError", TargetKind.FieldEntry, "m_connectionFailedError", null),
            new("FejdStartup", "FieldConnectionFailedErrorType", TargetKind.FieldTypeCompanion, "TMPro.TMP_Text", "FieldConnectionFailedError"),
            new("FejdStartup", "FieldConnectionFailedPanel", TargetKind.FieldEntry, "m_connectionFailedPanel", null),
            new("FejdStartup", "FieldConnectionFailedPanelType", TargetKind.FieldTypeCompanion, "UnityEngine.GameObject", "FieldConnectionFailedPanel"),
            new("FejdStartup", "ShowConnectError", TargetKind.MethodOverloads, "ShowConnectError", null),
            new("FejdStartup", "ShowConnectErrorParametersStatus", TargetKind.OverloadCompanion, "ZNet+ConnectionStatus", "ShowConnectError"),
            new("FejdStartup", "TypeName", TargetKind.TypeEntry, "FejdStartup", null),
            new("Fermenter", "AddItemRpc", TargetKind.MethodNameOnly, "RPC_AddItem", null),
            new("Fermenter", "Awake", TargetKind.MethodOverloads, "Awake", null),
            new("Fermenter", "AwakeParametersNone", TargetKind.OverloadCompanion, "", "Awake"),
            new("Fermenter", "GetHoverText", TargetKind.MethodOverloads, "GetHoverText", null),
            new("Fermenter", "GetHoverTextParametersNone", TargetKind.OverloadCompanion, "", "GetHoverText"),
            new("Fermenter", "Interact", TargetKind.MethodOverloads, "Interact", null),
            new("Fermenter", "InteractParametersUserHoldAlt", TargetKind.OverloadCompanion, "Humanoid,System.Boolean,System.Boolean", "Interact"),
            new("Fermenter", "TypeName", TargetKind.TypeEntry, "Fermenter", null),
            new("Fireplace", "Awake", TargetKind.MethodOverloads, "Awake", null),
            new("Fireplace", "AwakeParametersNone", TargetKind.OverloadCompanion, "", "Awake"),
            new("Fireplace", "GetHoverText", TargetKind.MethodOverloads, "GetHoverText", null),
            new("Fireplace", "GetHoverTextParametersNone", TargetKind.OverloadCompanion, "", "GetHoverText"),
            new("Fireplace", "Interact", TargetKind.MethodOverloads, "Interact", null),
            new("Fireplace", "InteractParametersUserHoldAlt", TargetKind.OverloadCompanion, "Humanoid,System.Boolean,System.Boolean", "Interact"),
            new("Fireplace", "TypeName", TargetKind.TypeEntry, "Fireplace", null),
            new("Game", "FieldDurabilityRate", TargetKind.FieldEntry, "m_durabilityRate", null),
            new("Game", "FieldDurabilityRateType", TargetKind.FieldTypeCompanion, "System.Single", "FieldDurabilityRate"),
            new("Game", "GetInstance", TargetKind.MethodNameOnly, "get_instance", null),
            new("Game", "GetPlayerProfile", TargetKind.MethodNameOnly, "GetPlayerProfile", null),
            new("Game", "IncrementPlayerStat", TargetKind.MethodNameOnly, "IncrementPlayerStat", null),
            new("Game", "TypeName", TargetKind.TypeEntry, "Game", null),
            new("Hud", "FieldBuildUi", TargetKind.FieldEntry, "m_buildUi", null),
            new("Hud", "FieldBuildUiType", TargetKind.FieldTypeCompanion, "BuildUi", "FieldBuildUi"),
            new("Hud", "GetInstance", TargetKind.MethodNameOnly, "get_instance", null),
            new("Hud", "IsPieceSelectionVisible", TargetKind.MethodNameOnly, "IsPieceSelectionVisible", null),
            // Edited by S-E003-005, ADR-GWIN-127 (contract 8.1): the Hud owner, declared before Humanoid in sorted order.
            // Edited by integration (S-E003-005 + S-E003-006): one Hud owner, its rows in ordinal Const order.
            new("Hud", "SetupPieceInfo", TargetKind.MethodOverloads, "SetupPieceInfo", null),
            new("Hud", "SetupPieceInfoParametersPiece", TargetKind.OverloadCompanion, "Piece", "SetupPieceInfo"),
            new("Hud", "TypeName", TargetKind.TypeEntry, "Hud", null),
            new("Humanoid", "DropInvalidItems", TargetKind.MethodOverloads, "DropInvalidItems", null),
            new("Humanoid", "DropInvalidItemsParametersNone", TargetKind.OverloadCompanion, "", "DropInvalidItems"),
            new("Humanoid", "EquipItem", TargetKind.MethodOverloads, "EquipItem", null),
            new("Humanoid", "EquipItemParametersItemEffects", TargetKind.OverloadCompanion, "ItemDrop+ItemData,System.Boolean", "EquipItem"),
            new("Humanoid", "GetInventory", TargetKind.MethodNameOnly, "GetInventory", null),
            new("Humanoid", "GetRightItem", TargetKind.MethodNameOnly, "GetRightItem", null),
            new("Humanoid", "IsItemEquiped", TargetKind.MethodOverloads, "IsItemEquiped", null),
            new("Humanoid", "IsItemEquipedParametersItem", TargetKind.OverloadCompanion, "ItemDrop+ItemData", "IsItemEquiped"),
            new("Humanoid", "TypeName", TargetKind.TypeEntry, "Humanoid", null),
            new("Humanoid", "UnequipItem", TargetKind.MethodOverloads, "UnequipItem", null),
            new("Humanoid", "UnequipItemParametersItemEffects", TargetKind.OverloadCompanion, "ItemDrop+ItemData,System.Boolean", "UnequipItem"),
            new("Inventory", "AddItem", TargetKind.MethodOverloads, "AddItem", null),
            new("Inventory", "AddItemParametersItem", TargetKind.OverloadCompanion, "ItemDrop+ItemData", "AddItem"),
            new("Inventory", "AddItemParametersItemPosition", TargetKind.OverloadCompanion, "ItemDrop+ItemData,Vector2i", "AddItem"), // Edited by S-QR-002 (ADR-GWIN-176)
            new("Inventory", "CanAddItem", TargetKind.MethodOverloads, "CanAddItem", null),
            new("Inventory", "CanAddItemParametersItemStack", TargetKind.OverloadCompanion, "ItemDrop+ItemData,System.Int32", "CanAddItem"),
            new("Inventory", "CanAddItemParametersPrefabStack", TargetKind.OverloadCompanion, "UnityEngine.GameObject,System.Int32", "CanAddItem"),
            new("Inventory", "ContainsItem", TargetKind.MethodNameOnly, "ContainsItem", null),
            new("Inventory", "CountItems", TargetKind.MethodNameOnly, "CountItems", null),
            new("Inventory", "GetAllItems", TargetKind.MethodOverloads, "GetAllItems", null),
            new("Inventory", "GetAllItemsParametersNone", TargetKind.OverloadCompanion, "", "GetAllItems"),
            new("Inventory", "GetEmptySlots", TargetKind.MethodOverloads, "GetEmptySlots", null),
            new("Inventory", "GetEmptySlotsParametersNone", TargetKind.OverloadCompanion, "", "GetEmptySlots"),
            new("Inventory", "GetHeight", TargetKind.MethodNameOnly, "GetHeight", null),
            new("Inventory", "GetItemAt", TargetKind.MethodNameOnly, "GetItemAt", null),
            new("Inventory", "GetWidth", TargetKind.MethodNameOnly, "GetWidth", null),
            new("Inventory", "HaveEmptySlot", TargetKind.MethodOverloads, "HaveEmptySlot", null),
            new("Inventory", "HaveEmptySlotParametersNone", TargetKind.OverloadCompanion, "", "HaveEmptySlot"),
            new("Inventory", "MoveInventoryToGrave", TargetKind.MethodOverloads, "MoveInventoryToGrave", null),
            new("Inventory", "MoveInventoryToGraveParametersOriginal", TargetKind.OverloadCompanion, "Inventory", "MoveInventoryToGrave"),
            new("Inventory", "RemoveItem", TargetKind.MethodOverloads, "RemoveItem", null),
            new("Inventory", "RemoveItemParametersItemAmount", TargetKind.OverloadCompanion, "ItemDrop+ItemData,System.Int32", "RemoveItem"),
            new("Inventory", "Save", TargetKind.MethodNameOnly, "Save", null),
            new("Inventory", "StackAll", TargetKind.MethodOverloads, "StackAll", null),
            new("Inventory", "StackAllParametersFromMessage", TargetKind.OverloadCompanion, "Inventory,System.Boolean", "StackAll"),
            new("Inventory", "TypeName", TargetKind.TypeEntry, "Inventory", null),
            new("InventoryElement", "FieldIcon", TargetKind.FieldEntry, "m_icon", null),
            new("InventoryElement", "FieldIconType", TargetKind.FieldTypeCompanion, "UnityEngine.UI.Image", "FieldIcon"),
            new("InventoryElement", "FieldQueued", TargetKind.FieldEntry, "m_queued", null),
            new("InventoryElement", "FieldQueuedType", TargetKind.FieldTypeCompanion, "UnityEngine.UI.Image", "FieldQueued"),
            new("InventoryElement", "TypeName", TargetKind.TypeEntry, "InventoryElement", null),
            new("InventoryGrid", "GetButtonPos", TargetKind.MethodNameOnly, "GetButtonPos", null),
            new("InventoryGrid", "GetElement", TargetKind.MethodNameOnly, "GetElement", null),
            new("InventoryGrid", "GetInventory", TargetKind.MethodNameOnly, "GetInventory", null),
            new("InventoryGrid", "OnLeftDown", TargetKind.MethodOverloads, "OnLeftDown", null),
            new("InventoryGrid", "OnLeftDownParametersHandler", TargetKind.OverloadCompanion, "UIInputHandler", "OnLeftDown"),
            new("InventoryGrid", "OnRightDown", TargetKind.MethodOverloads, "OnRightDown", null),
            new("InventoryGrid", "OnRightDownParametersHandler", TargetKind.OverloadCompanion, "UIInputHandler", "OnRightDown"),
            new("InventoryGrid", "TypeName", TargetKind.TypeEntry, "InventoryGrid", null),
            new("InventoryGrid", "UpdateGamepad", TargetKind.MethodOverloads, "UpdateGamepad", null),
            new("InventoryGrid", "UpdateGamepadParametersNone", TargetKind.OverloadCompanion, "", "UpdateGamepad"),
            new("InventoryGrid", "UpdateGui", TargetKind.MethodOverloads, "UpdateGui", null),
            new("InventoryGrid", "UpdateGuiParametersPlayerDragItem", TargetKind.OverloadCompanion, "Player,ItemDrop+ItemData", "UpdateGui"),
            new("InventoryGui", "CloseContainer", TargetKind.MethodNameOnly, "CloseContainer", null),
            new("InventoryGui", "DoCrafting", TargetKind.MethodOverloads, "DoCrafting", null),
            new("InventoryGui", "DoCraftingParametersPlayer", TargetKind.OverloadCompanion, "Player", "DoCrafting"),
            new("InventoryGui", "FieldContainer", TargetKind.FieldEntry, "m_container", null),
            new("InventoryGui", "FieldContainerType", TargetKind.FieldTypeCompanion, "UnityEngine.RectTransform", "FieldContainer"),
            new("InventoryGui", "FieldCraftBonusAmount", TargetKind.FieldEntry, "m_craftBonusAmount", null),
            new("InventoryGui", "FieldCraftBonusAmountType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldCraftBonusAmount"),
            // Edited by S-E004-012 (ADR-GWIN-136, contract 6): the station-actions rows of InventoryGui and UIGamePad.
            new("InventoryGui", "FieldCraftButton", TargetKind.FieldEntry, "m_craftButton", null),
            new("InventoryGui", "FieldCraftButtonType", TargetKind.FieldTypeCompanion, "UnityEngine.UI.Button", "FieldCraftButton"),
            new("InventoryGui", "FieldCraftRecipe", TargetKind.FieldEntry, "m_craftRecipe", null),
            new("InventoryGui", "FieldCraftRecipeType", TargetKind.FieldTypeCompanion, "Recipe", "FieldCraftRecipe"),
            new("InventoryGui", "FieldCraftUpgradeItem", TargetKind.FieldEntry, "m_craftUpgradeItem", null),
            new("InventoryGui", "FieldCraftUpgradeItemType", TargetKind.FieldTypeCompanion, "ItemDrop+ItemData", "FieldCraftUpgradeItem"),
            new("InventoryGui", "FieldCrafting", TargetKind.FieldEntry, "m_crafting", null),
            new("InventoryGui", "FieldCraftingStationName", TargetKind.FieldEntry, "m_craftingStationName", null),
            new("InventoryGui", "FieldCraftingStationNameType", TargetKind.FieldTypeCompanion, "TMPro.TMP_Text", "FieldCraftingStationName"),
            new("InventoryGui", "FieldCraftingType", TargetKind.FieldTypeCompanion, "UnityEngine.RectTransform", "FieldCrafting"),
            new("InventoryGui", "FieldCurrentContainer", TargetKind.FieldEntry, "m_currentContainer", null),
            new("InventoryGui", "FieldCurrentContainerType", TargetKind.FieldTypeCompanion, "Container", "FieldCurrentContainer"),
            new("InventoryGui", "FieldDragItem", TargetKind.FieldEntry, "m_dragItem", null),
            new("InventoryGui", "FieldDragItemType", TargetKind.FieldTypeCompanion, "ItemDrop+ItemData", "FieldDragItem"),
            new("InventoryGui", "FieldMultiCraftAmount", TargetKind.FieldEntry, "m_multiCraftAmount", null),
            new("InventoryGui", "FieldMultiCraftAmountType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldMultiCraftAmount"),
            new("InventoryGui", "FieldMultiCrafting", TargetKind.FieldEntry, "m_multiCrafting", null),
            new("InventoryGui", "FieldMultiCraftingType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldMultiCrafting"),
            new("InventoryGui", "FieldPlayerGrid", TargetKind.FieldEntry, "m_playerGrid", null),
            new("InventoryGui", "FieldPlayerGridType", TargetKind.FieldTypeCompanion, "InventoryGrid", "FieldPlayerGrid"),
            // Edited by S-E003-005, ADR-GWIN-127 (contract 8.1): the requirement-display rows of InventoryGui.
            new("InventoryGui", "FieldRecipeListRoot", TargetKind.FieldEntry, "m_recipeListRoot", null),
            new("InventoryGui", "FieldRecipeListRootType", TargetKind.FieldTypeCompanion, "UnityEngine.RectTransform", "FieldRecipeListRoot"),
            new("InventoryGui", "FieldRecipeRequirementList", TargetKind.FieldEntry, "m_recipeRequirementList", null),
            new("InventoryGui", "FieldRecipeRequirementListType", TargetKind.FieldTypeCompanion, "UnityEngine.GameObject[]", "FieldRecipeRequirementList"),
            new("InventoryGui", "FieldSelectedRecipe", TargetKind.FieldEntry, "m_selectedRecipe", null),
            new("InventoryGui", "FieldSelectedRecipeType", TargetKind.FieldTypeCompanion, "InventoryGui+RecipeDataPair", "FieldSelectedRecipe"),
            new("InventoryGui", "FieldStackAllButton", TargetKind.FieldEntry, "m_stackAllButton", null),
            new("InventoryGui", "FieldStackAllButtonType", TargetKind.FieldTypeCompanion, "UnityEngine.UI.Button", "FieldStackAllButton"),
            new("InventoryGui", "FieldTakeAllButton", TargetKind.FieldEntry, "m_takeAllButton", null),
            new("InventoryGui", "FieldTakeAllButtonType", TargetKind.FieldTypeCompanion, "UnityEngine.UI.Button", "FieldTakeAllButton"),
            new("InventoryGui", "GetInstance", TargetKind.MethodNameOnly, "get_instance", null),
            new("InventoryGui", "Hide", TargetKind.MethodOverloads, "Hide", null),
            new("InventoryGui", "HideParametersNone", TargetKind.OverloadCompanion, "", "Hide"),
            new("InventoryGui", "InCraftTab", TargetKind.MethodNameOnly, "InCraftTab", null),
            new("InventoryGui", "InUpradeTab", TargetKind.MethodNameOnly, "InUpradeTab", null),
            new("InventoryGui", "IsContainerOpen", TargetKind.MethodNameOnly, "IsContainerOpen", null),
            new("InventoryGui", "IsVisible", TargetKind.MethodNameOnly, "IsVisible", null),
            new("InventoryGui", "SetupRequirement", TargetKind.MethodOverloads, "SetupRequirement", null),
            new("InventoryGui", "SetupRequirementList", TargetKind.MethodOverloads, "SetupRequirementList", null),
            new("InventoryGui", "SetupRequirementListParametersQuality", TargetKind.OverloadCompanion, "System.Int32,Player,System.Boolean,System.Int32", "SetupRequirementList"),
            new("InventoryGui", "SetupRequirementParametersElement", TargetKind.OverloadCompanion, "UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32", "SetupRequirement"),
            new("InventoryGui", "Show", TargetKind.MethodNameOnly, "Show", null),
            new("InventoryGui", "TypeName", TargetKind.TypeEntry, "InventoryGui", null),
            new("InventoryGui", "UpdateContainer", TargetKind.MethodOverloads, "UpdateContainer", null),
            new("InventoryGui", "UpdateContainerParametersPlayer", TargetKind.OverloadCompanion, "Player", "UpdateContainer"),
            new("InventoryGui", "UpdateRecipe", TargetKind.MethodOverloads, "UpdateRecipe", null),
            new("InventoryGui", "UpdateRecipeGamepadInput", TargetKind.MethodOverloads, "UpdateRecipeGamepadInput", null),
            new("InventoryGui", "UpdateRecipeGamepadInputParametersNone", TargetKind.OverloadCompanion, "", "UpdateRecipeGamepadInput"),
            new("InventoryGui", "UpdateRecipeParametersPlayerDeltaTime", TargetKind.OverloadCompanion, "Player,System.Single", "UpdateRecipe"),
            new("ItemData", "Clone", TargetKind.MethodNameOnly, "Clone", null),
            // Edited by S-QR-002 (ADR-GWIN-176): the restock field rows of ItemData.
            new("ItemData", "FieldCrafterId", TargetKind.FieldEntry, "m_crafterID", null),
            new("ItemData", "FieldCrafterIdType", TargetKind.FieldTypeCompanion, "System.Int64", "FieldCrafterId"),
            new("ItemData", "FieldCrafterName", TargetKind.FieldEntry, "m_crafterName", null),
            new("ItemData", "FieldCrafterNameType", TargetKind.FieldTypeCompanion, "System.String", "FieldCrafterName"),
            new("ItemData", "FieldCustomData", TargetKind.FieldEntry, "m_customData", null),
            new("ItemData", "FieldCustomDataType", TargetKind.FieldTypeCompanion, "System.Collections.Generic.Dictionary`2[System.String,System.String]", "FieldCustomData"),
            new("ItemData", "FieldDropPrefab", TargetKind.FieldEntry, "m_dropPrefab", null),
            new("ItemData", "FieldDropPrefabType", TargetKind.FieldTypeCompanion, "UnityEngine.GameObject", "FieldDropPrefab"),
            new("ItemData", "FieldDurability", TargetKind.FieldEntry, "m_durability", null),
            new("ItemData", "FieldDurabilityType", TargetKind.FieldTypeCompanion, "System.Single", "FieldDurability"),
            // Edited by S-E004-015 (ADR-GWIN-143): the Deposit rows of deposit-button contract 7.
            new("ItemData", "FieldEquipped", TargetKind.FieldEntry, "m_equipped", null),
            new("ItemData", "FieldEquippedType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldEquipped"),
            new("ItemData", "FieldGridPos", TargetKind.FieldEntry, "m_gridPos", null),
            new("ItemData", "FieldGridPosType", TargetKind.FieldTypeCompanion, "Vector2i", "FieldGridPos"),
            new("ItemData", "FieldQuality", TargetKind.FieldEntry, "m_quality", null),
            new("ItemData", "FieldQualityType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldQuality"),
            new("ItemData", "FieldShared", TargetKind.FieldEntry, "m_shared", null),
            new("ItemData", "FieldSharedType", TargetKind.FieldTypeCompanion, "ItemDrop+ItemData+SharedData", "FieldShared"),
            new("ItemData", "FieldStack", TargetKind.FieldEntry, "m_stack", null),
            new("ItemData", "FieldStackType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldStack"),
            new("ItemData", "FieldVariant", TargetKind.FieldEntry, "m_variant", null), // Edited by S-QR-002 (ADR-GWIN-176)
            new("ItemData", "FieldVariantType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldVariant"), // Edited by S-QR-002 (ADR-GWIN-176)
            new("ItemData", "FieldWorldLevel", TargetKind.FieldEntry, "m_worldLevel", null), // Edited by S-QR-002 (ADR-GWIN-176)
            new("ItemData", "FieldWorldLevelType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldWorldLevel"), // Edited by S-QR-002 (ADR-GWIN-176)
            new("ItemData", "GetTooltip", TargetKind.MethodOverloads, "GetTooltip", null),
            new("ItemData", "GetTooltipParametersItem", TargetKind.OverloadCompanion, "ItemDrop+ItemData,System.Int32,System.Boolean,System.Single,System.Int32,System.Boolean", "GetTooltip"),
            new("ItemData", "TypeName", TargetKind.TypeEntry, "ItemDrop+ItemData", null),
            new("ItemDrop", "DropItem", TargetKind.MethodNameOnly, "DropItem", null),
            new("ItemDrop", "FieldItemData", TargetKind.FieldEntry, "m_itemData", null),
            new("ItemDrop", "FieldItemDataType", TargetKind.FieldTypeCompanion, "ItemDrop+ItemData", "FieldItemData"),
            new("ItemDrop", "TypeName", TargetKind.TypeEntry, "ItemDrop", null),
            new("Menu", "IsVisible", TargetKind.MethodNameOnly, "IsVisible", null),
            new("Menu", "TypeName", TargetKind.TypeEntry, "Menu", null),
            new("ObjectDB", "GetItemPrefab", TargetKind.MethodOverloads, "GetItemPrefab", null),
            new("ObjectDB", "GetItemPrefabParametersName", TargetKind.OverloadCompanion, "System.String", "GetItemPrefab"),
            new("ObjectDB", "TypeName", TargetKind.TypeEntry, "ObjectDB", null),
            new("Piece", "FieldCraftingStation", TargetKind.FieldEntry, "m_craftingStation", null),
            new("Piece", "FieldCraftingStationType", TargetKind.FieldTypeCompanion, "CraftingStation", "FieldCraftingStation"),
            new("Piece", "FieldDlc", TargetKind.FieldEntry, "m_dlc", null),
            new("Piece", "FieldDlcType", TargetKind.FieldTypeCompanion, "System.String", "FieldDlc"),
            new("Piece", "FieldResources", TargetKind.FieldEntry, "m_resources", null),
            new("Piece", "FieldResourcesType", TargetKind.FieldTypeCompanion, "Piece+Requirement[]", "FieldResources"),
            new("Piece", "FreeBuildKey", TargetKind.MethodNameOnly, "FreeBuildKey", null),
            new("Piece", "GetCreator", TargetKind.MethodNameOnly, "GetCreator", null),
            new("Piece", "TypeName", TargetKind.TypeEntry, "Piece", null),
            new("PieceTable", "FieldSkill", TargetKind.FieldEntry, "m_skill", null),
            new("PieceTable", "FieldSkillType", TargetKind.FieldTypeCompanion, "Skills+SkillType", "FieldSkill"),
            new("PieceTable", "TypeName", TargetKind.TypeEntry, "PieceTable", null),
            new("Player", "ConsumeResources", TargetKind.MethodOverloads, "ConsumeResources", null),
            new("Player", "ConsumeResourcesParametersRequirements", TargetKind.OverloadCompanion, "Piece+Requirement[],System.Int32,System.Int32,System.Int32", "ConsumeResources"),
            new("Player", "FieldBuildPieces", TargetKind.FieldEntry, "m_buildPieces", null),
            new("Player", "FieldBuildPiecesType", TargetKind.FieldTypeCompanion, "PieceTable", "FieldBuildPieces"),
            new("Player", "FieldBuildRemoveDebt", TargetKind.FieldEntry, "m_buildRemoveDebt", null),
            new("Player", "FieldBuildRemoveDebtType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldBuildRemoveDebt"),
            new("Player", "FieldCustomData", TargetKind.FieldEntry, "m_customData", null),
            new("Player", "FieldCustomDataType", TargetKind.FieldTypeCompanion, "System.Collections.Generic.Dictionary`2[System.String,System.String]", "FieldCustomData"),
            new("Player", "FieldLastToolUseTime", TargetKind.FieldEntry, "m_lastToolUseTime", null),
            new("Player", "FieldLastToolUseTimeType", TargetKind.FieldTypeCompanion, "System.Single", "FieldLastToolUseTime"),
            new("Player", "FieldLocalPlayer", TargetKind.FieldEntry, "m_localPlayer", null),
            new("Player", "FieldLocalPlayerType", TargetKind.FieldTypeCompanion, "Player", "FieldLocalPlayer"),
            new("Player", "FieldPlacementGhost", TargetKind.FieldEntry, "m_placementGhost", null),
            new("Player", "FieldPlacementGhostType", TargetKind.FieldTypeCompanion, "UnityEngine.GameObject", "FieldPlacementGhost"),
            new("Player", "FieldPlacementStatus", TargetKind.FieldEntry, "m_placementStatus", null),
            new("Player", "FieldPlacementStatusType", TargetKind.FieldTypeCompanion, "Player+PlacementStatus", "FieldPlacementStatus"),
            new("Player", "GetBuildStamina", TargetKind.MethodNameOnly, "GetBuildStamina", null),
            new("Player", "GetCurrentCraftingStation", TargetKind.MethodNameOnly, "GetCurrentCraftingStation", null),
            new("Player", "GetFirstRequiredItem", TargetKind.MethodNameOnly, "GetFirstRequiredItem", null),
            new("Player", "GetHoverObject", TargetKind.MethodNameOnly, "GetHoverObject", null),
            new("Player", "GetPlaceDurability", TargetKind.MethodNameOnly, "GetPlaceDurability", null),
            new("Player", "GetPlayerId", TargetKind.MethodNameOnly, "GetPlayerID", null),
            new("Player", "GetPlayerName", TargetKind.MethodNameOnly, "GetPlayerName", null),
            new("Player", "GetSelectedPiece", TargetKind.MethodNameOnly, "GetSelectedPiece", null),
            new("Player", "HaveRequirementItems", TargetKind.MethodOverloads, "HaveRequirementItems", null),
            new("Player", "HaveRequirementItemsParametersRecipe", TargetKind.OverloadCompanion, "Recipe,System.Boolean,System.Int32,System.Int32", "HaveRequirementItems"),
            new("Player", "HaveRequirements", TargetKind.MethodOverloads, "HaveRequirements", null),
            new("Player", "HaveRequirementsParametersPiece", TargetKind.OverloadCompanion, "Piece,Player+RequirementMode", "HaveRequirements"),
            new("Player", "HaveRequirementsParametersRecipe", TargetKind.OverloadCompanion, "Recipe,System.Boolean,System.Int32,System.Int32", "HaveRequirements"),
            new("Player", "InPlaceMode", TargetKind.MethodNameOnly, "InPlaceMode", null),
            new("Player", "IsDead", TargetKind.MethodNameOnly, "IsDead", null),
            new("Player", "NoCostCheat", TargetKind.MethodNameOnly, "NoCostCheat", null),
            new("Player", "PlacePiece", TargetKind.MethodOverloads, "PlacePiece", null),
            new("Player", "PlacePieceParametersPiecePositionRotation", TargetKind.OverloadCompanion, "Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean", "PlacePiece"),
            new("Player", "RaiseSkill", TargetKind.MethodNameOnly, "RaiseSkill", null),
            new("Player", "RequiredCraftingStation", TargetKind.MethodNameOnly, "RequiredCraftingStation", null),
            new("Player", "TakeInput", TargetKind.MethodNameOnly, "TakeInput", null),
            new("Player", "TryPlacePiece", TargetKind.MethodOverloads, "TryPlacePiece", null),
            new("Player", "TryPlacePieceParametersPiece", TargetKind.OverloadCompanion, "Piece", "TryPlacePiece"),
            new("Player", "TypeName", TargetKind.TypeEntry, "Player", null),
            new("Player", "UpdatePlacementGhost", TargetKind.MethodNameOnly, "UpdatePlacementGhost", null),
            new("Player", "UseStamina", TargetKind.MethodNameOnly, "UseStamina", null),
            new("PlayerInfo", "FieldCharacterId", TargetKind.FieldEntry, "m_characterID", null),
            new("PlayerInfo", "FieldCharacterIdType", TargetKind.FieldTypeCompanion, "ZDOID", "FieldCharacterId"),
            new("PlayerInfo", "FieldName", TargetKind.FieldEntry, "m_name", null),
            new("PlayerInfo", "FieldNameType", TargetKind.FieldTypeCompanion, "System.String", "FieldName"),
            new("PlayerInfo", "TypeName", TargetKind.TypeEntry, "ZNet+PlayerInfo", null),
            new("PlayerProfile", "GetName", TargetKind.MethodNameOnly, "GetName", null),
            new("PlayerProfile", "GetPlayerId", TargetKind.MethodNameOnly, "GetPlayerID", null),
            new("PlayerProfile", "TypeName", TargetKind.TypeEntry, "PlayerProfile", null),
            new("PrivateArea", "CheckAccess", TargetKind.MethodOverloads, "CheckAccess", null),
            new("PrivateArea", "CheckAccessParametersPoint", TargetKind.OverloadCompanion, "UnityEngine.Vector3,System.Single,System.Boolean,System.Boolean", "CheckAccess"),
            new("PrivateArea", "TypeName", TargetKind.TypeEntry, "PrivateArea", null),
            new("Recipe", "FieldAmount", TargetKind.FieldEntry, "m_amount", null),
            new("Recipe", "FieldAmountType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldAmount"),
            new("Recipe", "FieldItem", TargetKind.FieldEntry, "m_item", null),
            new("Recipe", "FieldItemType", TargetKind.FieldTypeCompanion, "ItemDrop", "FieldItem"),
            new("Recipe", "FieldRequireOnlyOneIngredient", TargetKind.FieldEntry, "m_requireOnlyOneIngredient", null),
            new("Recipe", "FieldRequireOnlyOneIngredientType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldRequireOnlyOneIngredient"),
            new("Recipe", "FieldResources", TargetKind.FieldEntry, "m_resources", null),
            new("Recipe", "FieldResourcesType", TargetKind.FieldTypeCompanion, "Piece+Requirement[]", "FieldResources"),
            new("Recipe", "TypeName", TargetKind.TypeEntry, "Recipe", null),
            // Edited by S-E003-005, ADR-GWIN-127 (contract 8.1): the RecipeDataPair owner, declared after Recipe.
            new("RecipeDataPair", "GetRecipe", TargetKind.MethodNameOnly, "get_Recipe", null),
            new("RecipeDataPair", "TypeName", TargetKind.TypeEntry, "InventoryGui+RecipeDataPair", null),
            new("Requirement", "FieldAmount", TargetKind.FieldEntry, "m_amount", null),
            new("Requirement", "FieldAmountType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldAmount"),
            new("Requirement", "FieldResItem", TargetKind.FieldEntry, "m_resItem", null),
            new("Requirement", "FieldResItemType", TargetKind.FieldTypeCompanion, "ItemDrop", "FieldResItem"),
            new("Requirement", "FieldUpgraderResource", TargetKind.FieldEntry, "m_upgraderResource", null),
            new("Requirement", "FieldUpgraderResourceType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldUpgraderResource"),
            new("Requirement", "GetAmount", TargetKind.MethodNameOnly, "GetAmount", null),
            new("Requirement", "TypeName", TargetKind.TypeEntry, "Piece+Requirement", null),
            new("SharedData", "FieldBuildEffect", TargetKind.FieldEntry, "m_buildEffect", null),
            new("SharedData", "FieldBuildEffectType", TargetKind.FieldTypeCompanion, "EffectList", "FieldBuildEffect"),
            new("SharedData", "FieldDlc", TargetKind.FieldEntry, "m_dlc", null),
            new("SharedData", "FieldDlcType", TargetKind.FieldTypeCompanion, "System.String", "FieldDlc"),
            new("SharedData", "FieldItemType", TargetKind.FieldEntry, "m_itemType", null),
            new("SharedData", "FieldItemTypeType", TargetKind.FieldTypeCompanion, "ItemDrop+ItemData+ItemType", "FieldItemType"),
            new("SharedData", "FieldMaxQuality", TargetKind.FieldEntry, "m_maxQuality", null),
            new("SharedData", "FieldMaxQualityType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldMaxQuality"),
            new("SharedData", "FieldMaxStackSize", TargetKind.FieldEntry, "m_maxStackSize", null),
            new("SharedData", "FieldMaxStackSizeType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldMaxStackSize"),
            new("SharedData", "FieldName", TargetKind.FieldEntry, "m_name", null),
            new("SharedData", "FieldNameType", TargetKind.FieldTypeCompanion, "System.String", "FieldName"),
            new("SharedData", "FieldUseDurability", TargetKind.FieldEntry, "m_useDurability", null),
            new("SharedData", "FieldUseDurabilityType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldUseDurability"),
            new("SharedData", "TypeName", TargetKind.TypeEntry, "ItemDrop+ItemData+SharedData", null),
            new("Ship", "TypeName", TargetKind.TypeEntry, "Ship", null),
            // Edited by S-E010-002 (organize-console-command 6.2): the Terminal rows.
            new("Smelter", "Awake", TargetKind.MethodOverloads, "Awake", null),
            new("Smelter", "AwakeParametersNone", TargetKind.OverloadCompanion, "", "Awake"),
            new("Smelter", "TypeName", TargetKind.TypeEntry, "Smelter", null),
            new("Switch", "GetHoverText", TargetKind.MethodOverloads, "GetHoverText", null),
            new("Switch", "GetHoverTextParametersNone", TargetKind.OverloadCompanion, "", "GetHoverText"),
            new("Switch", "Interact", TargetKind.MethodOverloads, "Interact", null),
            new("Switch", "InteractParametersUserHoldAlt", TargetKind.OverloadCompanion, "Humanoid,System.Boolean,System.Boolean", "Interact"),
            new("Switch", "TypeName", TargetKind.TypeEntry, "Switch", null),
            new("Terminal", "AddString", TargetKind.MethodOverloads, "AddString", null),
            new("Terminal", "AddStringParametersText", TargetKind.OverloadCompanion, "System.String", "AddString"),
            new("Terminal", "InitTerminal", TargetKind.MethodOverloads, "InitTerminal", null),
            new("Terminal", "InitTerminalParametersNone", TargetKind.OverloadCompanion, "", "InitTerminal"),
            new("Terminal", "TypeName", TargetKind.TypeEntry, "Terminal", null),
            new("TextInput", "IsVisible", TargetKind.MethodNameOnly, "IsVisible", null),
            new("TextInput", "TypeName", TargetKind.TypeEntry, "TextInput", null),
            new("TombStone", "TypeName", TargetKind.TypeEntry, "TombStone", null),
            new("UIGamePad", "FieldHint", TargetKind.FieldEntry, "m_hint", null),
            new("UIGamePad", "FieldHintType", TargetKind.FieldTypeCompanion, "UnityEngine.GameObject", "FieldHint"),
            new("UIGamePad", "TypeName", TargetKind.TypeEntry, "UIGamePad", null),
            new("UnifiedPopup", "IsAvailable", TargetKind.MethodNameOnly, "IsAvailable", null),
            new("UnifiedPopup", "IsVisible", TargetKind.MethodNameOnly, "IsVisible", null),
            new("UnifiedPopup", "Pop", TargetKind.MethodNameOnly, "Pop", null),
            new("UnifiedPopup", "Push", TargetKind.MethodNameOnly, "Push", null),
            new("UnifiedPopup", "TypeName", TargetKind.TypeEntry, "UnifiedPopup", null),
            new("Vagon", "InUse", TargetKind.MethodNameOnly, "InUse", null),
            new("Vagon", "TypeName", TargetKind.TypeEntry, "Vagon", null),
            new("YesNoPopup", "TypeName", TargetKind.TypeEntry, "YesNoPopup", null),
            new("ZDO", "FieldUid", TargetKind.FieldEntry, "m_uid", null),
            new("ZDO", "FieldUidType", TargetKind.FieldTypeCompanion, "ZDOID", "FieldUid"),
            new("ZDO", "GetByteArray", TargetKind.MethodOverloads, "GetByteArray", null),
            new("ZDO", "GetByteArrayParametersHash", TargetKind.OverloadCompanion, "System.Int32,System.Byte[]", "GetByteArray"),
            new("ZDO", "GetDataRevision", TargetKind.MethodNameOnly, "get_DataRevision", null),
            new("ZDO", "GetInt", TargetKind.MethodOverloads, "GetInt", null),
            new("ZDO", "GetIntParametersHash", TargetKind.OverloadCompanion, "System.Int32,System.Int32", "GetInt"),
            new("ZDO", "GetOwner", TargetKind.MethodNameOnly, "GetOwner", null),
            new("ZDO", "GetString", TargetKind.MethodOverloads, "GetString", null),
            new("ZDO", "GetStringParametersName", TargetKind.OverloadCompanion, "System.String,System.String", "GetString"),
            new("ZDO", "IsOwner", TargetKind.MethodNameOnly, "IsOwner", null),
            new("ZDO", "Set", TargetKind.MethodOverloads, "Set", null),
            new("ZDO", "SetOwner", TargetKind.MethodNameOnly, "SetOwner", null),
            new("ZDO", "SetParametersHashBytes", TargetKind.OverloadCompanion, "System.Int32,System.Byte[]", "Set"),
            new("ZDO", "SetParametersNameString", TargetKind.OverloadCompanion, "System.String,System.String", "Set"),
            new("ZDO", "TypeName", TargetKind.TypeEntry, "ZDO", null),
            new("ZDOID", "GetId", TargetKind.MethodNameOnly, "get_ID", null),
            new("ZDOID", "GetUserId", TargetKind.MethodNameOnly, "get_UserID", null),
            new("ZDOID", "TypeName", TargetKind.TypeEntry, "ZDOID", null),
            new("ZDOMan", "ForceSendZdo", TargetKind.MethodOverloads, "ForceSendZDO", null),
            new("ZDOMan", "ForceSendZdoParametersPeer", TargetKind.OverloadCompanion, "System.Int64,ZDOID", "ForceSendZdo"),
            new("ZDOMan", "GetSessionId", TargetKind.MethodNameOnly, "GetSessionID", null),
            new("ZDOMan", "IsInPeerActiveArea", TargetKind.MethodNameOnly, "IsInPeerActiveArea", null),
            new("ZDOMan", "ReleaseNearbyZdos", TargetKind.MethodNameOnly, "ReleaseNearbyZDOS", null),
            new("ZDOMan", "ReleaseZdos", TargetKind.MethodNameOnly, "ReleaseZDOS", null),
            new("ZDOMan", "RemovePeer", TargetKind.MethodNameOnly, "RemovePeer", null),
            new("ZDOMan", "TypeName", TargetKind.TypeEntry, "ZDOMan", null),
            new("ZDOVars", "FieldInUse", TargetKind.FieldEntry, "s_inUse", null),
            new("ZDOVars", "FieldInUseType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldInUse"),
            new("ZDOVars", "FieldItems", TargetKind.FieldEntry, "s_items", null),
            new("ZDOVars", "FieldItemsType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldItems"),
            new("ZDOVars", "TypeName", TargetKind.TypeEntry, "ZDOVars", null),
            new("ZNet", "Disconnect", TargetKind.MethodNameOnly, "Disconnect", null),
            new("ZNet", "GetConnectionStatus", TargetKind.MethodNameOnly, "GetConnectionStatus", null),
            new("ZNet", "GetInstance", TargetKind.MethodNameOnly, "get_instance", null),
            new("ZNet", "GetPeer", TargetKind.MethodOverloads, "GetPeer", null),
            new("ZNet", "GetPeerParametersUid", TargetKind.OverloadCompanion, "System.Int64", "GetPeer"),
            new("ZNet", "GetPeers", TargetKind.MethodNameOnly, "GetPeers", null),
            new("ZNet", "GetPlayerList", TargetKind.MethodNameOnly, "GetPlayerList", null),
            new("ZNet", "GetTimeSeconds", TargetKind.MethodNameOnly, "GetTimeSeconds", null),
            new("ZNet", "GetUid", TargetKind.MethodNameOnly, "GetUID", null),
            new("ZNet", "IsServer", TargetKind.MethodNameOnly, "IsServer", null),
            new("ZNet", "OnNewConnection", TargetKind.MethodOverloads, "OnNewConnection", null),
            new("ZNet", "OnNewConnectionParametersPeer", TargetKind.OverloadCompanion, "ZNetPeer", "OnNewConnection"),
            new("ZNet", "RpcError", TargetKind.MethodNameOnly, "RPC_Error", null),
            new("ZNet", "SetExternalError", TargetKind.MethodNameOnly, "SetExternalError", null),
            new("ZNet", "TypeName", TargetKind.TypeEntry, "ZNet", null),
            new("ZNetPeer", "FieldRpc", TargetKind.FieldEntry, "m_rpc", null),
            new("ZNetPeer", "FieldRpcType", TargetKind.FieldTypeCompanion, "ZRpc", "FieldRpc"),
            new("ZNetPeer", "FieldServer", TargetKind.FieldEntry, "m_server", null),
            new("ZNetPeer", "FieldServerType", TargetKind.FieldTypeCompanion, "System.Boolean", "FieldServer"),
            new("ZNetPeer", "TypeName", TargetKind.TypeEntry, "ZNetPeer", null),
            new("ZNetView", "ClaimOwnership", TargetKind.MethodNameOnly, "ClaimOwnership", null),
            new("ZNetView", "GetZdo", TargetKind.MethodNameOnly, "GetZDO", null),
            new("ZNetView", "InvokeRpc", TargetKind.MethodOverloads, "InvokeRPC", null),
            new("ZNetView", "InvokeRpcParametersOwner", TargetKind.OverloadCompanion, "System.String,System.Object[]", "InvokeRpc"),
            new("ZNetView", "InvokeRpcParametersTarget", TargetKind.OverloadCompanion, "System.Int64,System.String,System.Object[]", "InvokeRpc"),
            new("ZNetView", "IsOwner", TargetKind.MethodNameOnly, "IsOwner", null),
            new("ZNetView", "IsValid", TargetKind.MethodNameOnly, "IsValid", null),
            new("ZNetView", "OnDestroy", TargetKind.MethodOverloads, "OnDestroy", null),
            new("ZNetView", "OnDestroyParametersNone", TargetKind.OverloadCompanion, "", "OnDestroy"),
            new("ZNetView", "TypeName", TargetKind.TypeEntry, "ZNetView", null),
            new("ZPackage", "ReadByteArray", TargetKind.MethodOverloads, "ReadByteArray", null),
            new("ZPackage", "ReadByteArrayParametersNone", TargetKind.OverloadCompanion, "", "ReadByteArray"),
            new("ZPackage", "TypeName", TargetKind.TypeEntry, "ZPackage", null),
            new("ZPackage", "Write", TargetKind.MethodOverloads, "Write", null),
            new("ZPackage", "WriteParametersBytes", TargetKind.OverloadCompanion, "System.Byte[]", "Write"),
            new("ZRpc", "Invoke", TargetKind.MethodNameOnly, "Invoke", null),
            new("ZRpc", "Register", TargetKind.MethodOverloads, "Register", null),
            new("ZRpc", "RegisterParametersHandler", TargetKind.OverloadCompanion, "System.String,System.Action`2[ZRpc,T]", "Register"),
            new("ZRpc", "TypeName", TargetKind.TypeEntry, "ZRpc", null),
            new("ZoneSystem", "GetGlobalKey", TargetKind.MethodOverloads, "GetGlobalKey", null),
            new("ZoneSystem", "GetGlobalKeyParametersKey", TargetKind.OverloadCompanion, "GlobalKeys", "GetGlobalKey"),
            new("ZoneSystem", "GetInstance", TargetKind.MethodNameOnly, "get_instance", null),
            new("ZoneSystem", "TypeName", TargetKind.TypeEntry, "ZoneSystem", null),
        ];

        IReadOnlyList<TargetEntry> entries = TargetList.Classify(typeof(PatchTargetNames));
        IEnumerable<TargetEntry> ordered = entries
            .OrderBy(e => e.Owner, StringComparer.Ordinal)
            .ThenBy(e => e.Const, StringComparer.Ordinal);

        Assert.Equal(expected, ordered);
        // S-E004-003 (172 rows, 23 companions) plus the 32 S-E006-002 version-handshake rows (GW-C-1261).
        // Edited by S-E003-004, ADR-GWIN-124: 204 + 68 crafting rows (6, 8, 9, 13, 16, 16) = 272.
        // Edited by S-E003-005, ADR-GWIN-127: 272 + 11 requirement-display rows (2, 3, 3, 1, 1, 1) = 283.
        // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1652): plus 58 building rows (5, 2, 2, 19, 15, 15) = 330.
        // Edited by integration (S-E003-005 + S-E003-006): 272 + 11 + 58, less the Hud type row both add = 340 (6, 5, 5, 20, 16, 16).
        // Edited by integration (S-E004-004): plus the nine Category button rows (0, 1, 1, 1, 3, 3) = 349.
        // Edited by integration (S-E004-011): plus the 29 Organize run rows (7, 4, 4, 14, 0, 0) less the 8 the craft stories already declare
        // (Inventory AddItem, RemoveItem, CanAddItem and their ItemData companions, InventoryGui.IsVisible, ItemData.Clone) = 370.
        // Edited by S-E004-012 (ADR-GWIN-136): plus the 15 station-actions rows (1, 1, 1, 0, 6, 6) = 385.
        // Edited by S-E004-015 (ADR-GWIN-143): plus the seven Deposit rows (0, 0, 0, 1, 3, 3) = 392, as 48, 41, 45, 102, 78 and 78.
        // Edited by S-E010-002 (organize-console-command 6.2): 404, by kind 52, 43, 47, 102, 80 and 80.
        Assert.Equal(472, entries.Count); // Edited by S-QR-002 (ADR-GWIN-176)
        Assert.Equal(57, entries.Count(e => e.Kind == TargetKind.TypeEntry));
        Assert.Equal(68, entries.Count(e => e.Kind == TargetKind.MethodOverloads));
        Assert.Equal(73, entries.Count(e => e.Kind == TargetKind.OverloadCompanion)); // Edited by S-QR-002 (ADR-GWIN-176)
        Assert.Equal(104, entries.Count(e => e.Kind == TargetKind.MethodNameOnly));
        Assert.Equal(85, entries.Count(e => e.Kind == TargetKind.FieldEntry)); // Edited by S-QR-002 (ADR-GWIN-176)
        Assert.Equal(85, entries.Count(e => e.Kind == TargetKind.FieldTypeCompanion)); // Edited by S-QR-002 (ADR-GWIN-176)
        Assert.Equal(
            new[]
            {
                new HarmonyEntry("ContainerAwake", "Container.Awake()"),
                new HarmonyEntry("ContainerGetHoverText", "Container.GetHoverText()"),
                new HarmonyEntry("ContainerInteract", "Container.Interact(Humanoid,System.Boolean,System.Boolean)"),
                new HarmonyEntry("ContainerRpcOpenResponse", "Container.RPC_OpenResponse(System.Int64,System.Boolean)"),
                new HarmonyEntry("CookingStationAwake", "CookingStation.Awake()"),
                new HarmonyEntry("CookingStationGetHoverText", "CookingStation.GetHoverText()"),
                new HarmonyEntry("CookingStationInteract", "CookingStation.Interact(Humanoid,System.Boolean,System.Boolean)"),
                new HarmonyEntry("CraftingStationGetHoverText", "CraftingStation.GetHoverText()"),
                new HarmonyEntry("CraftingStationOnDestroy", "CraftingStation.OnDestroy()"),
                new HarmonyEntry("CraftingStationStart", "CraftingStation.Start()"),
                new HarmonyEntry("FejdStartupShowConnectError", "FejdStartup.ShowConnectError(ZNet+ConnectionStatus)"),
                new HarmonyEntry("FermenterAwake", "Fermenter.Awake()"),
                new HarmonyEntry("FermenterGetHoverText", "Fermenter.GetHoverText()"),
                new HarmonyEntry("FermenterInteract", "Fermenter.Interact(Humanoid,System.Boolean,System.Boolean)"),
                new HarmonyEntry("FireplaceAwake", "Fireplace.Awake()"),
                new HarmonyEntry("FireplaceGetHoverText", "Fireplace.GetHoverText()"),
                new HarmonyEntry("FireplaceInteract", "Fireplace.Interact(Humanoid,System.Boolean,System.Boolean)"),
                new HarmonyEntry("HudSetupPieceInfo", "Hud.SetupPieceInfo(Piece)"),
                new HarmonyEntry("HumanoidDropInvalidItems", "Humanoid.DropInvalidItems()"),
                new HarmonyEntry("HumanoidEquipItem", "Humanoid.EquipItem(ItemDrop+ItemData,System.Boolean)"),
                new HarmonyEntry("HumanoidIsItemEquiped", "Humanoid.IsItemEquiped(ItemDrop+ItemData)"),
                new HarmonyEntry("HumanoidUnequipItem", "Humanoid.UnequipItem(ItemDrop+ItemData,System.Boolean)"),
                new HarmonyEntry("InventoryGetEmptySlots", "Inventory.GetEmptySlots()"),
                new HarmonyEntry("InventoryGridOnLeftDown", "InventoryGrid.OnLeftDown(UIInputHandler)"),
                new HarmonyEntry("InventoryGridOnRightDown", "InventoryGrid.OnRightDown(UIInputHandler)"),
                new HarmonyEntry("InventoryGridUpdateGamepad", "InventoryGrid.UpdateGamepad()"),
                new HarmonyEntry("InventoryGridUpdateGui", "InventoryGrid.UpdateGui(Player,ItemDrop+ItemData)"),
                new HarmonyEntry("InventoryGuiDoCrafting", "InventoryGui.DoCrafting(Player)"),
                new HarmonyEntry("InventoryGuiHide", "InventoryGui.Hide()"),
                new HarmonyEntry("InventoryGuiSetupRequirement", "InventoryGui.SetupRequirement(UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32)"),
                new HarmonyEntry("InventoryGuiSetupRequirementList", "InventoryGui.SetupRequirementList(System.Int32,Player,System.Boolean,System.Int32)"),
                new HarmonyEntry("InventoryGuiUpdateContainer", "InventoryGui.UpdateContainer(Player)"),
                new HarmonyEntry("InventoryGuiUpdateRecipe", "InventoryGui.UpdateRecipe(Player,System.Single)"), // Edited by S-E004-012 (ADR-GWIN-136).
                new HarmonyEntry("InventoryGuiUpdateRecipeGamepadInput", "InventoryGui.UpdateRecipeGamepadInput()"),
                new HarmonyEntry("InventoryHaveEmptySlot", "Inventory.HaveEmptySlot()"),
                new HarmonyEntry("InventoryMoveInventoryToGrave", "Inventory.MoveInventoryToGrave(Inventory)"),
                new HarmonyEntry("InventoryStackAll", "Inventory.StackAll(Inventory,System.Boolean)"),
                new HarmonyEntry("ItemDataGetTooltip", "ItemDrop+ItemData.GetTooltip(ItemDrop+ItemData,System.Int32,System.Boolean,System.Single,System.Int32,System.Boolean)"),
                new HarmonyEntry("PlayerConsumeResources", "Player.ConsumeResources(Piece+Requirement[],System.Int32,System.Int32,System.Int32)"),
                new HarmonyEntry("PlayerHaveRequirementItems", "Player.HaveRequirementItems(Recipe,System.Boolean,System.Int32,System.Int32)"),
                new HarmonyEntry("PlayerHaveRequirementsPiece", "Player.HaveRequirements(Piece,Player+RequirementMode)"),
                new HarmonyEntry("PlayerPlacePiece", "Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)"),
                new HarmonyEntry("PlayerTryPlacePiece", "Player.TryPlacePiece(Piece)"),
                new HarmonyEntry("SmelterAwake", "Smelter.Awake()"),
                new HarmonyEntry("SwitchGetHoverText", "Switch.GetHoverText()"),
                new HarmonyEntry("SwitchInteract", "Switch.Interact(Humanoid,System.Boolean,System.Boolean)"),
                // Edited by S-E010-002 (organize-console-command 6.2): the console registration id (24 in all).
                new HarmonyEntry("TerminalInitTerminal", "Terminal.InitTerminal()"),
                new HarmonyEntry("ZNetOnNewConnection", "ZNet.OnNewConnection(ZNetPeer)"),
                new HarmonyEntry("ZNetViewOnDestroy", "ZNetView.OnDestroy()"),
            },
            TargetList.HarmonyPatchedEntries(typeof(PatchTargetNames)));
    }

    // GW-U-093
    [Fact]
    public void TheRealListHasNoShapeViolations()
    {
        Assert.Empty(ShapeRules.Check(typeof(PatchTargetNames)));
    }

    // GW-U-094
    [Theory]
    [InlineData(typeof(FixtureLists.NoTypeName), "BST2015: PatchTargetNames.Player.*: owner class has no TypeName.")]
    [InlineData(typeof(FixtureLists.EmptyValue), "BST2015: PatchTargetNames.Container.TypeName: value is empty.")]
    [InlineData(typeof(FixtureLists.SpaceValue), "BST2015: PatchTargetNames.Container.TypeName: value contains whitespace.")]
    [InlineData(typeof(FixtureLists.TabValue), "BST2015: PatchTargetNames.Container.TypeName: value contains whitespace.")]
    [InlineData(typeof(FixtureLists.FieldNoType), "BST2015: PatchTargetNames.Container.FieldWidth: field entry has no Type companion.")]
    [InlineData(typeof(FixtureLists.DuplicateType), "BST2015: PatchTargetNames.CraftingStation.TypeName: TypeName duplicates another owner class.")]
    [InlineData(typeof(FixtureLists.DuplicateCompanion), "BST2015: PatchTargetNames.Player.HaveRequirementsParametersRecipe: overload duplicates another companion of the same method.")]
    [InlineData(typeof(FixtureLists.HarmonyMissing), "BST2015: PatchTargetNames.HarmonyPatched.*: class is missing.")]
    public void EachOwnerClassShapeRuleFiresWithItsLiteralLine(Type list, string expected)
    {
        Assert.Equal(new[] { expected }, ShapeRules.Check(list));
    }

    // GW-U-094
    [Fact]
    public void MultipleViolationsComeBackSortedOrdinally()
    {
        Assert.Equal(
            new[]
            {
                "BST2015: PatchTargetNames.Container.FieldWidth: field entry has no Type companion.",
                "BST2015: PatchTargetNames.Container.TypeName: value is empty.",
            },
            ShapeRules.Check(typeof(FixtureLists.TwoViolations)));
    }

    // GW-U-094
    [Fact]
    public void AnEmptyParameterlessOverloadCompanionIsNotFlagged()
    {
        Assert.Empty(ShapeRules.Check(typeof(FixtureLists.ParameterlessOverload)));
    }

    // GW-U-095
    [Fact]
    public void ClassifierAppliesTheRuleOrderAndEdgeCases()
    {
        TargetEntry[] expected =
        [
            new("Edges", "FieldOrphanType", TargetKind.FieldEntry, "System.Int32", null),
            new("Edges", "FieldWidth", TargetKind.FieldEntry, "m_width", null),
            new("Edges", "FieldWidthParametersX", TargetKind.FieldEntry, "System.Int32", null),
            new("Edges", "FieldWidthType", TargetKind.FieldTypeCompanion, "System.Int32", "FieldWidth"),
            new("Edges", "HaveRequirements", TargetKind.MethodNameOnly, "HaveRequirements", null),
            new("Edges", "HaveRequirementsParameters", TargetKind.MethodNameOnly, "HaveRequirementsParameters", null),
            new("Edges", "Sized", TargetKind.MethodOverloads, "Sized", null),
            new("Edges", "SizedParametersA", TargetKind.OverloadCompanion, "System.Int32", "Sized"),
            new("Edges", "TypeName", TargetKind.TypeEntry, "Edges", null),
            new("Edges", "TypeNameParametersX", TargetKind.MethodNameOnly, "Foo", null),
        ];

        IEnumerable<TargetEntry> actual = TargetList.Classify(typeof(FixtureLists.RuleEdges))
            .OrderBy(e => e.Const, StringComparer.Ordinal);

        Assert.Equal(expected, actual);
    }

    // GW-U-097
    [Fact]
    public void RecipeOverloadIsDeclaredBeforePieceOverload()
    {
        string[] literalFields = typeof(PatchTargetNames.Player)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.StartsWith("HaveRequirementsParameters", StringComparison.Ordinal))
            .OrderBy(f => f.MetadataToken)
            .Select(f => f.Name)
            .ToArray();
        Assert.Equal(new[] { "HaveRequirementsParametersRecipe", "HaveRequirementsParametersPiece" }, literalFields);

        string[] companions = TargetList.Classify(typeof(PatchTargetNames))
            .Where(e => e.Kind == TargetKind.OverloadCompanion && e.BelongsTo == "HaveRequirements")
            .Select(e => e.Const)
            .ToArray();
        Assert.Equal(new[] { "HaveRequirementsParametersRecipe", "HaveRequirementsParametersPiece" }, companions);
    }

    // GW-U-101
    [Fact]
    public void ValidHarmonyCompositionsPass()
    {
        Assert.Empty(ShapeRules.Check(typeof(FixtureLists.Valid)));
    }

    // GW-U-101
    [Fact]
    public void NonCompositionHarmonyValuesFailWithTheFixedText()
    {
        IReadOnlyList<string> lines = ShapeRules.Check(typeof(FixtureLists.HarmonyBad));

        Assert.Contains("BST2015: PatchTargetNames.HarmonyPatched.PlayerUndeclared: value is not a TypeName.Method(Parameters) composition of declared constants.", lines);
        Assert.Contains("BST2015: PatchTargetNames.HarmonyPatched.PlayerDotNested: value is not a TypeName.Method(Parameters) composition of declared constants.", lines);
        Assert.Contains("BST2015: PatchTargetNames.HarmonyPatched.ContainerNoParens: value is not a TypeName.Method(Parameters) composition of declared constants.", lines);
        Assert.Contains("BST2015: PatchTargetNames.HarmonyPatched.PlayerSpaceAfterComma: value is not a TypeName.Method(Parameters) composition of declared constants.", lines);
    }

    // GW-U-101
    [Fact]
    public void EqualHarmonyPatchedValuesFailOnTheLaterConstant()
    {
        Assert.Equal(
            new[] { "BST2015: PatchTargetNames.HarmonyPatched.PlayerHaveRequirementsRecipeAgain: value duplicates another HarmonyPatched entry." },
            ShapeRules.Check(typeof(FixtureLists.HarmonyDuplicate)));
    }
}
