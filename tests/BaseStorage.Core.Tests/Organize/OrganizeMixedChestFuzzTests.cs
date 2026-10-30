namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.RandomBaseGenerator;

/// <summary>
/// S-A-001 (AC-3, AC-6, principle 1; mixed-chest-category-contracts.md 6.8 fuzz requirement): the Organize invariants hold with the
/// mixed-chest step on, over the fixed corpus of <see cref="RandomBaseGenerator"/>, and the step only gives a category to a source chest.
/// </summary>
public sealed class OrganizeMixedChestFuzzTests
{
    private const int ShardCount = OrganizeFuzzTests.Shards;

    private const int BasesPerShard = OrganizeFuzzTests.BasesPerShard;

    // GW-U-2404 (AC-3, principle 1, 6.8): the existing invariants with the step on, over 1,000 bases in four shards of 250.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomBasesWithTheStepConserveEveryItemAndKeepTheInvariants(int shard)
    {
        Assert.Equal(CorpusSize, ShardCount * BasesPerShard);
        OrganizeFuzzInvariants.Stats total = OrganizeFuzzInvariants.Stats.Zero;
        int first = shard * BasesPerShard;
        for (int index = first; index < first + BasesPerShard; index++)
        {
            GeneratedBase generated = Generate(index);
            try
            {
                total = total.Add(OrganizeFuzzInvariants.Check(generated.Input, autoAssignMixedChests: true));
            }
            catch (Xunit.Sdk.XunitException failure)
            {
                OrganizeFuzzShrinker.Shrunk? shrunk = OrganizeFuzzShrinker.Shrink(
                    Seed,
                    index,
                    generated.Input.Containers.Length,
                    input => OrganizeFuzzInvariants.Check(input, autoAssignMixedChests: true));
                Assert.Fail(OrganizeFuzzShrinker.Report(Seed, generated, failure, shrunk));
            }
        }

        Assert.True(total.Usable > 0, "The shard " + shard + " planned no usable chest, so it checked nothing.");
    }

    // GW-U-2427 (AC-6, 6.8 property): over every generated base, the step only turns SourceOnly entries into Assigned and leaves the rest alone.
    [Fact]
    public void TheStepOnlyAssignsSourceOnlyChestsOnEveryGeneratedBase()
    {
        for (int index = 0; index < CorpusSize; index++)
        {
            GeneratedBase generated = Generate(index);
            PlanInput input = generated.Input;
            CategoryCatalog catalog = input.Catalog;
            IReadOnlyDictionary<string, ItemKind> kinds = input.Kinds;
            ContainerSnapshot[] usable = Run(input).AutoAssign.Chests.Select(chest => chest.Container).ToArray();
            AutoAssignResult before = AutoAssigner.Resolve(usable, input.Assignments, catalog, kinds, input.Threshold);

            AutoAssignResult after = AutoAssigner.AssignMixedChests(before, catalog, kinds);

            string where = "seed " + Seed + " base " + index;
            Assert.True(before.Chests.Count == after.Chests.Count, where + ": the chest count changed.");
            for (int i = 0; i < before.Chests.Count; i++)
            {
                ChestAutoAssignment was = before.Chests[i];
                ChestAutoAssignment now = after.Chests[i];
                Assert.True(ReferenceEquals(was.Container, now.Container), where + ": the container of " + was.Id.Id + " changed.");
                Assert.Equal(was.Id, now.Id);
                if (was.Outcome == AutoAssignOutcome.SourceOnly)
                {
                    Assert.True(
                        now.Outcome == AutoAssignOutcome.SourceOnly || now.Outcome == AutoAssignOutcome.Assigned,
                        where + ": source chest " + was.Id.Id + " became " + now.Outcome + ".");
                    if (now.Outcome == AutoAssignOutcome.Assigned)
                    {
                        Assert.True(now.CategoryName is not null && now.Assignment == CategoryAssignment.ForCategory(now.CategoryName), where + ": " + was.Id.Id + " has no category.");
                    }
                }
                else
                {
                    Assert.True(was.Outcome == now.Outcome, where + ": chest " + was.Id.Id + " changed its outcome.");
                    Assert.True(was.Assignment == now.Assignment, where + ": chest " + was.Id.Id + " changed its assignment.");
                    Assert.True(was.CategoryName == now.CategoryName, where + ": chest " + was.Id.Id + " changed its category name.");
                }
            }

            Assert.True(
                before.OverflowChests.Select(c => c.Id).SequenceEqual(after.OverflowChests.Select(c => c.Id)),
                where + ": the overflow chests changed.");
        }
    }
}
