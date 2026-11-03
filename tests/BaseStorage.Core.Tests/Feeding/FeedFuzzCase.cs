namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>
/// One case of the S-QF-001 randomized test (contract section 8, ADR-GWIN-180). Case n is drawn from <c>new System.Random(Seed + n)</c> and has the
/// shape <c>n % 4</c>. The case is a mutable model: an order is applied to it, and the station state is rebuilt from the same factory. The skip
/// list and the assignments stay fixed for the whole case.
/// </summary>
internal sealed class FeedFuzzCase
{
    /// <summary>The fixed seed of the randomized test (contract 8).</summary>
    internal const int Seed = 20261103;

    /// <summary>The item pool of contract 8.</summary>
    internal static readonly string[] Pool =
    {
        "Wood", "Coal", "CopperOre", "TinOre", "MeadBaseTasty", "MeadBaseHealthMinor", "RawMeat", "NeckTail", "FishRaw", "Resin",
    };

    private FeedFuzzCase(int n, FeedStationShape shape)
    {
        N = n;
        Shape = shape;
        Inputs = Array.Empty<string>();
        Selection = FeedSelection.Empty;
        Chests = new List<FeedFuzzChest>();
        Assignments = new Dictionary<ContainerId, CategoryAssignment>();
        Skip = new List<ContainerId>();
    }

    /// <summary>Gets the case number n.</summary>
    internal int N { get; }

    /// <summary>Gets the station shape.</summary>
    internal FeedStationShape Shape { get; }

    /// <summary>The queue fill (smelter class).</summary>
    internal int Queued;

    /// <summary>The queue capacity (smelter class).</summary>
    internal int MaxQueued;

    /// <summary>The fuel inside (smelter class and fuel only).</summary>
    internal float Fuel;

    /// <summary>The fuel capacity (smelter class and fuel only).</summary>
    internal float MaxFuel;

    /// <summary>The fuel prefab, or null for a station that burns nothing.</summary>
    internal string? FuelPrefab;

    /// <summary>The accepted inputs (empty for fuel only).</summary>
    internal string[] Inputs;

    /// <summary>True when a single-input station already holds an input.</summary>
    internal bool Occupied;

    /// <summary>The slot count (several input).</summary>
    internal int SlotCount;

    /// <summary>The occupied slot count (several input).</summary>
    internal int OccupiedSlots;

    /// <summary>The station position.</summary>
    internal WorldPosition Position;

    /// <summary>The selection, in the order chosen.</summary>
    internal FeedSelection Selection;

    /// <summary>The chests, in input order.</summary>
    internal List<FeedFuzzChest> Chests;

    /// <summary>The persisted assignment per chest id. Fixed for the case.</summary>
    internal Dictionary<ContainerId, CategoryAssignment> Assignments;

    /// <summary>The eligibility options. Fixed for the case.</summary>
    internal EligibilityOptions Options;

    /// <summary>The chests to leave alone. Fixed for the case.</summary>
    internal List<ContainerId> Skip;

