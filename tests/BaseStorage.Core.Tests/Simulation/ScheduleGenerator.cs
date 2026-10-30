namespace BaseStorage.Core.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Organize;

/// <summary>
/// The deterministic schedule generator of the S-E005-004 simulation (AC-5.5). Schedule <c>index</c> is drawn from
/// <c>Mix(Seed, index)</c> alone: the run parameters first (peers, operations per peer, moves per frame, latency, denial and event
/// rates), then the base chests, the inventories of every peer and the stations. The caps keep the drawn world unchanged and only
/// limit how many peers and operations the runner uses, so a shrunk failing schedule plays in the same world. The interleaving
/// itself is drawn at run time from a second stream of the same seed (<see cref="RunSeed"/>). Not a test row.
/// </summary>
internal static class ScheduleGenerator
{
    /// <summary>The fixed seed of the corpus. Change it only on purpose; every shard logs it.</summary>
    internal const int Seed = 20261028;

    /// <summary>The number of schedules in the corpus, split into shards by the test.</summary>
    internal const int CorpusSize = 500;

    /// <summary>The largest number of peers.</summary>
    internal const int MaxPeers = 3;

    /// <summary>The largest number of chests in a base.</summary>
    internal const int MaxChests = 10;

    /// <summary>The termination guard of GW-U-1468 (ADR-GWIN-114 decision 7): every schedule must end within this many frames, and a schedule still running at the cap fails as a livelock.</summary>
    internal const int MaxTicksPerSchedule = 2000;

    /// <summary>The steam-id part shared by every chest id.</summary>
    internal const long ChestUserId = 3141592653L;

    /// <summary>The grid sizes a chest can have.</summary>
    internal static readonly (int Width, int Height)[] Sizes = { (2, 1), (4, 1), (4, 2), (3, 3), (8, 2), (8, 3), (8, 4) };

    /// <summary>One generated schedule: the parameters and the world.</summary>
    /// <param name="Index">The schedule index.</param>
    /// <param name="Seed">The 64-bit seed the schedule was drawn from.</param>
    /// <param name="Peers">The peers the runner uses (after the cap).</param>
    /// <param name="OpsPerPeer">The operations each peer runs (after the cap).</param>
    /// <param name="FullPeers">The peers drawn before any cap.</param>
    /// <param name="FullOps">The operations per peer drawn before any cap.</param>
    /// <param name="MovesPerFrame">The MovesPerFrame setting.</param>
    /// <param name="MaxLatencyTicks">The largest grant latency; above 30 ticks a request times out.</param>
    /// <param name="DenyPct">The owner's access-denial probability in percent.</param>
    /// <param name="ReassignDelayTicks">The ticks the server takes to reassign a disconnected holder's chests.</param>
    /// <param name="UiOpenPerMille">Per-tick probability, in per mille, that an idle peer opens a chest in the vanilla UI.</param>
    /// <param name="OwnerLostPerMille">Per-tick probability that the server moves a chest's ownership away.</param>
    /// <param name="OwnerAwayPerMille">Per-tick probability that a ghost-owned chest's owner becomes unreachable.</param>
    /// <param name="DisconnectPerMille">Per-tick probability that a busy peer disconnects.</param>
    /// <param name="StartPerMille">Per-tick probability that an idle peer starts its next operation.</param>
    /// <param name="Chests">The base chests, in generation order.</param>
    /// <param name="Assignments">The persisted category assignments (Keep or a category).</param>
    /// <param name="Inventories">One inventory per peer, for the first <paramref name="Peers"/> peers.</param>
    /// <param name="Stations">The base's stations.</param>
    /// <param name="WriteDuringLatencyPerMille">Per-tick probability, in per mille, that the idle owner of a chest with a request in flight moves one item out of it and releases it (ADR-GWIN-114 decision 10).</param>
    /// <param name="RenewEveryTicks">The frames between two lock renewals of a running Organize or Deposit; the base rate of 100 frames or a faster one.</param>
    internal sealed record SimulationConfig(
        int Index,
        ulong Seed,
        int Peers,
        int OpsPerPeer,
        int FullPeers,
        int FullOps,
        int MovesPerFrame,
        int MaxLatencyTicks,
        int DenyPct,
        int ReassignDelayTicks,
        int UiOpenPerMille,
        int OwnerLostPerMille,
        int OwnerAwayPerMille,
        int DisconnectPerMille,
        int StartPerMille,
        ContainerSnapshot[] Chests,
        Dictionary<ContainerId, CategoryAssignment> Assignments,
        ContainerSnapshot[] Inventories,
        StationSnapshot[] Stations,
        int WriteDuringLatencyPerMille,
        int RenewEveryTicks)
    {
        /// <summary>The seed of the run-time stream: the interleaving, recipes, favourites, latencies and events.</summary>
        internal ulong RunSeed => Seed ^ 0x5DEECE66D1234567UL;
    }

