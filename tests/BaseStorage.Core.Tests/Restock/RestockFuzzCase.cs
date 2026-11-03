namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using System.Text;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;
using static BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures;

/// <summary>One generated restock input (S-QR-001 contract 7.10), with the identity instances it uses so that a dump can name them.</summary>
internal sealed class FuzzCase
{
    /// <summary>Creates a case.</summary>
    /// <param name="seed">The seed that generated the case.</param>
    /// <param name="carried">The carried entries.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="containers">The chests in snapshot order.</param>
    /// <param name="assignments">The assignments.</param>
    /// <param name="options">The options.</param>
    /// <param name="wl2">The world-level-2 identity instance used by the case.</param>
    /// <param name="worn">The worn identity instance used by the case.</param>
    internal FuzzCase(
        int seed,
        List<CarriedStack> carried,
        WorldPosition origin,
        List<ContainerSnapshot> containers,
        Dictionary<ContainerId, CategoryAssignment> assignments,
        EligibilityOptions options,
        ItemStackIdentity wl2,
        ItemStackIdentity worn)
    {
        Seed = seed;
        Carried = carried;
        Origin = origin;
        Containers = containers;
        Assignments = assignments;
        Options = options;
        Wl2 = wl2;
        Worn = worn;
    }

    /// <summary>Gets the seed.</summary>
    internal int Seed { get; }

    /// <summary>Gets the carried entries.</summary>
    internal List<CarriedStack> Carried { get; }

    /// <summary>Gets the origin.</summary>
    internal WorldPosition Origin { get; }

    /// <summary>Gets the chests in snapshot order.</summary>
    internal List<ContainerSnapshot> Containers { get; }

    /// <summary>Gets the assignments.</summary>
    internal Dictionary<ContainerId, CategoryAssignment> Assignments { get; }

    /// <summary>Gets the options.</summary>
    internal EligibilityOptions Options { get; }

    /// <summary>Gets the world-level-2 identity instance.</summary>
    internal ItemStackIdentity Wl2 { get; }

    /// <summary>Gets the worn identity instance.</summary>
    internal ItemStackIdentity Worn { get; }

    /// <summary>Names an identity in a dump: E for Empty, wl2, worn, or ? for any other instance.</summary>
    /// <param name="identity">The identity.</param>
    /// <returns>The short name.</returns>
    internal string Label(ItemStackIdentity identity)
    {
        if (ReferenceEquals(identity, ItemStackIdentity.Empty))
        {
            return "E";
        }

        if (ReferenceEquals(identity, Wl2))
        {
            return "wl2";
        }

        return ReferenceEquals(identity, Worn) ? "worn" : "?";
    }

    /// <summary>Builds a readable dump of the inputs, for a failure message.</summary>
    /// <returns>The dump.</returns>
    internal string Dump()
    {
        var text = new StringBuilder();
        text.Append("origin=(").Append(Origin.X).Append(',').Append(Origin.Y).Append(',').Append(Origin.Z).Append(") ");
        text.Append("excludeVehicles=").Append(Options.ExcludeVehicles).Append(" keepChestsCraftable=").Append(Options.KeepChestsCraftable).Append('\n');
        for (int t = 0; t < Carried.Count; t++)
        {
            ItemStackSnapshot s = Carried[t].Stack;
            text.Append("  carried[").Append(t).Append("] ").Append(s.PrefabName).Append(' ').Append(s.Count).Append('/').Append(s.MaxStack)
                .Append(" q").Append(s.Quality).Append(" id=").Append(Label(s.Identity)).Append(" marks=").Append((int)Carried[t].Marks).Append('\n');
        }

        for (int c = 0; c < Containers.Count; c++)
        {
            ContainerSnapshot chest = Containers[c];
            text.Append("  chest[").Append(c).Append("] id=").Append(chest.Id.Id)
                .Append(" pos=(").Append(chest.Position.X).Append(',').Append(chest.Position.Y).Append(',').Append(chest.Position.Z).Append(')')
                .Append(" slots=").Append(chest.SlotCount).Append(" flags=").Append((int)chest.Flags);
            if (Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment) && assignment.IsKeep)
            {
                text.Append(" Keep");
            }

            text.Append(" contents:");
            for (int s = 0; s < chest.Contents.Count; s++)
            {
                ItemStackSnapshot item = chest.Contents[s];
                text.Append(" #").Append(s).Append(' ').Append(item.PrefabName).Append(' ').Append(item.Count).Append('/').Append(item.MaxStack)
                    .Append(" q").Append(item.Quality).Append(" id=").Append(Label(item.Identity));
            }

            text.Append('\n');
        }

