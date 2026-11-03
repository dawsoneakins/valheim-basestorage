namespace BaseStorage.PatchTargets.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using BaseStorage.PatchTargets.Tests.Checking;
using BaseStorage.PatchTargets.Tests.Infrastructure;
using BaseStorage.Shared;

/// <summary>
/// AC-3: the Harmony attributes and PatchTargetNames.HarmonyPatched agree on the built Plugin (contract 4.7.4).
/// Plugin-backed: skipped with BST2002 to BST2004 when the game or the built Plugin is missing. The Plugin is read
/// as metadata only and is never loaded or built here.
/// </summary>
[Collection("ValheimAssembly")]
public sealed class HarmonyAgreementTests
{
    private readonly ValheimAssemblyFixture fixture;

    public HarmonyAgreementTests(ValheimAssemblyFixture fixture)
    {
        this.fixture = fixture;
    }

    // GW-I-011
    [RequiresPluginFact]
    public void HarmonyAttributesAndHarmonyPatchedAgreeExactlyOnTheRealPlugin()
    {
        string plugin = BuildValues.PluginAssemblyPath;

        // Non-vacuous: the file is the BaseStorage assembly and it declares the Plugin entry class.
        using FileStream stream = File.OpenRead(plugin);
        using PEReader pe = new(stream);
        MetadataReader metadata = pe.GetMetadataReader();
        Assert.Equal("BaseStorage", metadata.GetString(metadata.GetAssemblyDefinition().Name));

        string[] typeNames = metadata.TypeDefinitions
            .Select(handle => metadata.GetTypeDefinition(handle))
            .Select(type => metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name))
            .ToArray();
        Assert.Contains("BaseStorage.Plugin.BaseStoragePlugin", typeNames);

        IReadOnlyList<PatchClass> patchClasses = HarmonyAttributeReader.Read(plugin, null, new GameTypeResolver(fixture));
        IReadOnlyList<HarmonyEntry> harmonyPatched = TargetList.HarmonyPatchedEntries(typeof(PatchTargetNames));

        string[] expectedIds =
        {
            "Container.Awake()",
            "Container.GetHoverText()",
            "Container.Interact(Humanoid,System.Boolean,System.Boolean)",
            "Container.RPC_OpenResponse(System.Int64,System.Boolean)",
            "CookingStation.Awake()",
            "CookingStation.GetHoverText()",
            "CookingStation.Interact(Humanoid,System.Boolean,System.Boolean)",
            "CraftingStation.GetHoverText()",
            "CraftingStation.OnDestroy()",
            "CraftingStation.Start()",
            "FejdStartup.ShowConnectError(ZNet+ConnectionStatus)",
            "Fermenter.Awake()",
            "Fermenter.GetHoverText()",
            "Fermenter.Interact(Humanoid,System.Boolean,System.Boolean)",
            "Fireplace.Awake()",
            "Fireplace.GetHoverText()",
            "Fireplace.Interact(Humanoid,System.Boolean,System.Boolean)",
            "Hud.SetupPieceInfo(Piece)",
            "Humanoid.IsItemEquiped(ItemDrop+ItemData)",
            "Inventory.StackAll(Inventory,System.Boolean)",
            "InventoryGrid.OnLeftDown(UIInputHandler)",
            "InventoryGrid.UpdateGamepad()",
            "InventoryGrid.UpdateGui(Player,ItemDrop+ItemData)",
            "InventoryGui.DoCrafting(Player)",
            "InventoryGui.Hide()",
            "InventoryGui.SetupRequirement(UnityEngine.Transform,Piece+Requirement,Player,System.Boolean,System.Int32,System.Int32)",
            "InventoryGui.SetupRequirementList(System.Int32,Player,System.Boolean,System.Int32)",
            "InventoryGui.UpdateContainer(Player)",
            "InventoryGui.UpdateRecipe(Player,System.Single)", // Edited by S-E004-012 (ADR-GWIN-136): the station-actions postfix.
            "InventoryGui.UpdateRecipeGamepadInput()",
            "ItemDrop+ItemData.GetTooltip(ItemDrop+ItemData,System.Int32,System.Boolean,System.Single,System.Int32,System.Boolean)",
            "Player.ConsumeResources(Piece+Requirement[],System.Int32,System.Int32,System.Int32)",
            "Player.HaveRequirementItems(Recipe,System.Boolean,System.Int32,System.Int32)",
            "Player.HaveRequirements(Piece,Player+RequirementMode)",
            "Player.PlacePiece(Piece,UnityEngine.Vector3,UnityEngine.Quaternion,System.Boolean,System.Boolean)",
            "Player.TryPlacePiece(Piece)",
            "Smelter.Awake()",
            "Switch.GetHoverText()",
            "Switch.Interact(Humanoid,System.Boolean,System.Boolean)",
            "Terminal.InitTerminal()", // Edited by S-E010-002 (organize-console-command 6.2): the console registration postfix.
            "ZNet.OnNewConnection(ZNetPeer)",
            "ZNetView.OnDestroy()",
        };

