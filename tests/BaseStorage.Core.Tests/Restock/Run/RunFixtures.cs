namespace BaseStorage.Core.Tests.Restock.Run;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;
using BaseStorage.Core.Tests.Organize.Running;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;

/// <summary>
/// The contract 10.1 fixture of the Core Restock run tests: station ws, player P at (5, 30, 1), chests 801, 802 and 803, and the carried
/// stacks K. Built on <see cref="RunHarness"/>. Not a test row.
/// </summary>
internal static class RunFixtures
{
    /// <summary>The player's position, inside the station's range (P in 10.1).</summary>
    public static readonly WorldPosition Player = new WorldPosition(5f, 30f, 1f);

    /// <summary>A position far from every station (Outside in 10.1).</summary>
    public static readonly WorldPosition Outside = new WorldPosition(500f, 30f, 0f);

    /// <summary>Chest 801: at (5, 30, 6), 4 x 1, Wood30 and Resin20.</summary>
    /// <returns>The chest.</returns>
    public static ContainerSnapshot Chest801() => Chest(801, 5f, 6f, S("Wood", 30), S("Resin", 20));

    /// <summary>Chest 802: at (15, 30, 1), 4 x 1, Wood50.</summary>
    /// <returns>The chest.</returns>
    public static ContainerSnapshot Chest802() => Chest(802, 15f, 1f, S("Wood", 50));

    /// <summary>Chest 803: at (6, 30, 1), 4 x 1, Wood50 (Keep in the fixture).</summary>
    /// <returns>The chest.</returns>
    public static ContainerSnapshot Chest803() => Chest(803, 6f, 1f, S("Wood", 50));

    /// <summary>One player-built chest of the fixture base, 4 x 1, at (x, 30, z).</summary>
    /// <param name="number">The chest number.</param>
    /// <param name="x">The x position.</param>
    /// <param name="z">The z position.</param>
    /// <param name="stacks">The stacks.</param>
    /// <returns>The chest.</returns>
    public static ContainerSnapshot Chest(uint number, float x, float z, params ItemStackSnapshot[] stacks) =>
        new ContainerSnapshot(Id(number), new WorldPosition(x, 30f, z), 4, 1, stacks, ContainerFlags.PlayerBuilt);

    /// <summary>The three chests of 10.1.</summary>
    /// <returns>The chests.</returns>
    public static ContainerSnapshot[] Chests() => new[] { Chest801(), Chest802(), Chest803() };

    /// <summary>The carried stacks K of 10.1: Wood10 (hotbar), Resin5 (hotbar), Stone10 (not marked).</summary>
    /// <returns>A new list.</returns>
    public static List<CarriedStack> Carried() => new List<CarriedStack> { H(S("Wood", 10)), H(S("Resin", 5)), U(S("Stone", 10)) };

    /// <summary>The Keep assignment of chest 803.</summary>
    /// <returns>The assignments.</returns>
    public static Dictionary<ContainerId, CategoryAssignment> Keep803() =>
        new Dictionary<ContainerId, CategoryAssignment> { { Id(803), CategoryAssignment.Keep } };

    /// <summary>Builds a coordinator harness over the fixture base with the carried stacks K.</summary>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable value.</param>
    /// <param name="chests">The base's chests; the 10.1 chests by default.</param>
    /// <param name="keep803">True when chest 803 is Keep (the 10.1 assignment).</param>
    /// <param name="inUse">The in-use names of chests (facts.InUseBy).</param>
    /// <param name="lockSeconds">The OrganizeLockSeconds value.</param>
    /// <returns>A new harness.</returns>
    public static RunHarness Harness(bool keepChestsCraftable = false, ContainerSnapshot[]? chests = null, bool keep803 = true, Dictionary<ContainerId, string>? inUse = null, int lockSeconds = 30)
    {
        var assignments = keep803 ? Keep803() : new Dictionary<ContainerId, CategoryAssignment>();
        OrganizeFixtures.PlanInput input = OrganizeFixtures.Make(chests ?? Chests(), assignments, inUse);
        RunHarness harness = RunHarness.Make(input, lockSeconds: lockSeconds);
        harness.Port.Carried = Carried();
        harness.SettingsSource.WithBool(SettingKeys.KeepChestsCraftable, keepChestsCraftable);
        return harness;
    }

    /// <summary>Plans the restock of the given stacks over the given chests, the way the coordinator does (RR4 settings).</summary>
    /// <param name="carried">The carried stacks.</param>
    /// <param name="chests">The chests.</param>
    /// <param name="keepChestsCraftable">The KeepChestsCraftable value.</param>
    /// <returns>The plan.</returns>
    public static RestockPlan Plan(IReadOnlyList<CarriedStack> carried, IReadOnlyList<ContainerSnapshot> chests, bool keepChestsCraftable = false) =>
        RestockPlanner.Plan(carried, Player, chests, Keep803(), keepChestsCraftable ? Craftable() : NotCraftable());

    /// <summary>Ticks the coordinator until it is idle, with a bound on the number of ticks.</summary>
    /// <param name="harness">The harness.</param>
    /// <param name="maxTicks">The bound.</param>
    public static void RunToEnd(RunHarness harness, int maxTicks = 500)
    {
        int ticks = 0;
        while (harness.Coordinator.State != OrganizeRunState.Idle)
        {
            Assert.True(ticks < maxTicks, "The restock did not end within " + maxTicks + " ticks.");
            harness.Coordinator.Tick();
            ticks++;
        }
    }

    /// <summary>The texts of the summary notices shown, in order.</summary>
    /// <param name="harness">The harness.</param>
    /// <returns>The texts.</returns>
    public static List<string> Summaries(RunHarness harness) =>
        harness.Port.Notices.Where(n => n.Key == "message.restock.summary").Select(n => (string)n.Args[0]!).ToList();

    /// <summary>The texts of the Info log lines, in order.</summary>
    /// <param name="harness">The harness.</param>
    /// <returns>The texts.</returns>
    public static List<string> InfoLines(RunHarness harness) =>
        harness.Sink.Entries.Where(e => e.Severity == LogSeverity.Info).Select(e => e.Message).ToList();

    /// <summary>A plain stack of one prefab with Wl2 identity, for the identity rows.</summary>
    /// <param name="count">The count.</param>
    /// <returns>The stack.</returns>
    public static ItemStackSnapshot WoodWl2(int count) => SI("Wood", count, BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures.Wl2());
}