        return text.ToString();
    }
}

/// <summary>The seeded generator of contract 7.10: the minimum shape, drawn from a System.Random that the test owns.</summary>
internal static class RestockFuzzGenerator
{
    private static readonly string[] Prefabs = { "Wood", "Resin", "Coal", "ArrowWood" };
    private static readonly int[] MaxChoices = { 1, 20, 50, 100 };

    /// <summary>
    /// Generates one case from a seed. Chest flags use a mixture: about half of the chests are a plain player-built chest,
    /// because the uniform draw alone makes a chest a usable source only about 7.5% of the time, so almost no case reached
    /// the planner's fill. The other half keep the uniform draw over the five flag bits of contract 7.10, so every flag
    /// combination stays reachable (a wider generator, never a narrower one).
    /// </summary>
    /// <param name="seed">The seed, which the caller derives as Seed + n.</param>
    /// <returns>The case.</returns>
    // Edited by S-QR-002 (ADR-GWIN-176): additive GenerateRun and RunFuzzCase; Generate is unchanged.
    /// <summary>
    /// One run case (S-QR-002 section 10.11): the S-QR-001 case of <paramref name="seed"/> plus seeded add modes, reservation answers and stale
    /// player stacks. Drawn from a second stream, so <see cref="Generate"/> is unchanged.
    /// </summary>
    /// <param name="seed">The seed.</param>
    /// <returns>The run case.</returns>
    internal static RunFuzzCase GenerateRun(int seed)
    {
        FuzzCase basic = Generate(seed);
        // Edited by S-QR-002 (ADR-GWIN-176): contract 10.11, no carried stack exceeds MaxStack. Generate can draw an over-full carried
        // stack (one in twenty), which is not a state a player's stack can start in, so the run case clamps it to MaxStack. No draw is added.
        for (int c = 0; c < basic.Carried.Count; c++)
        {
            ItemStackSnapshot held = basic.Carried[c].Stack;
            if (held.Count > held.MaxStack)
            {
                basic.Carried[c] = new CarriedStack(new ItemStackSnapshot(held.PrefabName, held.MaxStack, held.Quality, held.MaxStack, held.Identity), basic.Carried[c].Marks);
            }
        }

        var rnd = new System.Random(unchecked((seed * 7919) + 13));
        var modes = new System.Collections.Generic.Dictionary<int, BaseStorage.Core.Tests.Restock.Run.RestockAddMode>();
        var partials = new System.Collections.Generic.Dictionary<int, int>();
        for (int i = 0; i < basic.Carried.Count; i++)
        {
            int draw = rnd.Next(4);
            modes[i] = draw == 0 ? BaseStorage.Core.Tests.Restock.Run.RestockAddMode.Refuse : draw == 1 ? BaseStorage.Core.Tests.Restock.Run.RestockAddMode.PartialOnce : BaseStorage.Core.Tests.Restock.Run.RestockAddMode.Normal;
            partials[i] = rnd.Next(1, 20);
        }

        var reservations = new System.Collections.Generic.Dictionary<uint, BaseStorage.Core.Abstractions.ReservationStatus[]>();
        foreach (ContainerSnapshot chest in basic.Containers)
        {
            int draw = rnd.Next(8);
            reservations[chest.Id.Id] = draw == 0
                ? new[] { BaseStorage.Core.Abstractions.ReservationStatus.Denied }
                : draw == 1
                    ? new[] { BaseStorage.Core.Abstractions.ReservationStatus.Pending, BaseStorage.Core.Abstractions.ReservationStatus.Granted }
                    : new[] { BaseStorage.Core.Abstractions.ReservationStatus.Granted };
        }

        var stale = new System.Collections.Generic.List<(int Index, ItemStackSnapshot Stack)>();
        for (int i = 0; i < basic.Carried.Count; i++)
        {
            if (rnd.Next(5) == 0)
            {
                // Edited by S-QR-002 (ADR-GWIN-176): contract 10.8 allows an identity change. A stack with room gets one more item;
                // a full stack (Count == MaxStack) changes identity instead, so the stale stack never exceeds MaxStack.
                ItemStackSnapshot s = basic.Carried[i].Stack;
                stale.Add((i, s.Count < s.MaxStack
                    ? new ItemStackSnapshot(s.PrefabName, s.Count + 1, s.Quality, s.MaxStack, s.Identity)
                    : new ItemStackSnapshot(s.PrefabName, s.Count, s.Quality, s.MaxStack,
                        s.Identity.Equals(BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures.Wl2())
                            ? BaseStorage.Core.Snapshots.Identities.ItemStackIdentity.Empty
                            : BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures.Wl2())));
            }
        }

        return new RunFuzzCase(seed, basic, modes, partials, reservations, stale);
    }

