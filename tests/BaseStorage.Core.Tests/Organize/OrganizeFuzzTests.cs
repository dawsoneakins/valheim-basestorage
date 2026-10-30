namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using Xunit.Abstractions;
using Xunit.Sdk;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.RandomBaseGenerator;

/// <summary>
/// GW-U-950 to GW-U-954 (S-E004-007, AC-4.5 last paragraph, AC-4.6, principle 1): the randomized conservation test over 1000 random
/// bases. The corpus is generated from the fixed <see cref="RandomBaseGenerator.Seed"/>, which every shard logs together with its
/// totals; a failure names the seed, the base index and the smallest failing chest prefix.
/// </summary>
public sealed class OrganizeFuzzTests
{
    /// <summary>The shards of the corpus: each theory row checks <see cref="BasesPerShard"/> consecutive bases.</summary>
    internal const int Shards = 4;

    /// <summary>The bases per shard.</summary>
    internal const int BasesPerShard = CorpusSize / Shards;

    private readonly ITestOutputHelper output;

    /// <summary>Creates the tests with xunit's output sink for the seed line.</summary>
    /// <param name="output">The output sink.</param>
    public OrganizeFuzzTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    // GW-U-951, GW-U-952, GW-U-953
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomBasesConserveEveryItemRespectLimitsAndLeaveKeepAndSkippedChestsAlone(int shard)
    {
        Assert.Equal(CorpusSize, Shards * BasesPerShard);
        var stopwatch = Stopwatch.StartNew();
        OrganizeFuzzInvariants.Stats total = OrganizeFuzzInvariants.Stats.Zero;
        int chests = 0;
        int first = shard * BasesPerShard;
        for (int index = first; index < first + BasesPerShard; index++)
        {
            GeneratedBase generated = Generate(index);
            chests += generated.Input.Containers.Length;
            try
            {
                total = total.Add(OrganizeFuzzInvariants.Check(generated.Input));
            }
            catch (XunitException failure)
            {
                OrganizeFuzzShrinker.Shrunk? shrunk = OrganizeFuzzShrinker.Shrink(
                    Seed,
                    index,
                    generated.Input.Containers.Length,
                    input => OrganizeFuzzInvariants.Check(input));
                Assert.Fail(OrganizeFuzzShrinker.Report(Seed, generated, failure, shrunk));
            }
        }

        stopwatch.Stop();
        output.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "fuzz seed={0} shard={1} bases={2}..{3} chests={4} usable={5} skipped={6} (keep {7}, invalid {8}) moves={9} movedItems={10} noRoomItems={11} claimed={12} elapsed={13} ms",
            Seed,
            shard,
            first,
            first + BasesPerShard - 1,
            chests,
            total.Usable,
            total.Skipped,
            total.KeepSkipped,
            total.InvalidSkipped,
            total.Moves,
            total.MovedItems,
            total.NoRoomItems,
            total.Claimed,
            stopwatch.ElapsedMilliseconds));

        // Non-vacuous: the shard exercised moves, merges into claimed chests, no-room remainders, Keep skips and invalid-contents skips.
        Assert.True(total.Moves > 0, "the shard planned no moves");
        Assert.True(total.Claimed > 0, "the shard claimed no overflow chest");
        Assert.True(total.NoRoomItems > 0, "the shard had no no-room remainder");
        Assert.True(total.KeepSkipped > 0, "the shard had no Keep chest");
        Assert.True(total.InvalidSkipped > 0, "the shard had no invalid-contents chest");
    }

    // GW-U-950
    [Fact]
    public void TheSameSeedAndIndexGenerateTheSameBaseAndACapGivesItsChestPrefix()
    {
        foreach (int index in new[] { 0, 1, 49, 123, 500, 999 })
        {
            GeneratedBase first = Generate(index);
            GeneratedBase second = Generate(index);
            Assert.Equal(first.BaseSeed, second.BaseSeed);
            Assert.Equal(first.Input.Containers, second.Input.Containers);
            Assert.Equal(first.Input.Threshold, second.Input.Threshold);
            Assert.Equal(first.Input.Options, second.Input.Options);
            Assert.Equal(first.CatalogVariant, second.CatalogVariant);
            Assert.Equal(SortedAssignments(first.Input), SortedAssignments(second.Input));
            Assert.Equal(SortedInUse(first.Input), SortedInUse(second.Input));

            int count = first.Input.Containers.Length;
            Assert.Equal(first.FullChestCount, count);
            for (int cap = 1; cap < count; cap += Math.Max(1, count / 4))
            {
                GeneratedBase prefix = Generate(index, cap);
                Assert.Equal(cap, prefix.Input.Containers.Length);
                Assert.Equal(first.FullChestCount, prefix.FullChestCount);
                Assert.Equal(first.Input.Containers.Take(cap), prefix.Input.Containers);
                Assert.Equal(first.Input.Threshold, prefix.Input.Threshold);
                Assert.Equal(SortedAssignments(first.Input).Where(a => prefix.Input.Containers.Any(c => c.Id.Id == a.Id)), SortedAssignments(prefix.Input));
            }
        }

        Assert.NotEqual(Generate(3).Input.Containers, Generate(4).Input.Containers);
        Assert.NotEqual(Generate(Seed + 1, 3, MaxChests).Input.Containers, Generate(3).Input.Containers);
    }

    // GW-U-950
    [Fact]
    public void TheCorpusVariesChestCountsSizesAssignmentsFlagsContentsAndCatalogs()
    {
        var chestCounts = new HashSet<int>();
        var sizes = new HashSet<(int, int)>();
        var thresholds = new HashSet<int>();
        var catalogs = new HashSet<int>();
        var flags = new HashSet<ContainerFlags>();
        var categories = new HashSet<string>(StringComparer.Ordinal);
        var maxStacks = new HashSet<int>();
        int keep = 0, unassigned = 0, inUse = 0, empty = 0, full = 0, partialStacks = 0, fullStacks = 0, quality = 0, tooMany = 0, overMax = 0, excludeVehicles = 0, keepCraftable = 0, unmapped = 0;
        for (int index = 0; index < CorpusSize; index++)
        {
            PlanInput input = Generate(index).Input;
            chestCounts.Add(input.Containers.Length);
            thresholds.Add(input.Threshold);
            catalogs.Add(Generate(index).CatalogVariant);
            excludeVehicles += input.Options.ExcludeVehicles ? 1 : 0;
            keepCraftable += input.Options.KeepChestsCraftable ? 1 : 0;
            foreach (ContainerSnapshot chest in input.Containers)
            {
                sizes.Add((chest.Width, chest.Height));
                flags.Add(chest.Flags);
                inUse += input.InUseBy.ContainsKey(chest.Id) ? 1 : 0;
                if (input.Assignments.TryGetValue(chest.Id, out CategoryAssignment? assignment))
                {
                    keep += assignment!.IsKeep ? 1 : 0;
                    if (!assignment.IsKeep)
                    {
                        categories.Add(assignment.CategoryName!);
                    }
                }
                else
                {
                    unassigned++;
                }

                empty += chest.Contents.Count == 0 ? 1 : 0;
                full += chest.Contents.Count == chest.SlotCount ? 1 : 0;
                tooMany += chest.Contents.Count > chest.SlotCount ? 1 : 0;
                foreach (ItemStackSnapshot stack in chest.Contents)
                {
                    maxStacks.Add(stack.MaxStack);
                    quality += stack.Quality > 1 ? 1 : 0;
                    overMax += stack.Count > stack.MaxStack ? 1 : 0;
                    fullStacks += stack.Count == stack.MaxStack ? 1 : 0;
                    partialStacks += stack.Count < stack.MaxStack ? 1 : 0;
                    unmapped += input.Catalog.GetMappedCategory(stack.PrefabName) is null ? 1 : 0;
                }
            }
        }

        Assert.Contains(1, chestCounts);
        Assert.Contains(MaxChests, chestCounts);
        Assert.True(chestCounts.Count >= 40, "only " + chestCounts.Count + " distinct chest counts");
        Assert.Equal(Sizes.Length, sizes.Count);
        Assert.Equal(Thresholds.Length, thresholds.Count);
        Assert.Equal(CatalogTexts.Length, catalogs.Count);
        Assert.Equal(OddFlags.Length + 1, flags.Count);
        Assert.Superset(new HashSet<string>(ExtraAssignmentNames, StringComparer.Ordinal), categories);
        Assert.Contains("Wood", categories);
        Assert.Contains("Metals & Ores", categories);
        Assert.Superset(new HashSet<int>(MaxStacks), maxStacks);
        Assert.Contains(int.MaxValue, maxStacks);
        Assert.All(new[] { keep, unassigned, inUse, empty, full, partialStacks, fullStacks, quality, tooMany, overMax, excludeVehicles, keepCraftable, unmapped }, n => Assert.True(n > 0));
        Assert.True(excludeVehicles < CorpusSize && keepCraftable < CorpusSize, "an option never varied");
    }

    // GW-U-954
    [Fact]
    public void TheShrinkerFindsTheSmallestFailingChestPrefixAndReportsIt()
    {
        GeneratedBase large = Generate(LargeBaseEvery - 1);
        Assert.Equal(MaxChests, large.Input.Containers.Length);
        ContainerId third = large.Input.Containers[2].Id;

        static void FailsFromThreeChests(PlanInput input) =>
            Assert.True(input.Containers.Length < 3, "three or more chests");

        OrganizeFuzzShrinker.Shrunk? shrunk = OrganizeFuzzShrinker.Shrink(Seed, large.Index, large.Input.Containers.Length, FailsFromThreeChests);
        Assert.NotNull(shrunk);
        Assert.Equal(3, shrunk!.Base.Input.Containers.Length);
        Assert.Equal(third, shrunk.Base.Input.Containers[2].Id);
        Assert.Contains("three or more chests", shrunk.Failure.Message, StringComparison.Ordinal);

        XunitException failure = Assert.Throws<TrueException>(() => FailsFromThreeChests(large.Input));
        string report = OrganizeFuzzShrinker.Report(Seed, large, failure, shrunk);
        Assert.Contains("seed " + Seed.ToString(CultureInfo.InvariantCulture), report, StringComparison.Ordinal);
        Assert.Contains("base " + large.Index.ToString(CultureInfo.InvariantCulture), report, StringComparison.Ordinal);
        Assert.Contains("Smallest failing prefix: 3 chests", report, StringComparison.Ordinal);
        Assert.Contains("#" + third.Id.ToString(CultureInfo.InvariantCulture) + " ", report, StringComparison.Ordinal);

        Assert.Null(OrganizeFuzzShrinker.Shrink(Seed, large.Index, large.Input.Containers.Length, input => Assert.True(input.Containers.Length < MaxChests)));
        Assert.Contains("No smaller chest prefix fails", OrganizeFuzzShrinker.Report(Seed, large, failure, null), StringComparison.Ordinal);
    }

    private static (uint Id, string Value)[] SortedAssignments(PlanInput input) =>
        input.Assignments.Select(p => (p.Key.Id, p.Value.IsKeep ? "Keep" : p.Value.CategoryName!)).OrderBy(p => p.Item1).ToArray();

    private static (uint Id, string Name)[] SortedInUse(PlanInput input) =>
        input.InUseBy.Select(p => (p.Key.Id, p.Value)).OrderBy(p => p.Item1).ToArray();
}
