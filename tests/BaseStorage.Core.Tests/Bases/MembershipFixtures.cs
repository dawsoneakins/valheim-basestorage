namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>
/// The contract 7.1 world of S-E002-002 as literals (ADR-GWIN-023). Expected ids and reasons in the tests are copied from
/// contract section 7 and are never computed by calling Core. Every container is width 4, height 2, with empty contents.
/// </summary>
internal static class MembershipFixtures
{
    private static readonly ItemStackSnapshot[] NoContents = Array.Empty<ItemStackSnapshot>();

    /// <summary>Station S(1,1) at (0, 0, 0), build range 20, base A.</summary>
    internal static readonly StationSnapshot SA1 = new StationSnapshot(new StationId(1, 1), new WorldPosition(0f, 0f, 0f), 20f);

    /// <summary>Station S(1,2) at (15, 0, 0), build range 20, base A.</summary>
    internal static readonly StationSnapshot SA2 = new StationSnapshot(new StationId(1, 2), new WorldPosition(15f, 0f, 0f), 20f);

    /// <summary>Station S(2,1) at (50, 0, 0), build range 20, base B.</summary>
    internal static readonly StationSnapshot SB1 = new StationSnapshot(new StationId(2, 1), new WorldPosition(50f, 0f, 0f), 20f);

    /// <summary>Station S(3,1) at (8, 0, -8), build range 0, no base.</summary>
    internal static readonly StationSnapshot SZ = new StationSnapshot(new StationId(3, 1), new WorldPosition(8f, 0f, -8f), 0f);

    /// <summary>Plain chest, base A (PlayerBuilt).</summary>
    internal static readonly ContainerSnapshot C1 = Box(1, 5f, 0f, 5f, ContainerFlags.PlayerBuilt);

    /// <summary>Exactly 20 m horizontal from SA1, Y = 40: boundary, member of nothing (PlayerBuilt).</summary>
    internal static readonly ContainerSnapshot C2 = Box(2, -12f, 40f, -16f, ContainerFlags.PlayerBuilt);

    /// <summary>18 m from SA2 and 17 m from SB1: member of A and B (PlayerBuilt).</summary>
    internal static readonly ContainerSnapshot C3 = Box(3, 33f, 0f, 0f, ContainerFlags.PlayerBuilt);

    /// <summary>Cart chest (PlayerBuilt, Vehicle).</summary>
    internal static readonly ContainerSnapshot C4 = Box(4, 5f, 0f, -5f, ContainerFlags.PlayerBuilt | ContainerFlags.Vehicle);

    /// <summary>Tombstone, also not PlayerBuilt (multi-rule).</summary>
    internal static readonly ContainerSnapshot C5 = Box(5, 0f, 0f, 10f, ContainerFlags.Tombstone);

    /// <summary>Locked chest (PlayerBuilt, AccessDenied).</summary>
    internal static readonly ContainerSnapshot C6 = Box(6, 10f, 0f, 10f, ContainerFlags.PlayerBuilt | ContainerFlags.AccessDenied);

    /// <summary>Not player-built (no flags).</summary>
    internal static readonly ContainerSnapshot C7 = Box(7, -5f, 0f, 0f, ContainerFlags.None);

    /// <summary>Keep chest (PlayerBuilt).</summary>
    internal static readonly ContainerSnapshot C8 = Box(8, 15f, 0f, 5f, ContainerFlags.PlayerBuilt);

    /// <summary>Open by another player (PlayerBuilt, InUseByOther): still eligible.</summary>
    internal static readonly ContainerSnapshot C9 = Box(9, 15f, 0f, -5f, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther);

    /// <summary>Matches all five rules (Vehicle, Tombstone, AccessDenied, Keep).</summary>
    internal static readonly ContainerSnapshot C10 = Box(10, 2f, 0f, 2f, ContainerFlags.Vehicle | ContainerFlags.Tombstone | ContainerFlags.AccessDenied);

    /// <summary>Multi-rule (Vehicle, AccessDenied, Keep); the reason flips with ExcludeVehicles.</summary>
    internal static readonly ContainerSnapshot C11 = Box(11, 3f, 0f, 3f, ContainerFlags.Vehicle | ContainerFlags.AccessDenied);

    /// <summary>Named category "Ores" (PlayerBuilt): not Keep.</summary>
    internal static readonly ContainerSnapshot C12 = Box(12, 8f, 0f, 0f, ContainerFlags.PlayerBuilt);