    /// <summary>Draws the case n of contract 8.</summary>
    /// <param name="n">The case number.</param>
    /// <returns>The case.</returns>
    internal static FeedFuzzCase Generate(int n)
    {
        var rng = new System.Random(Seed + n);
        var shape = (FeedStationShape)(n % 4);
        var model = new FeedFuzzCase(n, shape);

        switch (shape)
        {
            case FeedStationShape.SmelterClass:
                model.MaxQueued = Pick(rng, 0, 1, 10);
                model.Queued = rng.Next(0, model.MaxQueued + 2);
                model.MaxFuel = rng.Next(0, 2) == 0 ? 0f : 20f;
                model.Fuel = rng.Next(0, (int)((model.MaxFuel + 2f) * 4f) + 1) / 4f;
                model.FuelPrefab = rng.Next(0, 5) == 0 ? null : "Coal";
                model.Inputs = FeedFixtures.Ores;
                break;
            case FeedStationShape.FuelOnly:
                model.MaxFuel = rng.Next(0, 49) / 4f;
                model.Fuel = rng.Next(0, (int)((model.MaxFuel + 2f) * 4f) + 1) / 4f;
                model.FuelPrefab = rng.Next(0, 10) == 0 ? null : "Wood";
                break;
            case FeedStationShape.SingleInput:
                model.Occupied = rng.Next(0, 2) == 0;
                model.Inputs = FeedFixtures.Mead;
                break;
            default:
                model.SlotCount = rng.Next(0, 7);
                model.OccupiedSlots = rng.Next(0, model.SlotCount + 2);
                model.Inputs = FeedFixtures.Food;
                break;
        }

        model.Position = RandomPosition(rng);

        string[] own = model.OwnItems();
        int picks = rng.Next(0, 4);
        for (int i = 0; i < picks; i++)
        {
            string prefab = Pool[rng.Next(0, Pool.Length)];
            int keep = rng.Next(0, 4) == 0 ? rng.Next(0, 41) : 0;
            model.Selection = model.Selection.With(prefab, keep);
        }

        if (own.Length > 0)
        {
            string extra = own[rng.Next(0, own.Length)];
            if (model.Selection.Find(extra) is null)
            {
                model.Selection = model.Selection.With(extra, 0);
            }
        }

        int chestCount = rng.Next(0, 9);
        for (int i = 0; i < chestCount; i++)
        {
            uint number = (uint)(i + 1);
            ContainerFlags flags = rng.Next(0, 10) < 9 ? ContainerFlags.PlayerBuilt : ContainerFlags.None;
            if (rng.Next(0, 8) == 0)
            {
                flags |= ContainerFlags.Vehicle;
            }

            if (rng.Next(0, 8) == 0)
            {
                flags |= ContainerFlags.Tombstone;
            }

            if (rng.Next(0, 8) == 0)
            {
                flags |= ContainerFlags.AccessDenied;
            }

            if (rng.Next(0, 8) == 0)
            {
                flags |= ContainerFlags.InUseByOther;
            }

            var chest = new FeedFuzzChest(number, RandomPosition(rng), flags);
            int stacks = rng.Next(0, 5);
            for (int s = 0; s < stacks; s++)
            {
                string prefab = own.Length > 0 && rng.Next(0, 2) == 0 ? own[rng.Next(0, own.Length)] : Pool[rng.Next(0, Pool.Length)];
                chest.Stacks.Add((prefab, rng.Next(1, 51)));
            }

            model.Chests.Add(chest);

            if (rng.Next(0, 4) == 0)
            {
                model.Assignments[Id(number)] = CategoryAssignment.Keep;
            }
            else if (rng.Next(0, 8) == 0)
            {
                model.Assignments[Id(number)] = CategoryAssignment.ForCategory("Ores");
            }
        }

        model.Options = new EligibilityOptions(rng.Next(0, 2) == 0, rng.Next(0, 2) == 0);

        foreach (FeedFuzzChest chest in model.Chests)
        {
            if (rng.Next(0, 6) == 0)
            {
                model.Skip.Add(Id(chest.Number));
            }
        }

        return model;
    }

    /// <summary>Builds the station state from the model, with the factory of its shape.</summary>
    /// <returns>The state.</returns>
    internal FeedStationState Station() => Shape switch
    {
        FeedStationShape.SmelterClass => FeedStationState.SmelterClass(Queued, MaxQueued, Fuel, MaxFuel, FuelPrefab, Inputs),
        FeedStationShape.FuelOnly => FeedStationState.FuelOnly(Fuel, MaxFuel, FuelPrefab),
        FeedStationShape.SingleInput => FeedStationState.SingleInput(Occupied, Inputs),
        _ => FeedStationState.SeveralInput(SlotCount, OccupiedSlots, Inputs),
    };

    /// <summary>Builds the chest snapshots from the model.</summary>
    /// <returns>The snapshots, in input order.</returns>
    internal ContainerSnapshot[] Snapshots() => Chests.Select(chest => chest.Snapshot()).ToArray();

    /// <summary>Applies an order to the model: removes the stock from the chest and adds the items to the station.</summary>
    /// <param name="order">The order.</param>
    /// <returns>False when the order names a chest the model does not hold.</returns>
    internal bool Apply(FeedOrder order)
    {
        FeedFuzzChest? chest = Chests.FirstOrDefault(candidate => Id(candidate.Number).Equals(order.Chest));
        if (chest is null)
        {
            return false;
        }

        int remaining = order.Amount;
        for (int i = 0; i < chest.Stacks.Count && remaining > 0; i++)
        {
            if (chest.Stacks[i].Prefab != order.PrefabName)
            {
                continue;
            }

            int take = Math.Min(remaining, chest.Stacks[i].Count);
            chest.Stacks[i] = (chest.Stacks[i].Prefab, chest.Stacks[i].Count - take);
            remaining -= take;
        }

        chest.Stacks.RemoveAll(stack => stack.Count == 0);

        switch (Shape)
        {
            case FeedStationShape.SmelterClass:
                if (order.IsFuel)
                {
                    Fuel += order.Amount;
                }
                else
                {
                    Queued += order.Amount;
                }

                break;
            case FeedStationShape.FuelOnly:
                Fuel += order.Amount;
                break;
            case FeedStationShape.SingleInput:
                Occupied = true;
                break;
            default:
                OccupiedSlots += order.Amount;
                break;
        }

        return true;
    }