    /// <summary>The run case built by <see cref="GenerateRun"/>: the basic case and the seeded layer script.</summary>
    internal sealed class RunFuzzCase
    {
        /// <summary>Creates the run case.</summary>
        /// <param name="seed">The seed.</param>
        /// <param name="basic">The S-QR-001 case.</param>
        /// <param name="modes">The add mode of each carried index.</param>
        /// <param name="partials">The PartialOnce amount of each carried index.</param>
        /// <param name="reservations">The reservation answers of each chest.</param>
        /// <param name="stale">The stale replacements applied before the run.</param>
        internal RunFuzzCase(
            int seed,
            FuzzCase basic,
            System.Collections.Generic.Dictionary<int, BaseStorage.Core.Tests.Restock.Run.RestockAddMode> modes,
            System.Collections.Generic.Dictionary<int, int> partials,
            System.Collections.Generic.Dictionary<uint, BaseStorage.Core.Abstractions.ReservationStatus[]> reservations,
            System.Collections.Generic.List<(int Index, ItemStackSnapshot Stack)> stale)
        {
            Seed = seed;
            Basic = basic;
            Modes = modes;
            Partials = partials;
            Reservations = reservations;
            Stale = stale;
        }

        /// <summary>Gets the seed.</summary>
        internal int Seed { get; }

        /// <summary>Gets the S-QR-001 case.</summary>
        internal FuzzCase Basic { get; }

        /// <summary>Gets the add mode of each carried index.</summary>
        internal System.Collections.Generic.Dictionary<int, BaseStorage.Core.Tests.Restock.Run.RestockAddMode> Modes { get; }

        /// <summary>Gets the PartialOnce amount of each carried index.</summary>
        internal System.Collections.Generic.Dictionary<int, int> Partials { get; }

        /// <summary>Gets the reservation answers of each chest.</summary>
        internal System.Collections.Generic.Dictionary<uint, BaseStorage.Core.Abstractions.ReservationStatus[]> Reservations { get; }

        /// <summary>Gets the stale replacements applied before the run.</summary>
        internal System.Collections.Generic.List<(int Index, ItemStackSnapshot Stack)> Stale { get; }
    }

