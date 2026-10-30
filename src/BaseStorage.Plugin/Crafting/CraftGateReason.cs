namespace BaseStorage.Plugin.Crafting;

/// <summary>Why the craft-from-base gate is open or closed (contract craft-from-base 3.1 and 5.1).</summary>
internal enum CraftGateReason
{
    /// <summary>The gate is open: the craft uses the base.</summary>
    UsesBase = 0,

    /// <summary>The CraftFromBase setting is off.</summary>
    SettingOff = 1,

    /// <summary>The check is a discovery check.</summary>
    Discover = 2,

    /// <summary>The player is not the local player.</summary>
    NotLocalPlayer = 3,

    /// <summary>The no-cost cheat or world key applies.</summary>
    NoCost = 4,

    /// <summary>No registered crafting station is in use.</summary>
    NoStation = 5,

    /// <summary>The station is an upgrader.</summary>
    UpgraderStation = 6,

    /// <summary>The recipe needs only one ingredient.</summary>
    OnlyOneIngredient = 7,

    /// <summary>The station is in no base.</summary>
    NotInBase = 8,

    /// <summary>The requirements or the inventory could not be read.</summary>
    Unreadable = 9,
}
