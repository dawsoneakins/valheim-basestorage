namespace BaseStorage.Shared;

/// <summary>Names of the game types, methods and fields that BaseStorage patches or reads by reflection.</summary>
internal static class PatchTargetNames
{
    /// <summary>The Player type.</summary>
    public static class Player
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Player";

        /// <summary>The HaveRequirements method.</summary>
        public const string HaveRequirements = "HaveRequirements";

        /// <summary>Parameter type full names of the HaveRequirements(Recipe, bool, int, int) overload, used for crafting and upgrading.</summary>
        public const string HaveRequirementsParametersRecipe =
            "Recipe,System.Boolean,System.Int32,System.Int32";

        /// <summary>Parameter type full names of the HaveRequirements(Piece, Player.RequirementMode) overload, used for building.</summary>
        public const string HaveRequirementsParametersPiece =
            "Piece,Player+RequirementMode";

        /// <summary>The m_localPlayer field.</summary>
        public const string FieldLocalPlayer = "m_localPlayer";

        /// <summary>The type full name of the m_localPlayer field.</summary>
        public const string FieldLocalPlayerType = "Player";

        /// <summary>The m_customData field.</summary>
        public const string FieldCustomData = "m_customData";

        /// <summary>The canonical type name of the m_customData field (rule PT1: generic arguments in brackets, no assembly qualification).</summary>
        public const string FieldCustomDataType = "System.Collections.Generic.Dictionary`2[System.String,System.String]";

        /// <summary>The HaveRequirementItems method.</summary>
        public const string HaveRequirementItems = "HaveRequirementItems";

        /// <summary>Parameter type full names of the HaveRequirementItems overload.</summary>
        public const string HaveRequirementItemsParametersRecipe = "Recipe,System.Boolean,System.Int32,System.Int32";

        /// <summary>The ConsumeResources method.</summary>
        public const string ConsumeResources = "ConsumeResources";

        /// <summary>Parameter type full names of the ConsumeResources overload.</summary>
        public const string ConsumeResourcesParametersRequirements =
            "Piece+Requirement[],System.Int32,System.Int32,System.Int32";

        /// <summary>The GetCurrentCraftingStation method.</summary>
        public const string GetCurrentCraftingStation = "GetCurrentCraftingStation";

        /// <summary>The NoCostCheat method.</summary>
        public const string NoCostCheat = "NoCostCheat";

        /// <summary>The RequiredCraftingStation method.</summary>
        public const string RequiredCraftingStation = "RequiredCraftingStation";

        /// <summary>The GetFirstRequiredItem method.</summary>
        public const string GetFirstRequiredItem = "GetFirstRequiredItem";

        /// <summary>The TryPlacePiece method, patched for build-from-base.</summary>
        public const string TryPlacePiece = "TryPlacePiece";

        /// <summary>Parameter type full names of the TryPlacePiece overload.</summary>
        public const string TryPlacePieceParametersPiece = "Piece";

        /// <summary>The PlacePiece method, patched for build-from-base.</summary>
        public const string PlacePiece = "PlacePiece";

        /// <summary>Parameter type full names of the PlacePiece overload.</summary>
        public const string PlacePieceParametersPiecePositionRotation =
            "Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean";

        /// <summary>The UpdatePlacementGhost method.</summary>
        public const string UpdatePlacementGhost = "UpdatePlacementGhost";

        /// <summary>The InPlaceMode method.</summary>
        public const string InPlaceMode = "InPlaceMode";

        /// <summary>The GetSelectedPiece method.</summary>
        public const string GetSelectedPiece = "GetSelectedPiece";

        /// <summary>The GetBuildStamina method.</summary>
        public const string GetBuildStamina = "GetBuildStamina";

        /// <summary>The GetPlaceDurability method.</summary>
        public const string GetPlaceDurability = "GetPlaceDurability";

        /// <summary>The UseStamina method.</summary>
        public const string UseStamina = "UseStamina";

        /// <summary>The RaiseSkill method.</summary>
        public const string RaiseSkill = "RaiseSkill";

        /// <summary>The IsDead method.</summary>
        public const string IsDead = "IsDead";

        /// <summary>The m_placementStatus field.</summary>
        public const string FieldPlacementStatus = "m_placementStatus";

        /// <summary>The type full name of the m_placementStatus field.</summary>
        public const string FieldPlacementStatusType = "Player+PlacementStatus";

        /// <summary>The m_placementGhost field.</summary>
        public const string FieldPlacementGhost = "m_placementGhost";

        /// <summary>The type full name of the m_placementGhost field.</summary>
        public const string FieldPlacementGhostType = "UnityEngine.GameObject";

        /// <summary>The m_lastToolUseTime field.</summary>
        public const string FieldLastToolUseTime = "m_lastToolUseTime";

        /// <summary>The type full name of the m_lastToolUseTime field.</summary>
        public const string FieldLastToolUseTimeType = "System.Single";

        /// <summary>The m_buildPieces field.</summary>
        public const string FieldBuildPieces = "m_buildPieces";

        /// <summary>The type full name of the m_buildPieces field.</summary>
        public const string FieldBuildPiecesType = "PieceTable";

        /// <summary>The m_buildRemoveDebt field.</summary>
        public const string FieldBuildRemoveDebt = "m_buildRemoveDebt";

        /// <summary>The type full name of the m_buildRemoveDebt field.</summary>
        public const string FieldBuildRemoveDebtType = "System.Int32";

        /// <summary>The TakeInput method (vanilla's gate for player input; the category hotkey honours it, S-E004-004).</summary>
        public const string TakeInput = "TakeInput";

        /// <summary>The GetHoverObject method (the object under the crosshair, read by the category hotkey).</summary>
        public const string GetHoverObject = "GetHoverObject";

        /// <summary>The GetPlayerID method (the lock holder id).</summary>
        public const string GetPlayerId = "GetPlayerID";