        Assert.Equal(
            new[]
            {
                // Edited by S-E003-006, ADR-GWIN-132 (GW-U-1655): the three building patch classes join the fifteen (eighteen classes).
                "BaseStorage.Plugin.BaseMenu.InventoryGridGamepadPatch",
                "BaseStorage.Plugin.BaseMenu.InventoryGuiHidePatch",
                "BaseStorage.Plugin.BaseMenu.InventoryGuiRecipeGamepadPatch",
                "BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch",
                "BaseStorage.Plugin.Building.PlayerPlacePiecePatch",
                "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch",
                // Edited by integration (S-E004-004): plus the Category button patch class.
                "BaseStorage.Plugin.CategoryButton.InventoryGuiUpdateContainerPatch",
                // Edited by S-E010-002 (organize-console-command 6.2): the console registration postfix class, in ordinal order.
                "BaseStorage.Plugin.Commands.TerminalInitTerminalPatch",
                "BaseStorage.Plugin.Crafting.InventoryGuiDoCraftingPatch",
                "BaseStorage.Plugin.Crafting.PlayerConsumeResourcesPatch",
                "BaseStorage.Plugin.Crafting.PlayerHaveRequirementItemsPatch",
                "BaseStorage.Plugin.Favourites.HumanoidIsItemEquipedPatch",
                "BaseStorage.Plugin.Favourites.InventoryGridOnLeftDownPatch",
                "BaseStorage.Plugin.Favourites.InventoryGridUpdateGuiPatch",
                "BaseStorage.Plugin.Favourites.InventoryStackAllPatch",
                "BaseStorage.Plugin.Feeding.CookingStationAwakePatch",
                "BaseStorage.Plugin.Feeding.CookingStationHoverTextPatch",
                "BaseStorage.Plugin.Feeding.CookingStationInteractPatch",
                "BaseStorage.Plugin.Feeding.FermenterAwakePatch",
                "BaseStorage.Plugin.Feeding.FermenterHoverTextPatch",
                "BaseStorage.Plugin.Feeding.FermenterInteractPatch",
                "BaseStorage.Plugin.Feeding.FireplaceAwakePatch",
                "BaseStorage.Plugin.Feeding.FireplaceHoverTextPatch",
                "BaseStorage.Plugin.Feeding.FireplaceInteractPatch",
                "BaseStorage.Plugin.Feeding.SmelterAwakePatch",
                "BaseStorage.Plugin.Feeding.SwitchGetHoverTextPatch",
                "BaseStorage.Plugin.Feeding.SwitchInteractPatch",
                "BaseStorage.Plugin.Hover.ContainerHoverTextPatch",
                "BaseStorage.Plugin.Hover.CraftingStationHoverTextPatch",
                "BaseStorage.Plugin.Networking.FejdStartupShowConnectErrorPatch",
                "BaseStorage.Plugin.Networking.ZNetOnNewConnectionPatch",
                "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch",
                "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch",
                "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementPatch",
                "BaseStorage.Plugin.Reservations.ContainerInteractPatch",
                "BaseStorage.Plugin.Reservations.ContainerOpenResponsePatch",
                "BaseStorage.Plugin.StationActions.InventoryGuiUpdateRecipePatch", // Edited by S-E004-012 (ADR-GWIN-136): the station-actions postfix.
                "BaseStorage.Plugin.Tooltips.ItemDataGetTooltipPatch",
                "BaseStorage.Plugin.World.ContainerAwakePatch",
                "BaseStorage.Plugin.World.CraftingStationOnDestroyPatch",
                "BaseStorage.Plugin.World.CraftingStationStartPatch",
                "BaseStorage.Plugin.World.ZNetViewOnDestroyPatch",
            },
            patchClasses.Select(patch => patch.ClassFullName).OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(expectedIds, patchClasses.SelectMany(patch => patch.TargetIds).OrderBy(id => id, StringComparer.Ordinal));
        Assert.Equal(expectedIds, harmonyPatched.Select(entry => entry.Value).OrderBy(id => id, StringComparer.Ordinal));
        Assert.Equal(string.Empty, AgreementChecker.Compare(patchClasses, harmonyPatched, plugin));
    }

    // GW-U-1150 (contract 8.2, AC-3; craft v3 text, edited by S-E003-006, ADR-GWIN-132): exactly one Plugin patch class targets
    // Player.HaveRequirements(, it is the building piece-overload patch, and no class targets the recipe overload or the first-required lookup.
    [RequiresPluginFact]
    public void OnlyThePiecePatchTargetsHaveRequirementsAndNoClassTargetsTheRecipeOverloadOrTheFirstRequiredLookup()
    {
        IReadOnlyList<PatchClass> patchClasses = HarmonyAttributeReader.Read(BuildValues.PluginAssemblyPath, null, new GameTypeResolver(fixture));
        string[] targets = patchClasses.SelectMany(patch => patch.TargetIds).ToArray();

        // The negative is meaningful only once the crafting patches exist, so the row also needs the DoCrafting target.
        Assert.Contains("InventoryGui.DoCrafting(Player)", targets);
        PatchClass[] holders = patchClasses.Where(patch => patch.TargetIds.Any(id => id.StartsWith("Player.HaveRequirements(", StringComparison.Ordinal))).ToArray();
        Assert.Single(holders);
        Assert.Equal("BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch", holders[0].ClassFullName);
        Assert.Equal(new[] { "Player.HaveRequirements(Piece,Player+RequirementMode)" }, holders[0].TargetIds.ToArray());
        Assert.DoesNotContain(targets, id => id.StartsWith("Player.HaveRequirements(Recipe", StringComparison.Ordinal) || id.StartsWith("Player.GetFirstRequiredItem(", StringComparison.Ordinal));
    }
}