    /// <summary>Generates schedule <paramref name="index"/> of the corpus.</summary>
    /// <param name="index">The schedule index.</param>
    /// <param name="peerCap">The largest peer count to use.</param>
    /// <param name="opsCap">The largest operations-per-peer count to use.</param>
    internal static SimulationConfig Generate(int index, int peerCap = MaxPeers, int opsCap = int.MaxValue) => Generate(Seed, index, peerCap, opsCap);

    /// <summary>Generates schedule <paramref name="index"/> of the corpus seeded by <paramref name="seed"/>.</summary>
    /// <param name="seed">The corpus seed.</param>
    /// <param name="index">The schedule index.</param>
    /// <param name="peerCap">The largest peer count to use; at least 1.</param>
    /// <param name="opsCap">The largest operations-per-peer count to use; at least 1.</param>
    internal static SimulationConfig Generate(int seed, int index, int peerCap, int opsCap)
    {
        if (peerCap < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(peerCap));
        }

        if (opsCap < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opsCap));
        }

        ulong mixed = RandomBaseGenerator.Mix(seed, index);
        var random = new FuzzRandom(mixed);

        int fullPeers = random.Chance(55) ? 3 : 2;
        int fullOps = random.Next(2, 6);
        int movesPerFrame = random.Next(1, 5);
        int maxLatency = random.Next(4, 46);
        int denyPct = random.Next(0, 16);
        int reassign = random.Next(20, 61);
        int uiOpen = random.Next(0, 25);
        int ownerLost = random.Next(0, 10);
        int ownerAway = random.Next(0, 10);
        int disconnect = random.Chance(40) ? random.Next(1, 4) : 0;
        int start = random.Next(60, 500);
        int chestCount = random.Next(4, MaxChests + 1);
        int stationCount = random.Next(1, 3);

        CategoryCatalog catalog = SimulationItems.Catalog();
        var usedIds = new HashSet<uint>();
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        var chests = new ContainerSnapshot[chestCount];
        for (int c = 0; c < chestCount; c++)
        {
            chests[c] = GenerateChest(random, catalog, usedIds, assignments);
        }

        var inventories = new ContainerSnapshot[fullPeers];
        for (int peer = 0; peer < fullPeers; peer++)
        {
            inventories[peer] = GenerateInventory(random, peer);
        }

        var stations = new StationSnapshot[stationCount];
        for (int s = 0; s < stationCount; s++)
        {
            stations[s] = new StationSnapshot(
                new StationId(2718281828L, 900u + (uint)random.Next(0, 50) + (uint)(s * 100)),
                new WorldPosition(random.Next(-10, 11), 30f, random.Next(-10, 11)),
                20f);
        }

        int writeDuringLatency = random.Chance(60) ? random.Next(40, 400) : 0;
        int renewEvery = random.Chance(50) ? random.Next(12, 60) : SimulationRunner.RenewEveryTicks;

        int peers = Math.Min(fullPeers, peerCap);
        var usedInventories = new ContainerSnapshot[peers];
        Array.Copy(inventories, usedInventories, peers);
        return new SimulationConfig(
            index,
            mixed,
            peers,
            Math.Min(fullOps, opsCap),
            fullPeers,
            fullOps,
            movesPerFrame,
            maxLatency,
            denyPct,
            reassign,
            uiOpen,
            ownerLost,
            ownerAway,
            disconnect,
            start,
            chests,
            assignments,
            usedInventories,
            stations,
            writeDuringLatency,
            renewEvery);
    }

    /// <summary>Describes a schedule compactly: the parameters, then one line per chest and per inventory.</summary>
    /// <param name="config">The schedule.</param>
    internal static string Describe(SimulationConfig config)
    {
        var text = new StringBuilder();
        text.Append("schedule ").Append(config.Index.ToString(CultureInfo.InvariantCulture))
            .Append(" seed=0x").Append(config.Seed.ToString("X16", CultureInfo.InvariantCulture))
            .Append(" peers=").Append(config.Peers.ToString(CultureInfo.InvariantCulture)).Append('/').Append(config.FullPeers.ToString(CultureInfo.InvariantCulture))
            .Append(" ops=").Append(config.OpsPerPeer.ToString(CultureInfo.InvariantCulture)).Append('/').Append(config.FullOps.ToString(CultureInfo.InvariantCulture))
            .Append(" movesPerFrame=").Append(config.MovesPerFrame.ToString(CultureInfo.InvariantCulture))
            .Append(" latency<=").Append(config.MaxLatencyTicks.ToString(CultureInfo.InvariantCulture))
            .Append(" deny%=").Append(config.DenyPct.ToString(CultureInfo.InvariantCulture))
            .Append(" reassign=").Append(config.ReassignDelayTicks.ToString(CultureInfo.InvariantCulture))
            .Append(" uiOpen=").Append(config.UiOpenPerMille.ToString(CultureInfo.InvariantCulture))
            .Append(" ownerLost=").Append(config.OwnerLostPerMille.ToString(CultureInfo.InvariantCulture))
            .Append(" ownerAway=").Append(config.OwnerAwayPerMille.ToString(CultureInfo.InvariantCulture))
            .Append(" disconnect=").Append(config.DisconnectPerMille.ToString(CultureInfo.InvariantCulture))
            .Append(" start=").Append(config.StartPerMille.ToString(CultureInfo.InvariantCulture))
            .Append(" stations=").Append(config.Stations.Length.ToString(CultureInfo.InvariantCulture))
            .Append(" renewEvery=").Append(config.RenewEveryTicks.ToString(CultureInfo.InvariantCulture))
            .Append(" writeDuringLatency=").Append(config.WriteDuringLatencyPerMille.ToString(CultureInfo.InvariantCulture))
            .Append('\n');
        foreach (ContainerSnapshot chest in config.Chests)
        {
            text.Append("  #").Append(chest.Id.Id.ToString(CultureInfo.InvariantCulture))
                .Append(' ').Append(chest.Width.ToString(CultureInfo.InvariantCulture))
                .Append('x').Append(chest.Height.ToString(CultureInfo.InvariantCulture))
                .Append(" @(").Append(chest.Position.X.ToString("F1", CultureInfo.InvariantCulture))
                .Append(',').Append(chest.Position.Z.ToString("F1", CultureInfo.InvariantCulture))
                .Append(") ").Append(chest.Flags);
            if (config.Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment))
            {
                text.Append(assignment!.IsKeep ? " Keep" : " =" + assignment.CategoryName);
            }

            text.Append(" [").Append(PlanAssert.Join(chest.Contents)).Append("]\n");
        }

        for (int peer = 0; peer < config.Inventories.Length; peer++)
        {
            ContainerSnapshot inventory = config.Inventories[peer];
            text.Append("  ").Append(SimulationItems.PeerNames[peer])
                .Append(" @(").Append(inventory.Position.X.ToString("F1", CultureInfo.InvariantCulture))
                .Append(',').Append(inventory.Position.Z.ToString("F1", CultureInfo.InvariantCulture))
                .Append(") [").Append(PlanAssert.Join(inventory.Contents)).Append("]\n");
        }

        return text.ToString();
    }

    private static ContainerSnapshot GenerateChest(FuzzRandom random, CategoryCatalog catalog, HashSet<uint> usedIds, Dictionary<ContainerId, CategoryAssignment> assignments)
    {
        uint rawId;
        do
        {
            rawId = (uint)random.Next(100, 1000);
        }
        while (!usedIds.Add(rawId));

        var id = new ContainerId(ChestUserId, rawId);
        var position = new WorldPosition(random.Next(-20, 21) * 0.5f, 30f, random.Next(-20, 21) * 0.5f);
        (int width, int height) = random.Pick(Sizes);
        int slots = width * height;

        int flagRoll = random.Next(0, 100);
        ContainerFlags flags = flagRoll < 90 ? ContainerFlags.PlayerBuilt
            : flagRoll < 95 ? ContainerFlags.Tombstone
            : ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle;

        string? assignedCategory = null;
        int assignmentRoll = random.Next(0, 100);
        if (assignmentRoll < 15)
        {
            assignments[id] = CategoryAssignment.Keep;
        }
        else if (assignmentRoll < 55)
        {
            assignedCategory = random.Pick(catalog.CategoryNames);
            assignments[id] = CategoryAssignment.ForCategory(assignedCategory);
        }

        return new ContainerSnapshot(id, position, width, height, GenerateContents(random, catalog, slots, assignedCategory), flags);
    }

    private static ItemStackSnapshot[] GenerateContents(FuzzRandom random, CategoryCatalog catalog, int slots, string? assignedCategory)
    {
        int mode = random.Next(0, 100);
        if (mode < 20)
        {
            return Array.Empty<ItemStackSnapshot>();
        }

        int stackCount = mode < 75 ? random.Next(1, slots + 1) : slots;
        IReadOnlyList<string> own = OwnPrefabs(catalog, assignedCategory);
        int ownPercent = own.Count == 0 ? 0 : random.Next(30, 90);
        var stacks = new ItemStackSnapshot[stackCount];
        for (int i = 0; i < stackCount; i++)
        {
            SimulationItems.ItemSpec item = own.Count > 0 && random.Chance(ownPercent)
                ? SimulationItems.SpecOf(random.Pick(own))
                : random.Pick(SimulationItems.Items);
            stacks[i] = GenerateStack(random, item);
        }

        return stacks;
    }

    private static IReadOnlyList<string> OwnPrefabs(CategoryCatalog catalog, string? categoryName)
    {
        if (categoryName is null)
        {
            return Array.Empty<string>();
        }

        int index = catalog.IndexOf(categoryName);
        if (index < 0)
        {
            return Array.Empty<string>();
        }

        var known = new List<string>();
        foreach (string prefab in catalog.Categories[index].Prefabs)
        {
            foreach (SimulationItems.ItemSpec item in SimulationItems.Items)
            {
                if (string.Equals(item.Prefab, prefab, StringComparison.Ordinal))
                {
                    known.Add(prefab);
                }
            }
        }

        return known;
    }

    private static ItemStackSnapshot GenerateStack(FuzzRandom random, SimulationItems.ItemSpec item)
    {
        int quality = item.IsGear ? random.Next(1, 4) : 1;
        int count = random.Chance(40) ? item.MaxStack : random.Next(1, item.MaxStack + 1);
        return new ItemStackSnapshot(item.Prefab, count, quality, item.MaxStack);
    }

    private static ContainerSnapshot GenerateInventory(FuzzRandom random, int peer)
    {
        int stackCount = random.Next(0, 15);
        var stacks = new ItemStackSnapshot[stackCount];
        for (int i = 0; i < stackCount; i++)
        {
            SimulationItems.ItemSpec item = random.Chance(70)
                ? SimulationItems.SpecOf(random.Pick(SimulationItems.CraftablePrefabs))
                : random.Pick(SimulationItems.Items);
            stacks[i] = GenerateStack(random, item);
        }

        return new ContainerSnapshot(
            new ContainerId(ReservationAuthority.SessionOf(peer), 1u),
            new WorldPosition(random.Next(-20, 21) * 0.5f, 30f, random.Next(-20, 21) * 0.5f),
            8,
            4,
            stacks,
            ContainerFlags.None);
    }
}
