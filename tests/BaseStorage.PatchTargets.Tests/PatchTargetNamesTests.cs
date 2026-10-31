namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Reflection;

/// <summary>
/// AC-9: the linked src/Shared/PatchTargetNames.cs holds the v1 starter patch targets (contract section 4).
/// The type is reached through reflection, so this project still compiles while the link is missing and the
/// tests fail at run time instead (ADR-GWIN-007). The tests need no game install and no ValheimDir.
/// </summary>
public sealed class PatchTargetNamesTests
{
    private const string SharedTypeName = "BaseStorage.Shared.PatchTargetNames";

    // GW-U-044
    [Theory]
    [InlineData("Player", "TypeName", "Player")]
    [InlineData("Player", "HaveRequirements", "HaveRequirements")]
    [InlineData("Player", "HaveRequirementsParametersRecipe", "Recipe,System.Boolean,System.Int32,System.Int32")]
    [InlineData("Player", "HaveRequirementsParametersPiece", "Piece,Player+RequirementMode")]
    [InlineData("Container", "TypeName", "Container")]
    [InlineData("Container", "RpcRequestOpen", "RPC_RequestOpen")]
    [InlineData("Container", "RpcOpenResponse", "RPC_OpenResponse")]
    [InlineData("Container", "RpcRequestStack", "RPC_RequestStack")]
    [InlineData("Container", "RpcStackResponse", "RPC_StackResponse")]
    [InlineData("Container", "FieldWidth", "m_width")]
    [InlineData("Container", "FieldWidthType", "System.Int32")]
    [InlineData("Container", "FieldHeight", "m_height")]
    [InlineData("Container", "FieldHeightType", "System.Int32")]
    [InlineData("CraftingStation", "TypeName", "CraftingStation")]
    [InlineData("CraftingStation", "FieldRangeBuild", "m_rangeBuild")]
    [InlineData("CraftingStation", "FieldRangeBuildType", "System.Single")]
    [InlineData("Container", "Interact", "Interact")]
    [InlineData("Container", "CheckAccess", "CheckAccess")]
    [InlineData("Container", "IsInUse", "IsInUse")]
    [InlineData("Container", "SetInUse", "SetInUse")]
    [InlineData("Container", "UpdateUseVisual", "UpdateUseVisual")]
    [InlineData("Container", "CheckForChanges", "CheckForChanges")]
    [InlineData("Container", "OnContainerChanged", "OnContainerChanged")]
    [InlineData("Container", "Save", "Save")]
    [InlineData("Container", "Load", "Load")]
    [InlineData("Container", "FieldNview", "m_nview")]
    [InlineData("Container", "FieldNviewType", "ZNetView")]
    [InlineData("Container", "FieldInventory", "m_inventory")]
    [InlineData("Container", "FieldInventoryType", "Inventory")]
    [InlineData("Container", "FieldInUse", "m_inUse")]
    [InlineData("Container", "FieldInUseType", "System.Boolean")]
    [InlineData("Container", "FieldPrivacy", "m_privacy")]
    [InlineData("Container", "FieldPrivacyType", "Container+PrivacySetting")]
    [InlineData("Container", "FieldCheckGuardStone", "m_checkGuardStone")]
    [InlineData("Container", "FieldCheckGuardStoneType", "System.Boolean")]
    [InlineData("Container", "FieldPiece", "m_piece")]
    [InlineData("Container", "FieldPieceType", "Piece")]
    [InlineData("Container", "FieldWagon", "m_wagon")]
    [InlineData("Container", "FieldWagonType", "Vagon")]
    [InlineData("ZNetView", "TypeName", "ZNetView")]
    [InlineData("ZNetView", "IsValid", "IsValid")]
    [InlineData("ZNetView", "IsOwner", "IsOwner")]
    [InlineData("ZNetView", "ClaimOwnership", "ClaimOwnership")]
    [InlineData("ZNetView", "GetZdo", "GetZDO")]
    [InlineData("ZNetView", "InvokeRpc", "InvokeRPC")]
    [InlineData("ZNetView", "InvokeRpcParametersOwner", "System.String,System.Object[]")]
    [InlineData("ZNetView", "InvokeRpcParametersTarget", "System.Int64,System.String,System.Object[]")]
    [InlineData("ZDO", "TypeName", "ZDO")]
    [InlineData("ZDO", "IsOwner", "IsOwner")]
    [InlineData("ZDO", "GetOwner", "GetOwner")]
    [InlineData("ZDO", "SetOwner", "SetOwner")]
    [InlineData("ZDO", "GetDataRevision", "get_DataRevision")]
    [InlineData("ZDO", "GetInt", "GetInt")]
    [InlineData("ZDO", "GetIntParametersHash", "System.Int32,System.Int32")]
    [InlineData("ZDO", "GetByteArray", "GetByteArray")]
    [InlineData("ZDO", "GetByteArrayParametersHash", "System.Int32,System.Byte[]")]
    [InlineData("ZDO", "Set", "Set")]
    [InlineData("ZDO", "SetParametersHashBytes", "System.Int32,System.Byte[]")]
    [InlineData("ZDO", "FieldUid", "m_uid")]
    [InlineData("ZDO", "FieldUidType", "ZDOID")]
    [InlineData("ZDOID", "TypeName", "ZDOID")]
    [InlineData("ZDOID", "GetUserId", "get_UserID")]
    [InlineData("ZDOVars", "TypeName", "ZDOVars")]
    [InlineData("ZDOVars", "FieldInUse", "s_inUse")]
    [InlineData("ZDOVars", "FieldInUseType", "System.Int32")]
    [InlineData("ZDOVars", "FieldItems", "s_items")]
    [InlineData("ZDOVars", "FieldItemsType", "System.Int32")]
    [InlineData("ZDOMan", "TypeName", "ZDOMan")]
    [InlineData("ZDOMan", "GetSessionId", "GetSessionID")]
    [InlineData("ZDOMan", "ForceSendZdo", "ForceSendZDO")]
    [InlineData("ZDOMan", "ForceSendZdoParametersPeer", "System.Int64,ZDOID")]
    [InlineData("ZDOMan", "RemovePeer", "RemovePeer")]
    [InlineData("ZDOMan", "ReleaseZdos", "ReleaseZDOS")]
    [InlineData("ZDOMan", "ReleaseNearbyZdos", "ReleaseNearbyZDOS")]
    [InlineData("ZDOMan", "IsInPeerActiveArea", "IsInPeerActiveArea")]
    [InlineData("ZNet", "TypeName", "ZNet")]
    [InlineData("ZNet", "GetUid", "GetUID")]
    [InlineData("ZNet", "GetPeer", "GetPeer")]
    [InlineData("ZNet", "GetPeerParametersUid", "System.Int64")]
    [InlineData("ZNet", "IsServer", "IsServer")]
    [InlineData("ZNet", "GetPlayerList", "GetPlayerList")]
    [InlineData("PrivateArea", "TypeName", "PrivateArea")]
    [InlineData("PrivateArea", "CheckAccess", "CheckAccess")]
    [InlineData("PrivateArea", "CheckAccessParametersPoint", "UnityEngine.Vector3,System.Single,System.Boolean,System.Boolean")]
    [InlineData("Piece", "TypeName", "Piece")]
    [InlineData("Piece", "GetCreator", "GetCreator")]
    [InlineData("Vagon", "TypeName", "Vagon")]
    [InlineData("Vagon", "InUse", "InUse")]
    [InlineData("Inventory", "TypeName", "Inventory")]
    [InlineData("Inventory", "Save", "Save")]
    [InlineData("InventoryGui", "TypeName", "InventoryGui")]
    [InlineData("InventoryGui", "Show", "Show")]
    [InlineData("InventoryGui", "Hide", "Hide")]
    [InlineData("InventoryGui", "CloseContainer", "CloseContainer")]
    [InlineData("InventoryGui", "UpdateContainer", "UpdateContainer")]
    [InlineData("InventoryGui", "IsContainerOpen", "IsContainerOpen")]
    [InlineData("InventoryGui", "FieldCurrentContainer", "m_currentContainer")]
    [InlineData("InventoryGui", "FieldCurrentContainerType", "Container")]
    [InlineData("Game", "TypeName", "Game")]
    [InlineData("Game", "GetPlayerProfile", "GetPlayerProfile")]
    [InlineData("PlayerProfile", "TypeName", "PlayerProfile")]
    [InlineData("PlayerProfile", "GetPlayerId", "GetPlayerID")]
    [InlineData("ItemData", "TypeName", "ItemDrop+ItemData")]
    [InlineData("ItemData", "FieldStack", "m_stack")]
    [InlineData("ItemData", "FieldStackType", "System.Int32")]
    [InlineData("ItemData", "FieldQuality", "m_quality")]
    [InlineData("ItemData", "FieldQualityType", "System.Int32")]
    [InlineData("ItemData", "FieldShared", "m_shared")]
    [InlineData("ItemData", "FieldSharedType", "ItemDrop+ItemData+SharedData")]
    [InlineData("ItemData", "FieldDropPrefab", "m_dropPrefab")]
    [InlineData("ItemData", "FieldDropPrefabType", "UnityEngine.GameObject")]
    [InlineData("SharedData", "TypeName", "ItemDrop+ItemData+SharedData")]
    [InlineData("SharedData", "FieldMaxStackSize", "m_maxStackSize")]
    [InlineData("SharedData", "FieldMaxStackSizeType", "System.Int32")]
    [InlineData("PlayerInfo", "TypeName", "ZNet+PlayerInfo")]
    [InlineData("PlayerInfo", "FieldCharacterId", "m_characterID")]
    [InlineData("PlayerInfo", "FieldCharacterIdType", "ZDOID")]
    [InlineData("TombStone", "TypeName", "TombStone")]
    [InlineData("Ship", "TypeName", "Ship")]
    [InlineData("Container", "Awake", "Awake")]
    [InlineData("Container", "AwakeParametersNone", "")]
    [InlineData("CraftingStation", "Start", "Start")]
    [InlineData("CraftingStation", "StartParametersNone", "")]
    [InlineData("CraftingStation", "OnDestroy", "OnDestroy")]
    [InlineData("CraftingStation", "OnDestroyParametersNone", "")]
    [InlineData("CraftingStation", "GetStationBuildRange", "GetStationBuildRange")]
    [InlineData("CraftingStation", "FieldNview", "m_nview")]
    [InlineData("CraftingStation", "FieldNviewType", "ZNetView")]
    [InlineData("ZNetView", "OnDestroy", "OnDestroy")]
    [InlineData("ZNetView", "OnDestroyParametersNone", "")]
    [InlineData("Inventory", "GetAllItems", "GetAllItems")]
    [InlineData("Inventory", "GetAllItemsParametersNone", "")]
    [InlineData("Inventory", "GetWidth", "GetWidth")]
    [InlineData("Inventory", "GetHeight", "GetHeight")]
    [InlineData("ZDOID", "GetId", "get_ID")]
    [InlineData("ZNet", "GetTimeSeconds", "GetTimeSeconds")]
    public void StarterSetHoldsTheContractedValues(string owner, string member, string expected)
    {
        Type holder = NestedType(owner);
        FieldInfo? field = holder.GetField(member, BindingFlags.Public | BindingFlags.Static);
        Assert.True(field is not null, $"{SharedTypeName}.{owner} has no constant {member}.");

        Assert.Equal(expected, (string?)field!.GetValue(null));
    }

