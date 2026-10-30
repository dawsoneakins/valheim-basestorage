namespace BaseStorage.Core.Tests.PatchGuard;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using BaseStorage.Core.Tests.Repo;

/// <summary>
/// AC-8 and AC-9 (ADR-GWIN-030, ADR-GWIN-031): the built Plugin dll is read as metadata and never loaded. The World namespace
/// holds the 11 contract types, the snapshot adapter implements IBaseSnapshotSource, PluginWorld has no public member, no World
/// type derives from MonoBehaviour, and the patch guard finds exactly the four World patch bodies and no violation.
/// </summary>
[Trait("Category", "Repo")]
[Collection("MSBuildProcess")]
public sealed class PluginWorldPatchGuardTests
{
    private const string WorldNamespace = "BaseStorage.Plugin.World";
    private const string Helper = "BaseStorage.Plugin.Patching.PatchSafety";

    // GW-U-515
    [Fact]
    public void BuiltWorldSurfaceMatchesTheContractOnTheBuiltDll()
    {
        string path = BuiltPlugin();
        using FileStream stream = File.OpenRead(path);
        using PEReader pe = new(stream);
        MetadataReader metadata = pe.GetMetadataReader();

        string[] topLevel = WorldTypes(metadata).Select(t => metadata.GetString(t.Name)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            new[]
            {
                "ContainerAwakePatch",
                "CraftingStationOnDestroyPatch",
                "CraftingStationStartPatch",
                "GameClock",
                "GameSnapshotSource",
                "LoadedRegistry`2",
                "PluginWorld",
                "RegistryKind",
                "RegistryMessages",
                "SnapshotMemo`2",
                "ZNetViewOnDestroyPatch",
            },
            topLevel);

        TypeDefinition snapshot = WorldTypes(metadata).Single(t => metadata.GetString(t.Name) == "GameSnapshotSource");
        Assert.Contains(
            snapshot.GetInterfaceImplementations(),
            handle =>
            {
                InterfaceImplementation implementation = metadata.GetInterfaceImplementation(handle);
                return implementation.Interface.Kind == HandleKind.TypeReference
                    && metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)implementation.Interface).Name) == "IBaseSnapshotSource";
            });
        int constructorParameters = snapshot.GetMethods()
            .Select(metadata.GetMethodDefinition)
            .Where(m => metadata.GetString(m.Name) == ".ctor")
            .Select(m => m.GetParameters().Select(metadata.GetParameter).Count(p => p.SequenceNumber > 0))
            .Single();
        Assert.Equal(2, constructorParameters);

        TypeDefinition world = WorldTypes(metadata).Single(t => metadata.GetString(t.Name) == "PluginWorld");
        Assert.DoesNotContain(
            world.GetMethods().Select(metadata.GetMethodDefinition),
            m => (m.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public);
        Assert.DoesNotContain(
            world.GetFields().Select(metadata.GetFieldDefinition),
            f => (f.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public);

        Assert.DoesNotContain(
            WorldTypes(metadata),
            t => !t.BaseType.IsNil
                && t.BaseType.Kind == HandleKind.TypeReference
                && metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)t.BaseType).Name) == "MonoBehaviour");
    }

    // Edited by S-E003-004 (ADR-GWIN-124): the three Crafting prefixes join the twelve. GW-U-525, edited by GW-U-563 (S-E002-005), by S-E004-014 (ADR-GWIN-055) and by S-E005-002 (ADR-GWIN-059): the two Favourites
    // bodies, the two Hover postfixes and the two Reservations prefixes join the four World bodies.
    [Fact]
    public void BuiltPluginPatchBodiesAreTheFourWorldTwoHoverTwoFavouritesTwoReservationsAndOneCategoryButtonPatchesWithNoViolation()
    {
        // Edited by S-E003-008, craft-from-base-toggle 8.3 amendment (ADR-GWIN-136)
        PatchGuardResult result = PatchGuardScanner.Scan(BuiltPlugin(), new PatchGuardRules("HarmonyLib", Helper, GateMethodFullName: "BaseStorage.Plugin.Patching.CraftFromBaseGate.IsOpen"));

        string[] expected =
        {
            "BaseStorage.Plugin.Building.PlayerHaveRequirementsPiecePatch.Prefix",
            "BaseStorage.Plugin.Building.PlayerPlacePiecePatch.Prefix",
            "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Postfix",
            "BaseStorage.Plugin.Building.PlayerTryPlacePiecePatch.Prefix",
            "BaseStorage.Plugin.CategoryButton.InventoryGuiUpdateContainerPatch.Postfix",
            // Edited by S-E010-002 (organize-console-command 6.2): the console registration postfix.
            "BaseStorage.Plugin.Commands.TerminalInitTerminalPatch.Postfix",
            "BaseStorage.Plugin.Crafting.InventoryGuiDoCraftingPatch.Prefix",
            "BaseStorage.Plugin.Crafting.PlayerConsumeResourcesPatch.Prefix",
            "BaseStorage.Plugin.Crafting.PlayerHaveRequirementItemsPatch.Prefix",
            "BaseStorage.Plugin.Favourites.InventoryGridOnLeftDownPatch.Prefix",
            "BaseStorage.Plugin.Favourites.InventoryGridUpdateGuiPatch.Postfix",
            "BaseStorage.Plugin.Hover.ContainerHoverTextPatch.Postfix",
            "BaseStorage.Plugin.Hover.CraftingStationHoverTextPatch.Postfix",
            "BaseStorage.Plugin.Networking.FejdStartupShowConnectErrorPatch.Postfix",
            "BaseStorage.Plugin.Networking.ZNetOnNewConnectionPatch.Postfix",
            // Edited by S-E003-005, ADR-GWIN-127: plus the five Requirements display bodies (twenty in all).
            "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Postfix",
            "BaseStorage.Plugin.Requirements.HudSetupPieceInfoPatch.Prefix",
            "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Postfix",
            "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementListPatch.Prefix",
            "BaseStorage.Plugin.Requirements.InventoryGuiSetupRequirementPatch.Postfix",
            "BaseStorage.Plugin.Reservations.ContainerInteractPatch.Prefix",
            "BaseStorage.Plugin.Reservations.ContainerOpenResponsePatch.Prefix",
            // Edited by S-E004-012 (ADR-GWIN-136): plus the StationActions UpdateRecipe postfix.
            "BaseStorage.Plugin.StationActions.InventoryGuiUpdateRecipePatch.Postfix",
            "BaseStorage.Plugin.Tooltips.ItemDataGetTooltipPatch.Postfix",
            "BaseStorage.Plugin.World.ContainerAwakePatch.Postfix",
            "BaseStorage.Plugin.World.CraftingStationOnDestroyPatch.Prefix",
            "BaseStorage.Plugin.World.CraftingStationStartPatch.Postfix",
            "BaseStorage.Plugin.World.ZNetViewOnDestroyPatch.Prefix",
        };
        Assert.Equal(expected, result.PatchBodies.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.True(result.Violations.Count == 0, PatchGuardScanner.FormatViolations(result));
    }

    private static string BuiltPlugin()
    {
        AssemblyConfigurationAttribute? configuration = typeof(PluginWorldPatchGuardTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        Assert.NotNull(configuration);
        return PluginDll.EnsureBuilt(configuration!.Configuration ?? string.Empty);
    }

    private static TypeDefinition[] WorldTypes(MetadataReader metadata) =>
        metadata.TypeDefinitions
            .Select(metadata.GetTypeDefinition)
            .Where(t => t.GetDeclaringType().IsNil && metadata.GetString(t.Namespace) == WorldNamespace)
            .ToArray();
}