    /// <summary>Base B only (PlayerBuilt).</summary>
    internal static readonly ContainerSnapshot C13 = Box(13, 55f, 0f, 0f, ContainerFlags.PlayerBuilt);

    /// <summary>C2 moved to (-12, 40, -15.9): horizontal 19.92, inside the range (M-5).</summary>
    internal static readonly ContainerSnapshot C2Prime = Box(2, -12f, 40f, -15.9f, ContainerFlags.PlayerBuilt);

    /// <summary>A second snapshot with the id of C1, far away at (500, 0, 0) (M-10, M-11).</summary>
    internal static readonly ContainerSnapshot C1Far = Box(1, 500f, 0f, 0f, ContainerFlags.PlayerBuilt);

    /// <summary>C1 through C13 in id order (contract 7.1).</summary>
    internal static readonly ContainerSnapshot[] All =
    {
        C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13,
    };

    /// <summary>The Keep, named and stray assignments of contract 7.1, as a fresh dictionary on every call.</summary>
    /// <returns>A new dictionary holding C8 Keep, C10 Keep, C11 Keep, C12 "Ores", and the stray C(99,99) Keep.</returns>
    internal static Dictionary<ContainerId, CategoryAssignment> Assignments()
    {
        return new Dictionary<ContainerId, CategoryAssignment>
        {
            [C8.Id] = CategoryAssignment.Keep,
            [C10.Id] = CategoryAssignment.Keep,
            [C11.Id] = CategoryAssignment.Keep,
            [C12.Id] = CategoryAssignment.ForCategory("Ores"),
            [new ContainerId(99, 99)] = CategoryAssignment.Keep,
        };
    }

    /// <summary>The same five entries as <see cref="Assignments"/>, inserted in reverse key order.</summary>
    /// <returns>A new dictionary with the same contents and a reversed insertion order.</returns>
    internal static Dictionary<ContainerId, CategoryAssignment> AssignmentsReversed()
    {
        return new Dictionary<ContainerId, CategoryAssignment>
        {
            [new ContainerId(99, 99)] = CategoryAssignment.Keep,
            [C12.Id] = CategoryAssignment.ForCategory("Ores"),
            [C11.Id] = CategoryAssignment.Keep,
            [C10.Id] = CategoryAssignment.Keep,
            [C8.Id] = CategoryAssignment.Keep,
        };
    }

    /// <summary>A container with the given flags at (0, 0, 0), id (20, 1), for single-container rows.</summary>
    /// <param name="flags">The flags of the container.</param>
    /// <returns>A new snapshot.</returns>
    internal static ContainerSnapshot Container(ContainerFlags flags)
    {
        return new ContainerSnapshot(new ContainerId(20, 1), new WorldPosition(0f, 0f, 0f), 4, 2, NoContents, flags);
    }

    /// <summary>The literal members of base A (contract 7.2, M-1): every container of <see cref="All"/> except C2 and C13.</summary>
    /// <returns>A new array in id order: C1, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12.</returns>
    internal static ContainerSnapshot[] MembersOfA()
    {
        return new[] { C1, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12 };
    }

    /// <summary>The ids of a list, as (UserId, Id) pairs in list order.</summary>
    /// <param name="containers">The list to read.</param>
    /// <returns>One pair per element.</returns>
    internal static (long UserId, uint Id)[] Ids(IReadOnlyList<ContainerSnapshot> containers)
    {
        return containers.Select(c => (c.Id.UserId, c.Id.Id)).ToArray();
    }

    /// <summary>The excluded containers of a result, as (UserId, Id, Reason) triples in list order.</summary>
    /// <param name="result">The result to read.</param>
    /// <returns>One triple per excluded container.</returns>
    internal static (long UserId, uint Id, ExclusionReason Reason)[] Excl(EligibilityResult result)
    {
        return result.Excluded.Select(e => (e.Container.Id.UserId, e.Container.Id.Id, e.Reason)).ToArray();
    }

    /// <summary>Asserts that a list holds exactly the given instances, in order, by reference.</summary>
    /// <param name="expected">The fixture instances expected.</param>
    /// <param name="actual">The list under test.</param>
    internal static void AssertSameInstances(ContainerSnapshot[] expected, IReadOnlyList<ContainerSnapshot> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Same(expected[i], actual[i]);
        }
    }

    private static ContainerSnapshot Box(uint serial, float x, float y, float z, ContainerFlags flags)
    {
        return new ContainerSnapshot(new ContainerId(10, serial), new WorldPosition(x, y, z), 4, 2, NoContents, flags);
    }
}
