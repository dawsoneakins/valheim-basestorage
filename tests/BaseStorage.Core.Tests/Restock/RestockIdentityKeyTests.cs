namespace BaseStorage.Core.Tests.Restock;

using System.Collections.Generic;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Snapshots.Identities;
using static BaseStorage.Core.Tests.Restock.RestockFixtures;
using static BaseStorage.Core.Tests.Snapshots.Identities.IdentityFixtures;

/// <summary>S-QR-001 AC-3 and AC-5 (contract 7.2 and 7.6): the same key only, carried order, and items of a different identity.</summary>
public sealed class RestockIdentityKeyTests
{
    // GW-U-2495 (Q8)
    [Fact]
    public void Q08_TargetsAreServedGreedilyInCarriedOrder()
    {
        RestockPlan a = RestockPlanner.Plan(
            new List<CarriedStack> { H(S("Wood", 10)), H(S("Wood", 20)) },
            Origin(),
            new List<ContainerSnapshot> { C(901, 1, 0, 2, S("Wood", 50)) },
            NoAssign(),
            Craftable());
        Assert.Equal(new[] { Row(901, 0, 0, 40), Row(901, 0, 1, 10) }, Shape(a));
        Assert.Equal(20, a.RemainingRoom);

        RestockPlan b = RestockPlanner.Plan(
            new List<CarriedStack> { H(S("Wood", 10)), F(S("Wood", 45)) },
            Origin(),
            new List<ContainerSnapshot> { C(902, 1, 0, 2, S("Wood", 30)) },
            NoAssign(),
            Craftable());
        Assert.Equal(new[] { Row(902, 0, 0, 30) }, Shape(b));
        Assert.Equal(15, b.RemainingRoom);

        RestockPlan c = RestockPlanner.Plan(
            new List<CarriedStack> { F(S("Wood", 45)), H(S("Wood", 10)) },
            Origin(),
            new List<ContainerSnapshot> { C(902, 1, 0, 2, S("Wood", 30)) },
            NoAssign(),
            Craftable());
        Assert.Equal(new[] { Row(902, 0, 0, 5), Row(902, 0, 1, 25) }, Shape(c));
        Assert.Equal(15, c.RemainingRoom);
    }

    // GW-U-2512 (Q11)
    [Fact]
    public void Q11_SameItemDifferentIdentityIsCountedUpToNeed()
    {
        // Wood50 with world level 2 gives 40 left, because only a different identity exists.
        RestockPlan wl2Only = OneChest(H(S("Wood", 10)), ContainerFlags.PlayerBuilt, 1, SI("Wood", 50, Wl2()));
        Assert.Empty(wl2Only.Moves);
        Assert.Equal(0, wl2Only.LeftInUse);
        Assert.Equal(40, wl2Only.LeftDifferentIdentity);

        // Wood30 with another durability gives 30, capped by the spare count.
        RestockPlan worn = OneChest(H(S("Wood", 10)), ContainerFlags.PlayerBuilt, 1, SI("Wood", 30, Worn()));
        Assert.Empty(worn.Moves);
        Assert.Equal(30, worn.LeftDifferentIdentity);

        // The same key fills first, and the rest is counted as a different identity.
        RestockPlan mixed = OneChest(H(S("Wood", 10)), ContainerFlags.PlayerBuilt, 2, S("Wood", 25), SI("Wood", 50, Wl2()));
        Assert.Equal(new[] { Row(905, 0, 0, 25) }, Shape(mixed));
        Assert.Equal(15, mixed.LeftDifferentIdentity);

        // A different quality or a different max stack is a different item, so nothing is counted.
        RestockPlan quality = OneChest(H(S("Wood", 10)), ContainerFlags.PlayerBuilt, 1, S("Wood", 50, 50, 2));
        Assert.Empty(quality.Moves);
        Assert.Equal(0, quality.LeftDifferentIdentity);

        RestockPlan maxStack = OneChest(H(S("Wood", 10)), ContainerFlags.PlayerBuilt, 1, S("Wood", 50, 60));
        Assert.Empty(maxStack.Moves);
        Assert.Equal(0, maxStack.LeftDifferentIdentity);

        // Equal identities with the custom data in another order are the same key.
        var erikReordered = new ItemStackIdentity(100f, 8812345012L, "Erik", 1, 0, new[] { KV("dyed", "red"), KV("rune", "frost") });
        RestockPlan reordered = OneChest(H(SI("Wood", 10, Erik())), ContainerFlags.PlayerBuilt, 1, SI("Wood", 50, erikReordered));
        Assert.Equal(new[] { Row(905, 0, 0, 40) }, Shape(reordered));
        Assert.Equal(0, reordered.LeftDifferentIdentity);

        // The same identity with a different crafter name is a different key.
        var astrid = new ItemStackIdentity(100f, 8812345012L, "Astrid", 1, 0, ErikData());
        RestockPlan crafter = OneChest(H(SI("Wood", 10, Erik())), ContainerFlags.PlayerBuilt, 1, SI("Wood", 50, astrid));
        Assert.Empty(crafter.Moves);
        Assert.Equal(40, crafter.LeftDifferentIdentity);

        // In-use items of world level 2 leave 30 in use and 10 with a different identity.
        var inUse = new List<ContainerSnapshot>
        {
            CF(962, 1, 0, 2, ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, S("Wood", 30)),
            C(963, 2, 0, 2, SI("Wood", 50, Wl2())),
        };
        RestockPlan inUsePlan = RestockPlanner.Plan(new List<CarriedStack> { H(S("Wood", 10)) }, Origin(), inUse, NoAssign(), Craftable());
        Assert.Equal(30, inUsePlan.LeftInUse);
        Assert.Equal(10, inUsePlan.LeftDifferentIdentity);

        // In-use world-level-2 items alone leave nothing counted.
        RestockPlan inUseOther = OneChest(H(S("Wood", 10)), ContainerFlags.PlayerBuilt | ContainerFlags.InUseByOther, 1, SI("Wood", 50, Wl2()));
        Assert.Equal(0, inUseOther.LeftInUse);
        Assert.Equal(0, inUseOther.LeftDifferentIdentity);

        // Contract 7.2, row 4: carried Wood10 with world level 2 and a chest with three Wood stacks gives 25 left.
        RestockPlan row4 = RestockPlanner.Plan(
            new List<CarriedStack> { H(SI("Wood", 10, Wl2())) },
            Origin(),
            new List<ContainerSnapshot> { C(903, 1, 0, 3, S("Wood", 50), SI("Wood", 15, Wl2()), SI("Wood", 40, Worn())) },
            NoAssign(),
            Craftable());
        Assert.Equal(new[] { Row(903, 1, 0, 15) }, Shape(row4));
        Assert.Equal(25, row4.RemainingRoom);
        Assert.Equal(25, row4.LeftDifferentIdentity);
    }

    private static RestockPlan OneChest(CarriedStack carried, ContainerFlags flags, int slots, params ItemStackSnapshot[] contents) =>
        RestockPlanner.Plan(new List<CarriedStack> { carried }, Origin(), new List<ContainerSnapshot> { CF(905, 1, 0, slots, flags, contents) }, NoAssign(), Craftable());
}
