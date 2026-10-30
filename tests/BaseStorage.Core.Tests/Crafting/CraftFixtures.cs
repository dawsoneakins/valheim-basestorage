namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Settings;

/// <summary>
/// The contract 5.6 craft fixture of S-E003-004: Me, Erik, the inventory, chests c5 and c7, the Wood 15 and Resin 2 recipe,
/// the 3600 s clock and the 1500 ms reserve timeout. Not a test row (ADR-GWIN-124).
/// </summary>
internal static class CraftFixtures
{
    /// <summary>The local session of the contract examples.</summary>
    internal const long Me = 3141592653L;

    /// <summary>The friend session of the contract examples.</summary>
    internal const long Erik = 2718281828L;

    /// <summary>The player inventory id: new ContainerId(Me, 0).</summary>
    internal static readonly ContainerId Inventory = new ContainerId(Me, 0u);

    /// <summary>Chest c5, owned by the local player, at distance 5.</summary>
    internal static readonly ContainerId C5 = new ContainerId(Me, 5u);

    /// <summary>Chest c7, owned by Erik, at distance 10.</summary>
    internal static readonly ContainerId C7 = new ContainerId(Erik, 7u);

    /// <summary>The manual clock start: 3600 seconds.</summary>
    internal static readonly TimeSpan Start = TimeSpan.FromSeconds(3600);

    /// <summary>The reserve timeout of the fixture.</summary>
    internal const int ReserveTimeoutMs = 1500;

    /// <summary>A quality 1 Wood stack with max stack 50.</summary>
    /// <param name="count">The stack size.</param>
    /// <returns>The stack.</returns>
    internal static ItemStackSnapshot Wood(int count) => new ItemStackSnapshot("Wood", count, 1, 50);

    /// <summary>A quality 1 Resin stack with max stack 50.</summary>
    /// <param name="count">The stack size.</param>
    /// <returns>The stack.</returns>
    internal static ItemStackSnapshot Resin(int count) => new ItemStackSnapshot("Resin", count, 1, 50);

    /// <summary>The recipe of the fixture: Wood 15 and Resin 2, multiplier 1.</summary>
    /// <returns>The requirements.</returns>
    internal static CraftRequirement[] Requirements() => new[] { new CraftRequirement("Wood", 15), new CraftRequirement("Resin", 2) };

    /// <summary>Builds a base snapshot with every member eligible and no category assignment.</summary>
    /// <param name="inventory">The player inventory.</param>
    /// <param name="chests">The member chests.</param>
    /// <returns>The snapshot.</returns>
    internal static BaseSnapshot Snapshot(ContainerSnapshot inventory, IReadOnlyList<ContainerSnapshot> chests) =>
        new BaseSnapshot(inventory, chests, new Dictionary<ContainerId, CategoryAssignment>(), new EligibilityOptions(true, true));

    /// <summary>Plans the fixture recipe over the given views.</summary>
    /// <param name="inventory">The player inventory.</param>
    /// <param name="chests">The member chests.</param>
    /// <returns>The plan, or null when the materials are not there.</returns>
    internal static ConsumptionPlan? PlanFor(ContainerSnapshot inventory, params ContainerSnapshot[] chests) =>
        ConsumptionPlanner.Plan(Snapshot(inventory, chests), Requirements());

    /// <summary>Builds the settings of the craft fixture.</summary>
    /// <param name="timeoutMs">The reserve timeout.</param>
    /// <returns>The settings.</returns>
    internal static BaseStorageSettings Settings(int timeoutMs = ReserveTimeoutMs) =>
        new BaseStorageSettings(new FakeSettingsSource().WithInt(SettingKeys.ReserveTimeoutMs, timeoutMs));
}
