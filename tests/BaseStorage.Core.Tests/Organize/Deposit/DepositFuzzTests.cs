namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Organize.Deposit;
using Xunit.Abstractions;
using Xunit.Sdk;

/// <summary>
/// AC-4.9, principle 1: 600 random Deposits (inventories, favourites and never lists over the S-E004-007 base corpus) conserve every
/// item exactly, never move an excluded item, and complete through the executor over the fake layer with the same guarantees
/// (deposit contract 6.9). A failure names the seed and the case, and prints the case.
/// </summary>
public sealed class DepositFuzzTests
{
    private const int Shards = 3;
    private const int CasesPerShard = 200;

    private readonly ITestOutputHelper output;

    public DepositFuzzTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    // GW-U-1066 (AC-4.9): the generator is deterministic, pairs each case with its base, and the corpus is varied.
    [Fact]
    public void TheGeneratorIsDeterministicAndVaried()
    {
        foreach (int index in new[] { 0, 1, 49, 123, 500, 599 })
        {
            RandomInventoryGenerator.GeneratedDeposit first = RandomInventoryGenerator.Generate(index);
            RandomInventoryGenerator.GeneratedDeposit second = RandomInventoryGenerator.Generate(index);
            Assert.Equal(first.Player.Container, second.Player.Container);
            Assert.Equal(first.Player.Stacks.Select(s => (s.Slot, s.Traits)), second.Player.Stacks.Select(s => (s.Slot, s.Traits)));
            Assert.Equal(first.Exclusions.Favourites, second.Exclusions.Favourites);
            Assert.Equal(first.Exclusions.NeverItems, second.Exclusions.NeverItems);
            Assert.Equal(first.Input.Containers, RandomBaseGenerator.Generate(index).Input.Containers);
            Assert.NotEqual(first.Player.Container, RandomInventoryGenerator.Generate(RandomInventoryGenerator.Seed + 1, index).Player.Container);
            Assert.All(first.Input.Containers, chest => Assert.NotEqual(first.Player.Id, chest.Id));
        }

        Assert.Equal(Shards * CasesPerShard, RandomInventoryGenerator.CorpusSize);
        var heights = new HashSet<int>();
        var variants = new HashSet<int>();
        var traits = InventoryItemTraits.None;
        int empty = 0, full = 0, favourites = 0, emptyFavourites = 0, overMax = 0, reversed = 0, neverPrefabs = 0;
        for (int index = 0; index < RandomInventoryGenerator.CorpusSize; index++)
        {
            RandomInventoryGenerator.GeneratedDeposit generated = RandomInventoryGenerator.Generate(index);
            heights.Add(generated.Player.Height);
            variants.Add(generated.NeverListVariant);
            int slots = generated.Player.Width * generated.Player.Height;
            empty += generated.Player.Stacks.Count == 0 ? 1 : 0;
            full += generated.Player.Stacks.Count == slots ? 1 : 0;
            favourites += generated.Exclusions.Favourites.Count;
            emptyFavourites += generated.Exclusions.Favourites.Slots.Count(slot => generated.Player.Stacks.All(stack => stack.Slot != slot));
            overMax += generated.Player.Stacks.Count(stack => stack.Stack.Count > stack.Stack.MaxStack);
            neverPrefabs += generated.Player.Stacks.Count(stack => RandomInventoryGenerator.NeverItems.Any(item => item.Prefab == stack.Stack.PrefabName));
            if (generated.Player.Stacks.Count > 1 && generated.Player.Stacks[0].Slot.CompareTo(generated.Player.Stacks[1].Slot) > 0)
            {
                reversed++;
            }

            foreach (PlayerInventoryStack stack in generated.Player.Stacks)
            {
                traits |= stack.Traits;
            }
        }

        Assert.Equal(new[] { 2, 3, 4, 5 }, heights.OrderBy(h => h));
        Assert.Equal(RandomInventoryGenerator.NeverListVariants, variants.Count);
        Assert.Equal(InventoryItemTraits.Equipped | InventoryItemTraits.Mead | InventoryItemTraits.Ammo | InventoryItemTraits.Bait, traits);
        Assert.True(empty > 0 && full > 0, "the corpus must have empty and full inventories");
        Assert.True(favourites > 0 && emptyFavourites > 0, "the corpus must have favourites, including favourites of empty slots");
        Assert.True(overMax > 0 && reversed > 0 && neverPrefabs > 0, "the corpus must have over-max stacks, reversed stack order and never-list items");
    }

    // GW-U-1067, GW-U-1068, GW-U-1069 (AC-4.9, AC-4.5, AC-4.6, principle 1): every case of the corpus holds every invariant.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EveryRandomDepositConservesAndNeverMovesAnExcludedItem(int shard)
    {
        Assert.True(shard >= 0 && shard < Shards);
        var watch = Stopwatch.StartNew();
        DepositFuzzInvariants.Stats total = DepositFuzzInvariants.Stats.Zero;
        int first = shard * CasesPerShard;
        for (int index = first; index < first + CasesPerShard; index++)
        {
            RandomInventoryGenerator.GeneratedDeposit generated = RandomInventoryGenerator.Generate(index);
            try
            {
                total = total.Add(DepositFuzzInvariants.Check(generated));
            }
            catch (XunitException failure)
            {
                string header = string.Format(
                    CultureInfo.InvariantCulture,
                    "Deposit fuzz seed {0}, case {1} ({2} stacks) failed: {3}\n",
                    RandomInventoryGenerator.Seed,
                    index,
                    generated.Player.Stacks.Count,
                    failure.Message);
                throw new XunitException(header + RandomInventoryGenerator.Describe(generated));
            }
        }

        watch.Stop();
        output.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "deposit fuzz seed={0} shard={1} cases={2} stacks={3} excluded={4} reasons={5} moves={6} movedItems={7} noRoomItems={8} claimed={9} executed={10} elapsed={11} ms",
            RandomInventoryGenerator.Seed,
            shard,
            CasesPerShard,
            total.Stacks,
            total.Excluded,
            total.Reasons,
            total.Moves,
            total.MovedItems,
            total.NoRoomItems,
            total.Claimed,
            total.Executed,
            watch.ElapsedMilliseconds));

        DepositExclusion all = DepositExclusion.Hotbar | DepositExclusion.Equipped | DepositExclusion.Favourite | DepositExclusion.NeverList
            | DepositExclusion.SameItemAsExcluded | DepositExclusion.InvalidStack;
        Assert.Equal(all, total.Reasons);
        Assert.True(total.Moves > 0, "no move was planned in the shard");
        Assert.True(total.NoRoomItems > 0, "no no-room remainder occurred in the shard");
        Assert.True(total.Claimed > 0, "no overflow chest was claimed in the shard");
        Assert.True(total.Executed >= CasesPerShard / 4, "only " + total.Executed + " cases ran through the executor");
    }
}
