namespace BaseStorage.Core.Tests.Organize;

using System.Linq;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>GW-U-541: every misplaced stack is moved or reported, and an in-place stack never moves (contract 3.2 step 5).</summary>
public sealed class OrganizeMisplacedTests
{
    // GW-U-541
    [Theory]
    [InlineData("E")]
    [InlineData("Merge")]
    [InlineData("Overflow")]
    [InlineData("Solo702")]
    public void EveryInputStackIsAccountedForExactlyWhenItIsMisplaced(string name)
    {
        PlanInput input = Named(name);
        OrganizePlan plan = Run(input);
        (uint Chest, int Index)[] misplaced = MisplacedStacksOf(name);

        foreach (ContainerSnapshot container in input.Containers)
        {
            if (plan.FindLayout(container.Id) is null)
            {
                continue;
            }

            for (int i = 0; i < container.Contents.Count; i++)
            {
                int count = container.Contents[i].Count;
                int placed = plan.Moves.Where(m => m.Source == container.Id && m.SourceStackIndex == i).Sum(m => m.Count);
                int noRoom = plan.NoRoom.Where(e => e.Source == container.Id && e.SourceStackIndex == i).Sum(e => e.Count);
                bool isMisplaced = misplaced.Any(p => p.Chest == container.Id.Id && p.Index == i);
                Assert.Equal(isMisplaced ? count : 0, placed + noRoom);
            }
        }
    }

    // GW-U-541 (derived)
    [Fact]
    public void ChestPersistedAsAnUnknownCategoryMovesItsStackAndReceivesNothing()
    {
        OrganizePlan plan = Run(Iron());

        Assert.Equal(new[] { Move(801, 0, 802, "Wood", 1, 50, 5, "Wood") }, MovesOf(plan));
        Assert.DoesNotContain(plan.Moves, m => m.Destination == Id(801));
    }

    /// <summary>The (chest, index) pairs of the stacks that are misplaced in each named base, from contract 6.2 to 6.7.</summary>
    private static (uint Chest, int Index)[] MisplacedStacksOf(string name) => name switch
    {
        "E" => new[] { (201u, 1), (203u, 0), (209u, 0), (209u, 1), (209u, 2), (209u, 3), (209u, 4) },
        "Merge" => new[] { (303u, 0), (303u, 1), (303u, 2) },
        "Overflow" => new[] { (402u, 0), (402u, 1), (402u, 2), (402u, 3) },
        "Solo702" => new[] { (702u, 0), (702u, 1), (702u, 2) },
        _ => throw new System.ArgumentException("No misplaced table for " + name, nameof(name)),
    };
}
