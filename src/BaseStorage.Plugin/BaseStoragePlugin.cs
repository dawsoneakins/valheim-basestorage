namespace BaseStorage.Plugin;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Diagnostics;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Patching;
using BaseStorage.Core.Settings;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Building;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.BaseMenu;
using BaseStorage.Plugin.Feeding;
using BaseStorage.Plugin.Boot;
using BaseStorage.Plugin.HostSync;
using BaseStorage.Plugin.Networking;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Requirements;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.Settings;
using BaseStorage.Plugin.StationActions;
using BaseStorage.Plugin.Tooltips;
using BaseStorage.Plugin.World;
using BepInEx;
using HarmonyLib;

/// <summary>The BepInEx entry point. Boots the one Harmony instance and applies every patch class.</summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class BaseStoragePlugin : BaseUnityPlugin
{
    /// <summary>The BepInEx plugin GUID; BepInEx also names the config file after it.</summary>
    public const string PluginGuid = "dawso.valheim.basestorage";

    /// <summary>The BepInEx plugin name; also the log source name.</summary>
    public const string PluginName = "BaseStorage";

    /// <summary>The plugin version, generated at build time from the Version property of Version.props.</summary>
    public const string PluginVersion = PluginBuildInfo.Version;

    private Harmony? harmony;
    private FirstRunCategoryFile? firstRunCategoryFile;

    /// <summary>The single settings read path, set once at boot. Plugin code reads settings only through it.</summary>
    internal static BaseStorageSettings? Settings { get; private set; }

    /// <summary>The one gated log sink, set once at boot. Plugin code writes Info and Debug lines only through LogPolicy over it.</summary>
    internal static ILogSink? LogSink { get; private set; }
    private void Update()
    {
        PluginReservations.Tick();
        PluginCrafting.Tick();
        PluginBuilding.Tick();
        ChestCategoryWriter.Tick();
        CategoryHotkey.Tick();
        OrganizeRunner.Tick();
        StationFeeder.Tick();
        if (firstRunCategoryFile != null && firstRunCategoryFile.Tick())
        {
            firstRunCategoryFile = null;
        }

        PluginVersionHandshake.Tick();
    }

    private void Awake()
    {
        try { Boot(); } catch (Exception ex) { Logger.LogError(BootMessages.LoadFailed(PluginVersion, ex)); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Boot()
    {
        var sink = new DebugGatedLogSink(new BepInExLogSink(Logger), () => Settings?.DebugLogging == true);
        LogSink = sink;
        PatchSafety.Initialize(new RateLimitedErrorReporter(sink));
        var settingsSource = new BepInExSettingsSource(Config);
        Settings = new BaseStorageSettings(PluginConfigSync.Initialize(settingsSource, sink));
        PluginWorld world = PluginWorld.Initialize(new GameClock());
        ReservationDriver<Container> reservations = PluginReservations.Initialize(new GameClock(), Settings, sink);
        ChestCategoryWriter.Initialize(reservations, new GameClock(), Settings);
        PluginCrafting.Initialize(reservations, new GameClock(), Settings, sink);
        PluginRequirementDisplay.Initialize();
        PluginBuilding.Initialize(reservations, new GameClock(), Settings, sink);
        CraftFromBaseGate.Initialize(static () => Settings != null && Settings.CraftFromBase, static () => PluginCrafting.InFlight || PluginBuilding.InFlight);
        PluginVersionHandshake.Initialize(new GameClock(), Settings, sink);
        CategoryHotkey.Initialize(settingsSource);
        PluginCategoryCatalog.Initialize(Paths.ConfigPath, sink);
        OrganizeRunner.Initialize(world, reservations, new GameClock(), Settings, sink);
        StationActionsPanel.Initialize(world, new GameClock(), Settings, sink);
        ItemCategoryLine.Initialize(Settings);
        BaseMenuPanel.Initialize(world, new GameClock(), Settings);
        ChestHighlight.Initialize(new GameClock());
        StationFeeder.Initialize(world, reservations, new GameClock(), Settings);
        FeedPanel.Initialize(new GameClock());
        Harmony instance = new Harmony(PluginGuid);
        harmony = instance;
        Type[] patchClasses = PatchDiscovery.FindPatchClasses(typeof(BaseStoragePlugin).Assembly);
        var units = new List<IPatchUnit>(patchClasses.Length);
        foreach (Type patchClass in patchClasses)
        {
            units.Add(new HarmonyPatchUnit(instance, patchClass));
        }

        PatchApplier.ApplyAll(units, sink);
        firstRunCategoryFile = new FirstRunCategoryFile(Paths.ConfigPath, sink);
        PluginConfigSync.UseCategoryFolder(firstRunCategoryFile.ConfigFolder);
        LogPolicy.Write(sink, LogCategory.Load, BootMessages.Loaded(PluginVersion));
    }
}