        /// <summary>The GetPlayerName method (the lock holder name).</summary>
        public const string GetPlayerName = "GetPlayerName";
    }

    /// <summary>The Container type.</summary>
    public static class Container
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Container";

        /// <summary>The RPC_RequestOpen handler.</summary>
        public const string RpcRequestOpen = "RPC_RequestOpen";

        /// <summary>The RPC_OpenResponse handler.</summary>
        public const string RpcOpenResponse = "RPC_OpenResponse";

        /// <summary>The RPC_RequestStack handler.</summary>
        public const string RpcRequestStack = "RPC_RequestStack";

        /// <summary>The RPC_StackResponse handler.</summary>
        public const string RpcStackResponse = "RPC_StackResponse";

        /// <summary>The inventory width field.</summary>
        public const string FieldWidth = "m_width";

        /// <summary>The type full name of the inventory width field.</summary>
        public const string FieldWidthType = "System.Int32";

        /// <summary>The inventory height field.</summary>
        public const string FieldHeight = "m_height";

        /// <summary>The type full name of the inventory height field.</summary>
        public const string FieldHeightType = "System.Int32";

        /// <summary>The Interact method.</summary>
        public const string Interact = "Interact";

        /// <summary>The CheckAccess method.</summary>
        public const string CheckAccess = "CheckAccess";

        /// <summary>The IsInUse method.</summary>
        public const string IsInUse = "IsInUse";

        /// <summary>The SetInUse method.</summary>
        public const string SetInUse = "SetInUse";

        /// <summary>The UpdateUseVisual method.</summary>
        public const string UpdateUseVisual = "UpdateUseVisual";

        /// <summary>The CheckForChanges method.</summary>
        public const string CheckForChanges = "CheckForChanges";

        /// <summary>The OnContainerChanged method.</summary>
        public const string OnContainerChanged = "OnContainerChanged";

        /// <summary>The Save method.</summary>
        public const string Save = "Save";

        /// <summary>The Load method.</summary>
        public const string Load = "Load";

        /// <summary>The m_nview field.</summary>
        public const string FieldNview = "m_nview";

        /// <summary>The type full name of the m_nview field.</summary>
        public const string FieldNviewType = "ZNetView";

        /// <summary>The m_inventory field.</summary>
        public const string FieldInventory = "m_inventory";

        /// <summary>The type full name of the m_inventory field.</summary>
        public const string FieldInventoryType = "Inventory";

        /// <summary>The m_inUse field.</summary>
        public const string FieldInUse = "m_inUse";

        /// <summary>The type full name of the m_inUse field.</summary>
        public const string FieldInUseType = "System.Boolean";

        /// <summary>The m_privacy field.</summary>
        public const string FieldPrivacy = "m_privacy";

        /// <summary>The type full name of the m_privacy field.</summary>
        public const string FieldPrivacyType = "Container+PrivacySetting";

        /// <summary>The m_checkGuardStone field.</summary>
        public const string FieldCheckGuardStone = "m_checkGuardStone";

        /// <summary>The type full name of the m_checkGuardStone field.</summary>
        public const string FieldCheckGuardStoneType = "System.Boolean";

        /// <summary>The m_piece field.</summary>
        public const string FieldPiece = "m_piece";

        /// <summary>The type full name of the m_piece field.</summary>
        public const string FieldPieceType = "Piece";

        /// <summary>The m_wagon field.</summary>
        public const string FieldWagon = "m_wagon";

        /// <summary>The type full name of the m_wagon field.</summary>
        public const string FieldWagonType = "Vagon";

        /// <summary>The Awake method.</summary>
        public const string Awake = "Awake";

        /// <summary>Parameter type full names of the Awake overload: none.</summary>
        public const string AwakeParametersNone = "";

        /// <summary>The GetHoverText method, which the chest hover line is appended to.</summary>
        public const string GetHoverText = "GetHoverText";

        /// <summary>Parameter type full names of the GetHoverText overload: none.</summary>
        public const string GetHoverTextParametersNone = "";

        /// <summary>Parameter type full names of the RPC_OpenResponse(long uid, bool granted) overload.</summary>
        public const string RpcOpenResponseParametersUidGranted = "System.Int64,System.Boolean";

        /// <summary>Parameter type full names of the Interact(Humanoid character, bool hold, bool alt) overload.</summary>
        public const string InteractParametersCharacterHoldAlt = "Humanoid,System.Boolean,System.Boolean";

        /// <summary>The GetInventory method, which the Organize layer reads and writes through.</summary>
        public const string GetInventory = "GetInventory";
    }

    /// <summary>The CraftingStation type.</summary>
    public static class CraftingStation
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "CraftingStation";

        /// <summary>The build range field.</summary>
        public const string FieldRangeBuild = "m_rangeBuild";

        /// <summary>The type full name of the build range field.</summary>
        public const string FieldRangeBuildType = "System.Single";

        /// <summary>The Start method.</summary>
        public const string Start = "Start";

        /// <summary>Parameter type full names of the Start overload: none.</summary>
        public const string StartParametersNone = "";

        /// <summary>The OnDestroy method.</summary>
        public const string OnDestroy = "OnDestroy";

        /// <summary>Parameter type full names of the OnDestroy overload: none.</summary>
        public const string OnDestroyParametersNone = "";

        /// <summary>The GetStationBuildRange method.</summary>
        public const string GetStationBuildRange = "GetStationBuildRange";

        /// <summary>The network view field.</summary>
        public const string FieldNview = "m_nview";

        /// <summary>The type full name of the m_nview field.</summary>
        public const string FieldNviewType = "ZNetView";

        /// <summary>The GetHoverText method, which the station hover line is appended to.</summary>
        public const string GetHoverText = "GetHoverText";

        /// <summary>Parameter type full names of the GetHoverText overload: none.</summary>
        public const string GetHoverTextParametersNone = "";

        /// <summary>The Upgrader field.</summary>
        public const string FieldUpgrader = "m_upgrader";

        /// <summary>The type full name of the Upgrader field.</summary>
        public const string FieldUpgraderType = "System.Boolean";

        /// <summary>The CraftingSkill field.</summary>
        public const string FieldCraftingSkill = "m_craftingSkill";

        /// <summary>The type full name of the CraftingSkill field.</summary>
        public const string FieldCraftingSkillType = "Skills+SkillType";

        /// <summary>The HaveBuildStationInRange method.</summary>
        public const string HaveBuildStationInRange = "HaveBuildStationInRange";
    }

    /// <summary>The ZNetView type.</summary>
    public static class ZNetView
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZNetView";

        /// <summary>The IsValid method.</summary>
        public const string IsValid = "IsValid";

        /// <summary>The IsOwner method.</summary>
        public const string IsOwner = "IsOwner";

        /// <summary>The ClaimOwnership method.</summary>
        public const string ClaimOwnership = "ClaimOwnership";

        /// <summary>The GetZDO method.</summary>
        public const string GetZdo = "GetZDO";

        /// <summary>The InvokeRPC method.</summary>
        public const string InvokeRpc = "InvokeRPC";

        /// <summary>Parameter type full names of the InvokeRPC overload.</summary>
        public const string InvokeRpcParametersOwner = "System.String,System.Object[]";

        /// <summary>Parameter type full names of the InvokeRPC overload that targets a peer.</summary>
        public const string InvokeRpcParametersTarget = "System.Int64,System.String,System.Object[]";

        /// <summary>The OnDestroy method.</summary>
        public const string OnDestroy = "OnDestroy";

        /// <summary>Parameter type full names of the OnDestroy overload: none.</summary>
        public const string OnDestroyParametersNone = "";
    }

    /// <summary>The ZDO type.</summary>
    public static class ZDO
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZDO";

        /// <summary>The IsOwner method.</summary>
        public const string IsOwner = "IsOwner";

        /// <summary>The GetOwner method.</summary>
        public const string GetOwner = "GetOwner";

        /// <summary>The SetOwner method.</summary>
        public const string SetOwner = "SetOwner";

        /// <summary>The get_DataRevision method.</summary>
        public const string GetDataRevision = "get_DataRevision";

        /// <summary>The GetInt method.</summary>
        public const string GetInt = "GetInt";

        /// <summary>Parameter type full names of the GetInt overload.</summary>
        public const string GetIntParametersHash = "System.Int32,System.Int32";

        /// <summary>The GetByteArray method.</summary>
        public const string GetByteArray = "GetByteArray";

        /// <summary>Parameter type full names of the GetByteArray overload.</summary>
        public const string GetByteArrayParametersHash = "System.Int32,System.Byte[]";

        /// <summary>The GetString method, which reads the stored chest category.</summary>
        public const string GetString = "GetString";

        /// <summary>Parameter type full names of the GetString(key, default) overload.</summary>
        public const string GetStringParametersName = "System.String,System.String";

        /// <summary>The Set method.</summary>
        public const string Set = "Set";

        /// <summary>Parameter type full names of the Set overload.</summary>
        public const string SetParametersHashBytes = "System.Int32,System.Byte[]";

        /// <summary>Parameter type full names of the Set(key, value) string overload, which writes the stored chest category.</summary>
        public const string SetParametersNameString = "System.String,System.String";

        /// <summary>The m_uid field.</summary>
        public const string FieldUid = "m_uid";

        /// <summary>The type full name of the m_uid field.</summary>
        public const string FieldUidType = "ZDOID";
    }

    /// <summary>The ZDOID type.</summary>
    public static class ZDOID
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZDOID";

        /// <summary>The get_UserID method.</summary>
        public const string GetUserId = "get_UserID";

        /// <summary>The get_ID method.</summary>
        public const string GetId = "get_ID";
    }

    /// <summary>The ZDOVars type.</summary>
    public static class ZDOVars
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZDOVars";

        /// <summary>The s_inUse field.</summary>
        public const string FieldInUse = "s_inUse";

        /// <summary>The type full name of the s_inUse field.</summary>
        public const string FieldInUseType = "System.Int32";

        /// <summary>The s_items field.</summary>
        public const string FieldItems = "s_items";

        /// <summary>The type full name of the s_items field.</summary>
        public const string FieldItemsType = "System.Int32";
    }

    /// <summary>The ZDOMan type.</summary>
    public static class ZDOMan
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZDOMan";

        /// <summary>The GetSessionID method.</summary>
        public const string GetSessionId = "GetSessionID";

        /// <summary>The ForceSendZDO method.</summary>
        public const string ForceSendZdo = "ForceSendZDO";

        /// <summary>Parameter type full names of the ForceSendZDO overload.</summary>
        public const string ForceSendZdoParametersPeer = "System.Int64,ZDOID";

        /// <summary>The RemovePeer method.</summary>
        public const string RemovePeer = "RemovePeer";

        /// <summary>The ReleaseZDOS method.</summary>
        public const string ReleaseZdos = "ReleaseZDOS";

        /// <summary>The ReleaseNearbyZDOS method.</summary>
        public const string ReleaseNearbyZdos = "ReleaseNearbyZDOS";

        /// <summary>The IsInPeerActiveArea method.</summary>
        public const string IsInPeerActiveArea = "IsInPeerActiveArea";
    }

    /// <summary>The ZNet type.</summary>
    public static class ZNet
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZNet";

        /// <summary>The GetUID method.</summary>
        public const string GetUid = "GetUID";

        /// <summary>The GetPeer method.</summary>
        public const string GetPeer = "GetPeer";

        /// <summary>Parameter type full names of the GetPeer overload.</summary>
        public const string GetPeerParametersUid = "System.Int64";

        /// <summary>The IsServer method.</summary>
        public const string IsServer = "IsServer";

        /// <summary>The GetPlayerList method.</summary>
        public const string GetPlayerList = "GetPlayerList";

        /// <summary>The GetTimeSeconds method.</summary>
        public const string GetTimeSeconds = "GetTimeSeconds";

        /// <summary>The OnNewConnection method, which registers the peer RPCs of a new connection; the version handshake postfix target.</summary>
        public const string OnNewConnection = "OnNewConnection";

        /// <summary>Parameter type full names of the OnNewConnection(ZNetPeer) overload.</summary>
        public const string OnNewConnectionParametersPeer = "ZNetPeer";

        /// <summary>The Disconnect(ZNetPeer) method, the vanilla path that drops a refused client.</summary>
        public const string Disconnect = "Disconnect";

        /// <summary>The GetPeers method, which lists the connected peers.</summary>
        public const string GetPeers = "GetPeers";

        /// <summary>The SetExternalError method, which raises a connection error through the vanilla error screen.</summary>
        public const string SetExternalError = "SetExternalError";

        /// <summary>The GetConnectionStatus method.</summary>
        public const string GetConnectionStatus = "GetConnectionStatus";

        /// <summary>The RPC_Error handler, whose RPC name is Error: the vanilla connection-error message a server sends a client.</summary>
        public const string RpcError = "RPC_Error";

        /// <summary>The get_instance method (the instance property getter).</summary>
        public const string GetInstance = "get_instance";
    }

    /// <summary>The PrivateArea type.</summary>
    public static class PrivateArea
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "PrivateArea";

        /// <summary>The CheckAccess method.</summary>
        public const string CheckAccess = "CheckAccess";

        /// <summary>Parameter type full names of the CheckAccess overload.</summary>
        public const string CheckAccessParametersPoint = "UnityEngine.Vector3,System.Single,System.Boolean,System.Boolean";
    }

    /// <summary>The Piece type.</summary>
    public static class Piece
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Piece";

        /// <summary>The GetCreator method.</summary>
        public const string GetCreator = "GetCreator";

        /// <summary>The m_craftingStation field.</summary>
        public const string FieldCraftingStation = "m_craftingStation";

        /// <summary>The type full name of the m_craftingStation field.</summary>
        public const string FieldCraftingStationType = "CraftingStation";

        /// <summary>The m_dlc field.</summary>
        public const string FieldDlc = "m_dlc";

        /// <summary>The type full name of the m_dlc field.</summary>
        public const string FieldDlcType = "System.String";

        /// <summary>The m_resources field.</summary>
        public const string FieldResources = "m_resources";

        /// <summary>The type full name of the m_resources field.</summary>
        public const string FieldResourcesType = "Piece+Requirement[]";

        /// <summary>The FreeBuildKey method.</summary>
        public const string FreeBuildKey = "FreeBuildKey";
    }

    /// <summary>The Vagon type.</summary>
    public static class Vagon
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Vagon";

        /// <summary>The InUse method.</summary>
        public const string InUse = "InUse";
    }

    /// <summary>The Inventory type.</summary>
    public static class Inventory
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Inventory";

        /// <summary>The Save method.</summary>
        public const string Save = "Save";

        /// <summary>The GetItemAt method.</summary>
        public const string GetItemAt = "GetItemAt";

        /// <summary>The CountItems method.</summary>
        public const string CountItems = "CountItems";

        /// <summary>The GetAllItems method.</summary>
        public const string GetAllItems = "GetAllItems";

        /// <summary>Parameter type full names of the GetAllItems overload: none.</summary>
        public const string GetAllItemsParametersNone = "";

        /// <summary>The GetWidth method.</summary>
        public const string GetWidth = "GetWidth";

        /// <summary>The GetHeight method.</summary>
        public const string GetHeight = "GetHeight";

        /// <summary>The ContainsItem method.</summary>
        public const string ContainsItem = "ContainsItem";

        /// <summary>The CanAddItem method.</summary>
        public const string CanAddItem = "CanAddItem";

        /// <summary>Parameter type full names of the CanAddItem overload.</summary>
        public const string CanAddItemParametersItemStack = "ItemDrop+ItemData,System.Int32";

        /// <summary>Parameter type full names of the CanAddItem overload.</summary>
        public const string CanAddItemParametersPrefabStack = "UnityEngine.GameObject,System.Int32";

        /// <summary>The AddItem method.</summary>
        public const string AddItem = "AddItem";

        /// <summary>Parameter type full names of the AddItem overload.</summary>
        public const string AddItemParametersItem = "ItemDrop+ItemData";

        /// <summary>The RemoveItem method.</summary>
        public const string RemoveItem = "RemoveItem";

        /// <summary>Parameter type full names of the RemoveItem overload.</summary>
        public const string RemoveItemParametersItemAmount = "ItemDrop+ItemData,System.Int32";
    }

    /// <summary>The InventoryGui type.</summary>
    public static class InventoryGui
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "InventoryGui";

        /// <summary>The Show method.</summary>
        public const string Show = "Show";

        /// <summary>The Hide method.</summary>
        public const string Hide = "Hide";

        /// <summary>The CloseContainer method.</summary>
        public const string CloseContainer = "CloseContainer";

        /// <summary>The UpdateContainer method.</summary>
        public const string UpdateContainer = "UpdateContainer";

        /// <summary>The IsContainerOpen method.</summary>
        public const string IsContainerOpen = "IsContainerOpen";

        /// <summary>The m_currentContainer field.</summary>
        public const string FieldCurrentContainer = "m_currentContainer";

        /// <summary>The type full name of the m_currentContainer field.</summary>
        public const string FieldCurrentContainerType = "Container";

        /// <summary>The get_instance method (the instance property getter).</summary>
        public const string GetInstance = "get_instance";

        /// <summary>The m_playerGrid field.</summary>
        public const string FieldPlayerGrid = "m_playerGrid";

        /// <summary>The type full name of the m_playerGrid field.</summary>
        public const string FieldPlayerGridType = "InventoryGrid";

        /// <summary>The m_dragItem field.</summary>
        public const string FieldDragItem = "m_dragItem";

        /// <summary>The type full name of the m_dragItem field.</summary>
        public const string FieldDragItemType = "ItemDrop+ItemData";

        /// <summary>The DoCrafting method.</summary>
        public const string DoCrafting = "DoCrafting";

        /// <summary>Parameter type full names of the DoCrafting overload.</summary>
        public const string DoCraftingParametersPlayer = "Player";

        /// <summary>The CraftRecipe field.</summary>
        public const string FieldCraftRecipe = "m_craftRecipe";

        /// <summary>The type full name of the CraftRecipe field.</summary>
        public const string FieldCraftRecipeType = "Recipe";

        /// <summary>The CraftUpgradeItem field.</summary>
        public const string FieldCraftUpgradeItem = "m_craftUpgradeItem";

        /// <summary>The type full name of the CraftUpgradeItem field.</summary>
        public const string FieldCraftUpgradeItemType = "ItemDrop+ItemData";

        /// <summary>The MultiCrafting field.</summary>
        public const string FieldMultiCrafting = "m_multiCrafting";

        /// <summary>The type full name of the MultiCrafting field.</summary>
        public const string FieldMultiCraftingType = "System.Boolean";

        /// <summary>The MultiCraftAmount field.</summary>
        public const string FieldMultiCraftAmount = "m_multiCraftAmount";

        /// <summary>The type full name of the MultiCraftAmount field.</summary>
        public const string FieldMultiCraftAmountType = "System.Int32";

        /// <summary>The CraftBonusAmount field.</summary>
        public const string FieldCraftBonusAmount = "m_craftBonusAmount";

        /// <summary>The type full name of the CraftBonusAmount field.</summary>
        public const string FieldCraftBonusAmountType = "System.Int32";

        /// <summary>The IsVisible method.</summary>
        public const string IsVisible = "IsVisible";

        /// <summary>The InCraftTab method.</summary>
        public const string InCraftTab = "InCraftTab";

        /// <summary>The SetupRequirement method (one declared overload).</summary>
        public const string SetupRequirement = "SetupRequirement";

        /// <summary>Parameter type full names of the SetupRequirement overload.</summary>
        public const string SetupRequirementParametersElement =
            "UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32";

        /// <summary>The SetupRequirementList method (one declared overload).</summary>
        public const string SetupRequirementList = "SetupRequirementList";

        /// <summary>Parameter type full names of the SetupRequirementList overload.</summary>
        public const string SetupRequirementListParametersQuality = "System.Int32,Player,System.Boolean,System.Int32";

        /// <summary>The m_selectedRecipe field.</summary>
        public const string FieldSelectedRecipe = "m_selectedRecipe";

        /// <summary>The type full name of the m_selectedRecipe field.</summary>
        public const string FieldSelectedRecipeType = "InventoryGui+RecipeDataPair";

        /// <summary>Parameter type full names of the UpdateContainer(Player) overload (the Category button postfix, S-E004-004).</summary>
        public const string UpdateContainerParametersPlayer = "Player";

        /// <summary>The m_takeAllButton field (the Category button template).</summary>
        public const string FieldTakeAllButton = "m_takeAllButton";

        /// <summary>The type full name of the m_takeAllButton field.</summary>
        public const string FieldTakeAllButtonType = "UnityEngine.UI.Button";

        /// <summary>The m_stackAllButton field (the other sibling of the Category button row).</summary>
        public const string FieldStackAllButton = "m_stackAllButton";

        /// <summary>The type full name of the m_stackAllButton field.</summary>
        public const string FieldStackAllButtonType = "UnityEngine.UI.Button";

        /// <summary>The m_container field (the container panel root).</summary>
        public const string FieldContainer = "m_container";

        /// <summary>The type full name of the m_container field.</summary>
        public const string FieldContainerType = "UnityEngine.RectTransform";

        /// <summary>The UpdateRecipe method (one declared overload; the station actions postfix, S-E004-012).</summary>
        public const string UpdateRecipe = "UpdateRecipe";

        /// <summary>Parameter type full names of the UpdateRecipe overload.</summary>
        public const string UpdateRecipeParametersPlayerDeltaTime = "Player,System.Single";

        /// <summary>The m_craftButton field (the Organize base button template).</summary>
        public const string FieldCraftButton = "m_craftButton";

        /// <summary>The type full name of the m_craftButton field.</summary>
        public const string FieldCraftButtonType = "UnityEngine.UI.Button";

        /// <summary>The m_recipeListRoot field (the recipe list rect of the layout).</summary>
        public const string FieldRecipeListRoot = "m_recipeListRoot";

        /// <summary>The type full name of the m_recipeListRoot field.</summary>
        public const string FieldRecipeListRootType = "UnityEngine.RectTransform";

        /// <summary>The m_recipeRequirementList field (the requirements rect of the layout).</summary>
        public const string FieldRecipeRequirementList = "m_recipeRequirementList";

        /// <summary>The type full name of the m_recipeRequirementList field.</summary>
        public const string FieldRecipeRequirementListType = "UnityEngine.GameObject[]";

        /// <summary>The m_craftingStationName field (the status line template).</summary>
        public const string FieldCraftingStationName = "m_craftingStationName";

        /// <summary>The type full name of the m_craftingStationName field.</summary>
        public const string FieldCraftingStationNameType = "TMPro.TMP_Text";

        /// <summary>The m_crafting field (the crafting panel root, the parent of the clones).</summary>
        public const string FieldCrafting = "m_crafting";

        /// <summary>The type full name of the m_crafting field.</summary>
        public const string FieldCraftingType = "UnityEngine.RectTransform";
    }

    /// <summary>The UIGamePad type (its hint is hidden on the cloned station buttons).</summary>
    public static class UIGamePad
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "UIGamePad";

        /// <summary>The m_hint field.</summary>
        public const string FieldHint = "m_hint";

        /// <summary>The type full name of the m_hint field.</summary>
        public const string FieldHintType = "UnityEngine.GameObject";
    }

    /// <summary>The Game type.</summary>
    public static class Game
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Game";

        /// <summary>The GetPlayerProfile method.</summary>
        public const string GetPlayerProfile = "GetPlayerProfile";

        /// <summary>The GetInstance method.</summary>
        public const string GetInstance = "get_instance";

        /// <summary>The IncrementPlayerStat method.</summary>
        public const string IncrementPlayerStat = "IncrementPlayerStat";

        /// <summary>The m_durabilityRate field.</summary>
        public const string FieldDurabilityRate = "m_durabilityRate";

        /// <summary>The type full name of the m_durabilityRate field.</summary>
        public const string FieldDurabilityRateType = "System.Single";
    }

    /// <summary>The PlayerProfile type.</summary>
    public static class PlayerProfile
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "PlayerProfile";

        /// <summary>The GetPlayerID method.</summary>
        public const string GetPlayerId = "GetPlayerID";

        /// <summary>The GetName method (the lock holder name).</summary>
        public const string GetName = "GetName";
    }

    /// <summary>The InventoryGrid type.</summary>
    public static class InventoryGrid
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "InventoryGrid";

        /// <summary>The OnLeftDown method.</summary>
        public const string OnLeftDown = "OnLeftDown";

        /// <summary>Parameter type full names of the OnLeftDown(UIInputHandler) overload.</summary>
        public const string OnLeftDownParametersHandler = "UIInputHandler";

        /// <summary>The UpdateGui method.</summary>
        public const string UpdateGui = "UpdateGui";

        /// <summary>Parameter type full names of the UpdateGui(Player, ItemDrop.ItemData) overload.</summary>
        public const string UpdateGuiParametersPlayerDragItem = "Player,ItemDrop+ItemData";

        /// <summary>The GetButtonPos method.</summary>
        public const string GetButtonPos = "GetButtonPos";

        /// <summary>The GetElement method.</summary>
        public const string GetElement = "GetElement";

        /// <summary>The GetInventory method.</summary>
        public const string GetInventory = "GetInventory";
    }

    /// <summary>The InventoryElement type.</summary>
    public static class InventoryElement
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "InventoryElement";

        /// <summary>The m_icon field.</summary>
        public const string FieldIcon = "m_icon";

        /// <summary>The type full name of the m_icon field.</summary>
        public const string FieldIconType = "UnityEngine.UI.Image";

        /// <summary>The m_queued field.</summary>
        public const string FieldQueued = "m_queued";

        /// <summary>The type full name of the m_queued field.</summary>
        public const string FieldQueuedType = "UnityEngine.UI.Image";
    }

    /// <summary>The ItemDrop.ItemData nested type.</summary>
    public static class ItemData
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ItemDrop+ItemData";

        /// <summary>The GetTooltip method, which the category line is appended to.</summary>
        public const string GetTooltip = "GetTooltip";

        /// <summary>Parameter type full names of the static GetTooltip overload the grid and the recipe preview share.</summary>
        public const string GetTooltipParametersItem = "ItemDrop+ItemData,System.Int32,System.Boolean,System.Single,System.Int32,System.Boolean";

        /// <summary>The stack size field.</summary>
        public const string FieldStack = "m_stack";

        /// <summary>The type full name of the m_stack field.</summary>
        public const string FieldStackType = "System.Int32";

        /// <summary>The quality field.</summary>
        public const string FieldQuality = "m_quality";

        /// <summary>The type full name of the m_quality field.</summary>
        public const string FieldQualityType = "System.Int32";

        /// <summary>The shared data field.</summary>
        public const string FieldShared = "m_shared";

        /// <summary>The type full name of the m_shared field.</summary>
        public const string FieldSharedType = "ItemDrop+ItemData+SharedData";

        /// <summary>The drop prefab field.</summary>
        public const string FieldDropPrefab = "m_dropPrefab";

        /// <summary>The type full name of the m_dropPrefab field.</summary>
        public const string FieldDropPrefabType = "UnityEngine.GameObject";

        /// <summary>The Clone method.</summary>
        public const string Clone = "Clone";

        /// <summary>The m_durability field.</summary>
        public const string FieldDurability = "m_durability";

        /// <summary>The type full name of the m_durability field.</summary>
        public const string FieldDurabilityType = "System.Single";

        /// <summary>The m_equipped field.</summary>
        public const string FieldEquipped = "m_equipped";

        /// <summary>The type full name of the m_equipped field.</summary>
        public const string FieldEquippedType = "System.Boolean";

        /// <summary>The m_gridPos field.</summary>
        public const string FieldGridPos = "m_gridPos";

        /// <summary>The type full name of the m_gridPos field.</summary>
        public const string FieldGridPosType = "Vector2i";
    }

    /// <summary>The ItemDrop.ItemData.SharedData nested type.</summary>
    public static class SharedData
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ItemDrop+ItemData+SharedData";

        /// <summary>The maximum stack size field.</summary>
        public const string FieldMaxStackSize = "m_maxStackSize";

        /// <summary>The type full name of the m_maxStackSize field.</summary>
        public const string FieldMaxStackSizeType = "System.Int32";

        /// <summary>The m_name field.</summary>
        public const string FieldName = "m_name";

        /// <summary>The type full name of the m_name field.</summary>
        public const string FieldNameType = "System.String";

        /// <summary>The MaxQuality field.</summary>
        public const string FieldMaxQuality = "m_maxQuality";

        /// <summary>The type full name of the MaxQuality field.</summary>
        public const string FieldMaxQualityType = "System.Int32";

        /// <summary>The Dlc field.</summary>
        public const string FieldDlc = "m_dlc";

        /// <summary>The type full name of the Dlc field.</summary>
        public const string FieldDlcType = "System.String";

        /// <summary>The m_useDurability field.</summary>
        public const string FieldUseDurability = "m_useDurability";

        /// <summary>The type full name of the m_useDurability field.</summary>
        public const string FieldUseDurabilityType = "System.Boolean";

        /// <summary>The m_buildEffect field.</summary>
        public const string FieldBuildEffect = "m_buildEffect";

        /// <summary>The type full name of the m_buildEffect field.</summary>
        public const string FieldBuildEffectType = "EffectList";

        /// <summary>The m_itemType field.</summary>
        public const string FieldItemType = "m_itemType";

        /// <summary>The type full name of the m_itemType field.</summary>
        public const string FieldItemTypeType = "ItemDrop+ItemData+ItemType";
    }

    /// <summary>The ZNet.PlayerInfo nested type.</summary>
    public static class PlayerInfo
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZNet+PlayerInfo";

        /// <summary>The character id field.</summary>
        public const string FieldCharacterId = "m_characterID";

        /// <summary>The type full name of the m_characterID field.</summary>
        public const string FieldCharacterIdType = "ZDOID";

        /// <summary>The player name field.</summary>
        public const string FieldName = "m_name";

        /// <summary>The type full name of the m_name field.</summary>
        public const string FieldNameType = "System.String";
    }

    /// <summary>The TombStone type.</summary>
    public static class TombStone
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "TombStone";
    }

    /// <summary>The Ship type.</summary>
    public static class Ship
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Ship";
    }

    /// <summary>The ZNetPeer type: one connected peer.</summary>
    public static class ZNetPeer
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZNetPeer";

        /// <summary>The m_rpc field, the peer's RPC channel.</summary>
        public const string FieldRpc = "m_rpc";

        /// <summary>The type full name of the m_rpc field.</summary>
        public const string FieldRpcType = "ZRpc";

        /// <summary>The m_server field, true when the remote peer is the server.</summary>
        public const string FieldServer = "m_server";

        /// <summary>The type full name of the m_server field.</summary>
        public const string FieldServerType = "System.Boolean";
    }

    /// <summary>The ZRpc type: the per-peer RPC channel.</summary>
    public static class ZRpc
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZRpc";

        /// <summary>The Register method, which binds an RPC name to a handler.</summary>
        public const string Register = "Register";

        /// <summary>Canonical parameter type names of the Register&lt;T&gt;(string, Action&lt;ZRpc, T&gt;) overload (rule PT2: a generic method parameter appears by name).</summary>
        public const string RegisterParametersHandler = "System.String,System.Action`2[ZRpc,T]";

        /// <summary>The Invoke method, which sends one RPC to the peer.</summary>
        public const string Invoke = "Invoke";
    }

    /// <summary>The ZPackage type: the RPC payload buffer.</summary>
    public static class ZPackage
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZPackage";

        /// <summary>The Write method.</summary>
        public const string Write = "Write";

        /// <summary>Parameter type full names of the Write(byte[]) overload, which writes a length-prefixed byte array.</summary>
        public const string WriteParametersBytes = "System.Byte[]";

        /// <summary>The ReadByteArray method.</summary>
        public const string ReadByteArray = "ReadByteArray";

        /// <summary>Parameter type full names of the ReadByteArray() overload: none.</summary>
        public const string ReadByteArrayParametersNone = "";
    }

    /// <summary>The FejdStartup type: the main menu, which shows the connection-error panel.</summary>
    public static class FejdStartup
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "FejdStartup";

        /// <summary>The ShowConnectError method, which fills the connection-error panel; the version handshake postfix target.</summary>
        public const string ShowConnectError = "ShowConnectError";

        /// <summary>Parameter type full names of the ShowConnectError(ZNet.ConnectionStatus) overload.</summary>
        public const string ShowConnectErrorParametersStatus = "ZNet+ConnectionStatus";

        /// <summary>The m_connectionFailedError field, the error text of the panel.</summary>
        public const string FieldConnectionFailedError = "m_connectionFailedError";

        /// <summary>The type full name of the m_connectionFailedError field.</summary>
        public const string FieldConnectionFailedErrorType = "TMPro.TMP_Text";

        /// <summary>The m_connectionFailedPanel field, the panel itself.</summary>
        public const string FieldConnectionFailedPanel = "m_connectionFailedPanel";

        /// <summary>The type full name of the m_connectionFailedPanel field.</summary>
        public const string FieldConnectionFailedPanelType = "UnityEngine.GameObject";
    }

    /// <summary>The ZNet.ConnectionStatus nested enum.</summary>
    public static class ConnectionStatus
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZNet+ConnectionStatus";

        /// <summary>The ErrorVersion member, the vanilla incompatible-version error.</summary>
        public const string FieldErrorVersion = "ErrorVersion";

        /// <summary>The type full name of the ErrorVersion member.</summary>
        public const string FieldErrorVersionType = "ZNet+ConnectionStatus";
    }

    /// <summary>The Humanoid type.</summary>
    public static class Humanoid
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Humanoid";

        /// <summary>The GetInventory method.</summary>
        public const string GetInventory = "GetInventory";

        /// <summary>The GetRightItem method.</summary>
        public const string GetRightItem = "GetRightItem";

        /// <summary>The IsItemEquiped method.</summary>
        public const string IsItemEquiped = "IsItemEquiped";
    }

    /// <summary>The Recipe type.</summary>
    public static class Recipe
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Recipe";

        /// <summary>The Item field.</summary>
        public const string FieldItem = "m_item";

        /// <summary>The type full name of the Item field.</summary>
        public const string FieldItemType = "ItemDrop";

        /// <summary>The Amount field.</summary>
        public const string FieldAmount = "m_amount";

        /// <summary>The type full name of the Amount field.</summary>
        public const string FieldAmountType = "System.Int32";

        /// <summary>The Resources field.</summary>
        public const string FieldResources = "m_resources";

        /// <summary>The type full name of the Resources field.</summary>
        public const string FieldResourcesType = "Piece+Requirement[]";

        /// <summary>The RequireOnlyOneIngredient field.</summary>
        public const string FieldRequireOnlyOneIngredient = "m_requireOnlyOneIngredient";

        /// <summary>The type full name of the RequireOnlyOneIngredient field.</summary>
        public const string FieldRequireOnlyOneIngredientType = "System.Boolean";
    }

    /// <summary>The Piece.Requirement nested type.</summary>
    public static class Requirement
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Piece+Requirement";

        /// <summary>The GetAmount method.</summary>
        public const string GetAmount = "GetAmount";

        /// <summary>The ResItem field.</summary>
        public const string FieldResItem = "m_resItem";

        /// <summary>The type full name of the ResItem field.</summary>
        public const string FieldResItemType = "ItemDrop";

        /// <summary>The UpgraderResource field.</summary>
        public const string FieldUpgraderResource = "m_upgraderResource";

        /// <summary>The type full name of the UpgraderResource field.</summary>
        public const string FieldUpgraderResourceType = "System.Boolean";

        /// <summary>The m_amount field.</summary>
        public const string FieldAmount = "m_amount";

        /// <summary>The type full name of the m_amount field.</summary>
        public const string FieldAmountType = "System.Int32";
    }

    /// <summary>The ItemDrop type.</summary>
    public static class ItemDrop
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ItemDrop";

        /// <summary>The DropItem method.</summary>
        public const string DropItem = "DropItem";

        /// <summary>The ItemData field.</summary>
        public const string FieldItemData = "m_itemData";

        /// <summary>The type full name of the ItemData field.</summary>
        public const string FieldItemDataType = "ItemDrop+ItemData";
    }

    /// <summary>The DLCMan type.</summary>
    public static class DLCMan
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "DLCMan";

        /// <summary>The GetInstance method.</summary>
        public const string GetInstance = "get_instance";

        /// <summary>The IsDlcInstalled method.</summary>
        public const string IsDlcInstalled = "IsDLCInstalled";

        /// <summary>Parameter type full names of the IsDlcInstalled overload.</summary>
        public const string IsDlcInstalledParametersName = "System.String";
    }

    /// <summary>The ZoneSystem type.</summary>
    public static class ZoneSystem
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ZoneSystem";

        /// <summary>The GetInstance method.</summary>
        public const string GetInstance = "get_instance";

        /// <summary>The GetGlobalKey method.</summary>
        public const string GetGlobalKey = "GetGlobalKey";

        /// <summary>Parameter type full names of the GetGlobalKey overload.</summary>
        public const string GetGlobalKeyParametersKey = "GlobalKeys";
    }

    /// <summary>The InventoryGui.RecipeDataPair nested struct.</summary>
    public static class RecipeDataPair
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "InventoryGui+RecipeDataPair";

        /// <summary>The Recipe property getter.</summary>
        public const string GetRecipe = "get_Recipe";
    }

    /// <summary>The Character type.</summary>
    public static class Character
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Character";

        /// <summary>The GetZDOID method.</summary>
        public const string GetZdoid = "GetZDOID";
    }

    /// <summary>The Hud type (requirement display, S-E003-005, and build from base, S-E003-006).</summary>
    public static class Hud
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Hud";

        /// <summary>The SetupPieceInfo method.</summary>
        public const string SetupPieceInfo = "SetupPieceInfo";

        /// <summary>Parameter type full names of the SetupPieceInfo overload.</summary>
        public const string SetupPieceInfoParametersPiece = "Piece";

        /// <summary>The GetInstance method.</summary>
        public const string GetInstance = "get_instance";

        /// <summary>The IsPieceSelectionVisible method.</summary>
        public const string IsPieceSelectionVisible = "IsPieceSelectionVisible";

        /// <summary>The m_buildUi field.</summary>
        public const string FieldBuildUi = "m_buildUi";

        /// <summary>The type full name of the m_buildUi field.</summary>
        public const string FieldBuildUiType = "BuildUi";
    }

    /// <summary>The BuildUi type.</summary>
    public static class BuildUi
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "BuildUi";

        /// <summary>The AddRecentPiece method.</summary>
        public const string AddRecentPiece = "AddRecentPiece";
    }

    /// <summary>The PieceTable type.</summary>
    public static class PieceTable
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "PieceTable";

        /// <summary>The m_skill field.</summary>
        public const string FieldSkill = "m_skill";

        /// <summary>The type full name of the m_skill field.</summary>
        public const string FieldSkillType = "Skills+SkillType";
    }

    /// <summary>The EffectList type.</summary>
    public static class EffectList
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "EffectList";

        /// <summary>The Create method.</summary>
        public const string Create = "Create";
    }

    /// <summary>The ObjectDB type.</summary>
    public static class ObjectDB
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "ObjectDB";

        /// <summary>The GetItemPrefab method; the Organize layer uses the name overload for a fallback clone.</summary>
        public const string GetItemPrefab = "GetItemPrefab";

        /// <summary>Parameter type full names of the GetItemPrefab(string name) overload.</summary>
        public const string GetItemPrefabParametersName = "System.String";
    }

    /// <summary>The UnifiedPopup type (the vanilla popup stack of the Organize preview).</summary>
    public static class UnifiedPopup
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "UnifiedPopup";

        /// <summary>The static Push method.</summary>
        public const string Push = "Push";

        /// <summary>The static Pop method.</summary>
        public const string Pop = "Pop";

        /// <summary>The static IsAvailable method.</summary>
        public const string IsAvailable = "IsAvailable";

        /// <summary>The static IsVisible method.</summary>
        public const string IsVisible = "IsVisible";
    }

    /// <summary>The YesNoPopup type (the confirm and cancel popup of the Organize preview).</summary>
    public static class YesNoPopup
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "YesNoPopup";
    }

    /// <summary>The Chat type (the hotkey is ignored while the chat has focus).</summary>
    public static class Chat
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Chat";

        /// <summary>The HasFocus method.</summary>
        public const string HasFocus = "HasFocus";
    }

    /// <summary>The Console type (the hotkey is ignored while the console is open).</summary>
    public static class Console
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Console";

        /// <summary>The static IsVisible method.</summary>
        public const string IsVisible = "IsVisible";
    }

    /// <summary>The Menu type (the hotkey is ignored while the menu is open).</summary>
    public static class Menu
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Menu";

        /// <summary>The static IsVisible method.</summary>
        public const string IsVisible = "IsVisible";
    }

    /// <summary>The TextInput type (the hotkey is ignored while a text input is open).</summary>
    public static class TextInput
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "TextInput";

        /// <summary>The static IsVisible method.</summary>
        public const string IsVisible = "IsVisible";
    }

    /// <summary>The Terminal type (the base of the console and the chat; the console command registration, S-E010-002).</summary>
    public static class Terminal
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Terminal";

        /// <summary>The static InitTerminal method, which registers the vanilla commands; its postfix registers basestorage.</summary>
        public const string InitTerminal = "InitTerminal";

        /// <summary>Parameter type full names of the InitTerminal() overload (none).</summary>
        public const string InitTerminalParametersNone = "";

        /// <summary>The AddString method; the command prints its reply line through the string overload.</summary>
        public const string AddString = "AddString";

        /// <summary>Parameter type full names of the AddString(string text) overload.</summary>
        public const string AddStringParametersText = "System.String";
    }

    /// <summary>The Terminal.ConsoleCommand type (the registration target; its constructor adds the command to Terminal.commands).</summary>
    public static class ConsoleCommand
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Terminal+ConsoleCommand";
    }

    /// <summary>The Terminal.ConsoleEvent delegate type (the command handler).</summary>
    public static class ConsoleEvent
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Terminal+ConsoleEvent";
    }

    /// <summary>The Terminal.ConsoleEventArgs type (the handler's argument).</summary>
    public static class ConsoleEventArgs
    {
        /// <summary>The reflection full name of the type.</summary>
        public const string TypeName = "Terminal+ConsoleEventArgs";

        /// <summary>The Args field: the typed line split on spaces, the command word first.</summary>
        public const string FieldArgs = "Args";

        /// <summary>The type full name of the Args field.</summary>
        public const string FieldArgsType = "System.String[]";

        /// <summary>The Context field: the terminal (console or chat) that ran the command.</summary>
        public const string FieldContext = "Context";

        /// <summary>The type full name of the Context field.</summary>
        public const string FieldContextType = "Terminal";
    }

    /// <summary>
    /// The methods that the Plugin patches with Harmony. Each constant is a target id composed as
    /// TypeName + "." + Method + "(" + Parameters + ")".
    /// </summary>
    public static class HarmonyPatched
    {
        /// <summary>The InventoryGrid.OnLeftDown(UIInputHandler) prefix of the favourites toggle.</summary>
        public const string InventoryGridOnLeftDown = "InventoryGrid.OnLeftDown(UIInputHandler)";

        /// <summary>The InventoryGrid.UpdateGui(Player, ItemDrop.ItemData) postfix of the favourite markers.</summary>
        public const string InventoryGridUpdateGui = "InventoryGrid.UpdateGui(Player,ItemDrop+ItemData)";

        /// <summary>The Container.Awake patch target.</summary>
        public const string ContainerAwake = "Container.Awake()";

        /// <summary>The Container.GetHoverText patch target (the chest hover line).</summary>
        public const string ContainerGetHoverText = "Container.GetHoverText()";

        /// <summary>The CraftingStation.GetHoverText patch target (the station hover line).</summary>
        public const string CraftingStationGetHoverText = "CraftingStation.GetHoverText()";

        /// <summary>The CraftingStation.OnDestroy patch target.</summary>
        public const string CraftingStationOnDestroy = "CraftingStation.OnDestroy()";

        /// <summary>The CraftingStation.Start patch target.</summary>
        public const string CraftingStationStart = "CraftingStation.Start()";

        /// <summary>The ZNetView.OnDestroy patch target.</summary>
        public const string ZNetViewOnDestroy = "ZNetView.OnDestroy()";

        /// <summary>The Container.Interact patch target.</summary>
        public const string ContainerInteract = "Container.Interact(Humanoid,System.Boolean,System.Boolean)";

        /// <summary>The Container.RPC_OpenResponse patch target.</summary>
        public const string ContainerRpcOpenResponse = "Container.RPC_OpenResponse(System.Int64,System.Boolean)";

        /// <summary>The ZNet.OnNewConnection(ZNetPeer) postfix of the version handshake.</summary>
        public const string ZNetOnNewConnection = "ZNet.OnNewConnection(ZNetPeer)";

        /// <summary>The FejdStartup.ShowConnectError(ZNet.ConnectionStatus) postfix that appends the version mismatch to the error panel.</summary>
        public const string FejdStartupShowConnectError = "FejdStartup.ShowConnectError(ZNet+ConnectionStatus)";

        /// <summary>The Player.HaveRequirementItems prefix of the craft-from-base patch.</summary>
        public const string PlayerHaveRequirementItems =
            "Player.HaveRequirementItems(Recipe,System.Boolean,System.Int32,System.Int32)";

        /// <summary>The Player.ConsumeResources prefix of the craft-from-base patch.</summary>
        public const string PlayerConsumeResources =
            "Player.ConsumeResources(Piece+Requirement[],System.Int32,System.Int32,System.Int32)";

        /// <summary>The InventoryGui.DoCrafting prefix of the craft-from-base patch.</summary>
        public const string InventoryGuiDoCrafting = "InventoryGui.DoCrafting(Player)";

        /// <summary>The InventoryGui.SetupRequirement postfix of the requirement display.</summary>
        public const string InventoryGuiSetupRequirement =
            "InventoryGui.SetupRequirement(UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32)";

        /// <summary>The InventoryGui.SetupRequirementList prefix and postfix of the requirement display.</summary>
        public const string InventoryGuiSetupRequirementList =
            "InventoryGui.SetupRequirementList(System.Int32,Player,System.Boolean,System.Int32)";

        /// <summary>The Hud.SetupPieceInfo prefix and postfix of the requirement display.</summary>
        public const string HudSetupPieceInfo = "Hud.SetupPieceInfo(Piece)";

        /// <summary>The Player.HaveRequirements(Piece, Player.RequirementMode) prefix of the build-from-base patch.</summary>
        public const string PlayerHaveRequirementsPiece = "Player.HaveRequirements(Piece,Player+RequirementMode)";

        /// <summary>The Player.TryPlacePiece prefix and postfix of the build-from-base patch.</summary>
        public const string PlayerTryPlacePiece = "Player.TryPlacePiece(Piece)";

        /// <summary>The Player.PlacePiece prefix of the build-from-base patch.</summary>
        public const string PlayerPlacePiece =
            "Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)";

        /// <summary>The InventoryGui.UpdateContainer(Player) postfix of the Category button (S-E004-004).</summary>
        public const string InventoryGuiUpdateContainer = "InventoryGui.UpdateContainer(Player)";

        /// <summary>The InventoryGui.UpdateRecipe(Player, float) postfix of the station buttons (S-E004-012).</summary>
        public const string InventoryGuiUpdateRecipe = "InventoryGui.UpdateRecipe(Player,System.Single)";

        /// <summary>The Terminal.InitTerminal() postfix that registers the basestorage console command (S-E010-002).</summary>
        public const string TerminalInitTerminal = "Terminal.InitTerminal()";

        /// <summary>The static ItemData.GetTooltip postfix of the category line (S-B-001).</summary>
        public const string ItemDataGetTooltip = "ItemDrop+ItemData.GetTooltip(ItemDrop+ItemData,System.Int32,System.Boolean,System.Single,System.Int32,System.Boolean)";
    }
}