    /// <summary>Gets the total of one item over every chest.</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>The total.</returns>
    internal int ChestTotal(string prefab) => Chests.Sum(chest => chest.CountOf(prefab));

    /// <summary>Describes the whole case for a failure message, so that it can be replayed from Seed and n.</summary>
    /// <returns>The text.</returns>
    internal string Dump()
    {
        var text = new StringBuilder();
        text.Append("shape=").Append(Shape)
            .Append(" queued=").Append(Queued).Append('/').Append(MaxQueued)
            .Append(" fuel=").Append(Fuel).Append('/').Append(MaxFuel)
            .Append(" fuelPrefab=").Append(FuelPrefab ?? "null")
            .Append(" occupied=").Append(Occupied)
            .Append(" slots=").Append(OccupiedSlots).Append('/').Append(SlotCount)
            .Append(" inputs=[").Append(string.Join(",", Inputs)).Append(']')
            .Append(" selection=[").Append(string.Join(",", Selection.Entries.Select(entry => entry.PrefabName + "=" + entry.Keep))).Append(']')
            .Append(" options=(excludeVehicles ").Append(Options.ExcludeVehicles).Append(", keepChestsCraftable ").Append(Options.KeepChestsCraftable).Append(')')
            .Append(" skip=[").Append(string.Join(",", Skip.Select(id => id.ToString()))).Append(']')
            .Append(" station=").Append(Position.X).Append(',').Append(Position.Y).Append(',').Append(Position.Z);
        foreach (FeedFuzzChest chest in Chests)
        {
            text.Append("\n  chest ").Append(chest.Number)
                .Append(" at ").Append(chest.Position.X).Append(',').Append(chest.Position.Z)
                .Append(" flags=").Append((int)chest.Flags)
                .Append(" assignment=").Append(AssignmentText(chest.Number))
                .Append(" stacks=[").Append(string.Join(",", chest.Stacks.Select(stack => stack.Prefab + "x" + stack.Count))).Append(']');
        }

        return text.ToString();
    }

    private static int Pick(System.Random rng, params int[] values) => values[rng.Next(0, values.Length)];

    private static WorldPosition RandomPosition(System.Random rng) =>
        new WorldPosition(rng.Next(-20, 21), rng.Next(25, 36), rng.Next(-20, 21));

    private string AssignmentText(uint number)
    {
        if (!Assignments.TryGetValue(Id(number), out CategoryAssignment? assignment) || assignment is null)
        {
            return "none";
        }

        return assignment.IsKeep ? "Keep" : assignment.CategoryName ?? "none";
    }

    private string[] OwnItems()
    {
        var items = new List<string>(Inputs);
        if (FuelPrefab is not null)
        {
            items.Add(FuelPrefab);
        }

        return items.ToArray();
    }
}

/// <summary>One chest of the model. It has four slots, and its stacks are (prefab, count) pairs in slot order.</summary>
internal sealed class FeedFuzzChest
{
    /// <summary>Creates a chest.</summary>
    /// <param name="number">The chest's number; its id is <c>Id(number)</c>.</param>
    /// <param name="position">Where the chest is.</param>
    /// <param name="flags">The chest's flags.</param>
    internal FeedFuzzChest(uint number, WorldPosition position, ContainerFlags flags)
    {
        Number = number;
        Position = position;
        Flags = flags;
        Stacks = new List<(string Prefab, int Count)>();
    }

    /// <summary>Gets the chest's number.</summary>
    internal uint Number { get; }

    /// <summary>Gets where the chest is.</summary>
    internal WorldPosition Position { get; }

    /// <summary>Gets the chest's flags.</summary>
    internal ContainerFlags Flags { get; }

    /// <summary>Gets the stacks, in slot order.</summary>
    internal List<(string Prefab, int Count)> Stacks { get; }

    /// <summary>Builds the snapshot of the chest as it is now.</summary>
    /// <returns>The snapshot.</returns>
    internal ContainerSnapshot Snapshot() =>
        new ContainerSnapshot(Id(Number), Position, 4, 1, Stacks.Select(stack => S(stack.Prefab, stack.Count)).ToArray(), Flags);

    /// <summary>Counts one item in the chest.</summary>
    /// <param name="prefab">The prefab name.</param>
    /// <returns>The count.</returns>
    internal int CountOf(string prefab) => Stacks.Where(stack => stack.Prefab == prefab).Sum(stack => stack.Count);
}