    // GW-U-044
    [Fact]
    public void RemovedV1ConstantsNoLongerExist()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        Assert.Null(NestedType("Player").GetField("HaveRequirementsParametersPieceRequirements", All));
        Assert.Null(NestedType("Container").GetField("RpcOpenRespons", All));
    }

    // GW-U-044 (S-E002-005 edit, GW-U-561 and GW-U-562; S-E006-002 edit, GW-C-1260: plus the two version-handshake ids)
    // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1654): plus the three building ids of contract 8.1 (eighteen).
    // Edited by S-E003-005, ADR-GWIN-127: plus the three requirement-display ids (eighteen in all).
    // Edited by integration (S-E003-005 + S-E003-006): the union, 15 + 3 + 3 = 21 ids.
    // Edited by integration (S-E004-004): plus InventoryGuiUpdateContainer (GW-U-1330), 22 ids.
    // Edited by S-E004-012 (ADR-GWIN-136): plus InventoryGuiUpdateRecipe (GW-U-1790), 23 ids.
    [Fact]
    public void HarmonyPatchedHoldsExactlyTheTwentyThreePatchIds()
    {
        Type? holder = SharedType().GetNestedType("HarmonyPatched", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(holder is not null, $"{SharedTypeName} has no nested class HarmonyPatched.");

        FieldInfo[] fields = holder!.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var actual = fields
            .Select(field => (field.Name, Value: (string?)field.GetValue(null)))
            .OrderBy(pair => pair.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new (string Name, string? Value)[]
            {
                ("ContainerAwake", "Container.Awake()"),
                ("ContainerGetHoverText", "Container.GetHoverText()"),
                ("ContainerInteract", "Container.Interact(Humanoid,System.Boolean,System.Boolean)"),
                ("ContainerRpcOpenResponse", "Container.RPC_OpenResponse(System.Int64,System.Boolean)"),
                ("CraftingStationGetHoverText", "CraftingStation.GetHoverText()"),
                ("CraftingStationOnDestroy", "CraftingStation.OnDestroy()"),
                ("CraftingStationStart", "CraftingStation.Start()"),
                ("FejdStartupShowConnectError", "FejdStartup.ShowConnectError(ZNet+ConnectionStatus)"),
                ("HudSetupPieceInfo", "Hud.SetupPieceInfo(Piece)"),
                ("HumanoidIsItemEquiped", "Humanoid.IsItemEquiped(ItemDrop+ItemData)"),
                ("InventoryGridOnLeftDown", "InventoryGrid.OnLeftDown(UIInputHandler)"),
                ("InventoryGridUpdateGui", "InventoryGrid.UpdateGui(Player,ItemDrop+ItemData)"),
                ("InventoryGuiDoCrafting", "InventoryGui.DoCrafting(Player)"),
                ("InventoryGuiSetupRequirement", "InventoryGui.SetupRequirement(UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32)"),
                ("InventoryGuiSetupRequirementList", "InventoryGui.SetupRequirementList(System.Int32,Player,System.Boolean,System.Int32)"),
                ("InventoryGuiUpdateContainer", "InventoryGui.UpdateContainer(Player)"),
                ("InventoryGuiUpdateRecipe", "InventoryGui.UpdateRecipe(Player,System.Single)"), // Edited by S-E004-012 (ADR-GWIN-136).
                ("InventoryStackAll", "Inventory.StackAll(Inventory,System.Boolean)"),
                ("ItemDataGetTooltip", "ItemDrop+ItemData.GetTooltip(ItemDrop+ItemData,System.Int32,System.Boolean,System.Single,System.Int32,System.Boolean)"),
                ("PlayerConsumeResources", "Player.ConsumeResources(Piece+Requirement[],System.Int32,System.Int32,System.Int32)"),
                ("PlayerHaveRequirementItems", "Player.HaveRequirementItems(Recipe,System.Boolean,System.Int32,System.Int32)"),
                ("PlayerHaveRequirementsPiece", "Player.HaveRequirements(Piece,Player+RequirementMode)"),
                ("PlayerPlacePiece", "Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)"),
                ("PlayerTryPlacePiece", "Player.TryPlacePiece(Piece)"),
                ("SmelterAwake", "Smelter.Awake()"),
                ("SwitchGetHoverText", "Switch.GetHoverText()"),
                ("SwitchInteract", "Switch.Interact(Humanoid,System.Boolean,System.Boolean)"),
                ("TerminalInitTerminal", "Terminal.InitTerminal()"), // Edited by S-E010-002 (organize-console-command 6.2).
                ("ZNetOnNewConnection", "ZNet.OnNewConnection(ZNetPeer)"),
                ("ZNetViewOnDestroy", "ZNetView.OnDestroy()"),
            },
            actual);
    }

    // GW-U-789
    [Theory]
    [InlineData("Container", "RpcOpenResponseParametersUidGranted", "System.Int64,System.Boolean")]
    [InlineData("Container", "InteractParametersCharacterHoldAlt", "Humanoid,System.Boolean,System.Boolean")]
    [InlineData("PlayerInfo", "FieldName", "m_name")]
    [InlineData("PlayerInfo", "FieldNameType", "System.String")]
    [InlineData("HarmonyPatched", "ContainerInteract", "Container.Interact(Humanoid,System.Boolean,System.Boolean)")]
    [InlineData("HarmonyPatched", "ContainerRpcOpenResponse", "Container.RPC_OpenResponse(System.Int64,System.Boolean)")]
    public void ReservationConstantsHoldTheContractedValues(string owner, string member, string expected)
    {
        Type holder = NestedType(owner);
        FieldInfo? field = holder.GetField(member, BindingFlags.Public | BindingFlags.Static);
        Assert.True(field is not null, $"{SharedTypeName}.{owner} has no constant {member}.");

        Assert.Equal(expected, (string?)field!.GetValue(null));
    }

    // GW-U-789
    [Fact]
    public void ReservationConstantsHaveSummariesAndTheFileStaysLfWithoutBom()
    {
        string path = Path.Combine(RepoRootPath(), "src", "Shared", "PatchTargetNames.cs");
        byte[] bytes = File.ReadAllBytes(path);
        string text = File.ReadAllText(path);
        string[] lines = text.Split('\n');

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "PatchTargetNames.cs has a BOM.");
        Assert.DoesNotContain('\r', text);
        foreach (string member in new[] { "RpcOpenResponseParametersUidGranted", "InteractParametersCharacterHoldAlt", "FieldName", "FieldNameType", "ContainerInteract", "ContainerRpcOpenResponse" })
        {
            int index = Array.FindIndex(lines, line => line.TrimStart().StartsWith($"public const string {member} ", StringComparison.Ordinal));
            Assert.True(index > 0, $"{member} is not declared.");
            Assert.StartsWith("/// <summary>", lines[index - 1].TrimStart(), StringComparison.Ordinal);
        }
    }

    private static string RepoRootPath()
    {
        string dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "BaseStorage.sln")))
        {
            dir = Path.GetDirectoryName(dir) ?? throw new InvalidOperationException("BaseStorage.sln was not found above the test binaries.");
        }

        return dir;
    }

    // GW-U-045
    [Fact]
    public void TheFileIsCompiledIntoThisAssemblyAndNotReferenced()
    {
        Assert.Equal("BaseStorage.PatchTargets.Tests", SharedType().Assembly.GetName().Name);
    }

    // GW-U-045
    [Fact]
    public void PatchTargetNamesIsAnInternalStaticClassInTheSharedNamespace()
    {
        Type type = SharedType();

        Assert.True(type.IsNotPublic, "PatchTargetNames must not be public.");
        Assert.Equal("BaseStorage.Shared", type.Namespace);
        Assert.True(type.IsAbstract && type.IsSealed, "PatchTargetNames must be a static class (abstract and sealed).");
    }

    private static Type SharedType()
    {
        Type? type = typeof(PatchTargetNamesTests).Assembly.GetType(SharedTypeName, throwOnError: false);
        Assert.True(
            type is not null,
            $"{SharedTypeName} is not compiled into BaseStorage.PatchTargets.Tests (src/Shared/PatchTargetNames.cs is not linked yet).");
        return type!;
    }

    private static Type NestedType(string owner)
    {
        Type? nested = SharedType().GetNestedType(owner, BindingFlags.Public | BindingFlags.NonPublic);
        Assert.True(nested is not null, $"{SharedTypeName} has no nested class {owner}.");
        return nested!;
    }
}