    internal static FuzzCase Generate(int seed)
    {
        var rnd = new System.Random(seed);
        ItemStackIdentity wl2 = Wl2();
        ItemStackIdentity worn = Worn();
        ItemStackIdentity[] identities = { ItemStackIdentity.Empty, wl2, worn };

        var perPrefabMax = new int[Prefabs.Length];
        for (int p = 0; p < Prefabs.Length; p++)
        {
            perPrefabMax[p] = MaxChoices[rnd.Next(MaxChoices.Length)];
        }

        var carried = new List<CarriedStack>();
        int carriedCount = rnd.Next(13);
        for (int i = 0; i < carriedCount; i++)
        {
            ItemStackSnapshot stack = RandomStack(rnd, perPrefabMax, identities, overFull: rnd.Next(20) == 0);
            carried.Add(new CarriedStack(stack, (CarriedMarks)rnd.Next(8)));
        }

        var containers = new List<ContainerSnapshot>();
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        int chestCount = rnd.Next(9);
        for (int i = 0; i < chestCount; i++)
        {
            uint id = (uint)(1000 + i);
            var containerId = new ContainerId(3141592653L, id);
            var position = new WorldPosition(rnd.Next(-20, 21), rnd.Next(25, 36), rnd.Next(-20, 21));
            int slots = rnd.Next(1, 5);
            bool bad = rnd.Next(20) == 0;

            var contents = new List<ItemStackSnapshot>();
            if (bad && rnd.Next(2) == 0)
            {
                int extra = slots + 1;
                for (int j = 0; j < extra; j++)
                {
                    contents.Add(ChestStack(rnd, carried, perPrefabMax, identities));
                }
            }
            else
            {
                int stacks = rnd.Next(slots + 1);
                for (int j = 0; j < stacks; j++)
                {
                    contents.Add(ChestStack(rnd, carried, perPrefabMax, identities));
                }

                if (bad)
                {
                    contents.Add(RandomStack(rnd, perPrefabMax, identities, overFull: true));
                }
            }

            // Half plain player-built chests (see Generate); the rest keep the uniform 5-bit draw of 7.10.
            int flagsValue = rnd.Next(2) == 0
                ? (int)ContainerFlags.PlayerBuilt
                : ((rnd.Next(16) << 1) | (rnd.Next(5) != 0 ? (int)ContainerFlags.PlayerBuilt : 0));
            var flags = (ContainerFlags)flagsValue;
            containers.Add(new ContainerSnapshot(containerId, position, slots, 1, contents, flags));

            if (rnd.Next(4) == 0)
            {
                assignments[containerId] = CategoryAssignment.Keep;
            }
        }

        var options = new EligibilityOptions(rnd.Next(2) == 0, rnd.Next(2) == 0);
        var origin = new WorldPosition(rnd.Next(-20, 21), rnd.Next(25, 36), rnd.Next(-20, 21));
        return new FuzzCase(seed, carried, origin, containers, assignments, options, wl2, worn);
    }

    /// <summary>
    /// A chest stack. Half of them reuse the key of a random carried entry, so that same-key sources occur often enough to
    /// exercise the fill (a wider draw than the 7.10 minimum, which it does not narrow). The other half are drawn as carried stacks are.
    /// </summary>
    private static ItemStackSnapshot ChestStack(System.Random rnd, List<CarriedStack> carried, int[] perPrefabMax, ItemStackIdentity[] identities)
    {
        if (carried.Count == 0 || rnd.Next(2) == 0)
        {
            return RandomStack(rnd, perPrefabMax, identities, overFull: rnd.Next(20) == 0);
        }

        ItemStackSnapshot key = carried[rnd.Next(carried.Count)].Stack;
        bool overFull = rnd.Next(20) == 0;
        int count = overFull ? key.MaxStack + 1 : 1 + rnd.Next(key.MaxStack);
        return new ItemStackSnapshot(key.PrefabName, count, key.Quality, key.MaxStack, key.Identity);
    }

    private static ItemStackSnapshot RandomStack(System.Random rnd, int[] perPrefabMax, ItemStackIdentity[] identities, bool overFull)
    {
        int p = rnd.Next(Prefabs.Length);
        int max = rnd.Next(10) == 0 ? MaxChoices[rnd.Next(MaxChoices.Length)] : perPrefabMax[p];
        int quality = rnd.Next(2) + 1;
        ItemStackIdentity identity = identities[rnd.Next(identities.Length)];
        int count = overFull ? max + 1 : 1 + rnd.Next(max);
        return new ItemStackSnapshot(Prefabs[p], count, quality, max, identity);
    }
}
