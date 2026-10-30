namespace BaseStorage.Core.Tests.Fakes;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>
/// AC-2 and AC-3: <see cref="ConservationAssert.Holds"/> passes on conserving states, names each item type whose total
/// differs, validates its arguments and checks limits (stack max and chest capacity) on the after state only.
/// </summary>
public sealed class ConservationAssertTests
{
    private const long UserId = 3141592653L;

    // GW-U-074
    [Fact]
    public void IdenticalStatesPass()
    {
        ContainerSnapshot[] state = { FakeFixtures.ChestA(), FakeFixtures.ChestB() };

        ConservationAssert.Holds(state, state);
    }

    // GW-U-074
    [Fact]
    public void EmptyBeforeAndEmptyAfterPass()
    {
        ConservationAssert.Holds(Array.Empty<ContainerSnapshot>(), Array.Empty<ContainerSnapshot>());
    }

    // GW-U-074
    [Fact]
    public void ContractStepsFourAndFivePass()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot b = FakeFixtures.ChestB();

        ConservationAssert.Holds(
            new[] { a, b },
            new[] { WithContents(a, Wood(17)), WithContents(b, Ore(30), Ore(12)) });
    }

    // GW-U-074
    [Fact]
    public void SplitMergeAndReorderWithinOneChestPass()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot split = WithContents(a, Ore(20), Ore(10), Wood(17));

        ConservationAssert.Holds(new[] { a }, new[] { split });
        ConservationAssert.Holds(new[] { split }, new[] { WithContents(a, Ore(30), Wood(17)) });
        ConservationAssert.Holds(new[] { a }, new[] { WithContents(a, Wood(17), Ore(30)) });
    }

    // GW-U-074
    [Fact]
    public void DepositFromInventoryToChestPasses()
    {
        ItemStackSnapshot stone = new("Stone", 20, 1, 50);
        ContainerSnapshot inventoryBefore = FakeFixtures.Inventory(stone);
        ContainerSnapshot inventoryAfter = FakeFixtures.Inventory();
        ContainerSnapshot b = FakeFixtures.ChestB();

        ConservationAssert.Holds(
            new[] { inventoryBefore, b },
            new[] { inventoryAfter, WithContents(b, Ore(12), stone) });
    }

    // GW-U-074
    [Fact]
    public void BeforeAndAfterInDifferentContainerOrderPass()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot b = FakeFixtures.ChestB();

        ConservationAssert.Holds(new[] { a, b }, new[] { b, a });
    }

    // GW-U-075
    [Fact]
    public void NamesEachTypeWhoseTotalDiffersAndNotTheUnchangedOne()
    {
        ItemStackSnapshot stone = new("Stone", 5, 1, 50);
        ContainerSnapshot inventory = FakeFixtures.Inventory(stone);
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot b = FakeFixtures.ChestB();

        Xunit.Sdk.XunitException ex = Fails(
            new[] { inventory, a, b },
            new[] { inventory, WithContents(a, Wood(34)), b });

        Assert.Contains("CopperOre", ex.Message);
        Assert.Contains("lost 30", ex.Message);
        Assert.Contains("Wood", ex.Message);
        Assert.Contains("duplicated 17", ex.Message);
        Assert.DoesNotContain("Stone", ex.Message);
    }

    // GW-U-075
    [Fact]
    public void ItemQualityIsPartOfTheKey()
    {
        ContainerSnapshot before = FakeFixtures.Inventory(new ItemStackSnapshot("SwordIron", 1, 1, 1));
        ContainerSnapshot after = FakeFixtures.Inventory(new ItemStackSnapshot("SwordIron", 1, 2, 1));

        Xunit.Sdk.XunitException ex = Fails(new[] { before }, new[] { after });

        Assert.Contains("SwordIron (quality 1)", ex.Message);
        Assert.Contains("lost 1", ex.Message);
        Assert.Contains("SwordIron (quality 2)", ex.Message);
        Assert.Contains("duplicated 1", ex.Message);
    }

    // GW-U-075
    [Fact]
    public void PrefabNamesCompareOrdinally()
    {
        ContainerSnapshot before = FakeFixtures.Inventory(Wood(17));
        ContainerSnapshot after = FakeFixtures.Inventory(new ItemStackSnapshot("wood", 17, 1, 50));

        Xunit.Sdk.XunitException ex = Fails(new[] { before }, new[] { after });

        Assert.Contains("Wood (quality 1)", ex.Message);
        Assert.Contains("wood (quality 1)", ex.Message);
    }

    // GW-U-075
    [Fact]
    public void ContainerMissingFromAfterReportsItsItemsLost()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot b = FakeFixtures.ChestB();

        Xunit.Sdk.XunitException ex = Fails(new[] { a, b }, new[] { b });

        Assert.Contains("CopperOre", ex.Message);
        Assert.Contains("lost 30", ex.Message);
        Assert.Contains("Wood", ex.Message);
        Assert.Contains("lost 17", ex.Message);
    }

    // GW-U-076
    [Theory]
    [InlineData("NullBefore", typeof(ArgumentNullException), "before")]
    [InlineData("NullAfter", typeof(ArgumentNullException), "after")]
    [InlineData("NullElementInAfter", typeof(ArgumentException), "after")]
    [InlineData("DuplicateIdInBefore", typeof(ArgumentException), "before")]
    public void InvalidStatesThrowWithTheMatchingParamName(string scenario, Type expectedType, string paramName)
    {
        Exception? actual = Record.Exception(() => RunInvalid(scenario));

        Assert.NotNull(actual);
        Assert.IsType(expectedType, actual);
        Assert.Equal(paramName, ((ArgumentException)actual).ParamName);
    }

    // GW-U-077
    [Fact]
    public void OverFullStackInTheAfterStateFailsWhenTotalsAreConserved()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot inventoryBefore = FakeFixtures.Inventory(Wood(50), Wood(8));

        Xunit.Sdk.XunitException ex = Fails(
            new[] { a, inventoryBefore },
            new[] { WithContents(a, Ore(30), Wood(75)), FakeFixtures.Inventory() });

        Assert.Contains("3141592653:42", ex.Message);
        Assert.Contains("Wood", ex.Message);
        Assert.Contains("75/50", ex.Message);
    }

    // GW-U-077
    [Fact]
    public void StackExactlyAtMaxPasses()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();

        ConservationAssert.Holds(
            new[] { a, FakeFixtures.Inventory(Wood(33)) },
            new[] { WithContents(a, Ore(30), Wood(50)), FakeFixtures.Inventory() });
    }

    // GW-U-077
    [Fact]
    public void OverFullBeforeWithAConformingAfterPasses()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();

        ConservationAssert.Holds(
            new[] { WithContents(a, Ore(30), Wood(75)), FakeFixtures.Inventory() },
            new[] { WithContents(a, Ore(30), Wood(50)), FakeFixtures.Inventory(Wood(25)) });
    }

    // GW-U-078
    [Fact]
    public void OneByOneChestWithTwoStacksFailsAndNamesTheContainer()
    {
        ContainerSnapshot before = FakeFixtures.Inventory(Ore(30), Wood(17));

        Xunit.Sdk.XunitException ex = Fails(
            new[] { before },
            new[] { FakeFixtures.Inventory(), OneByOne(90u, Ore(30), Wood(17)) });

        Assert.Contains("3141592653:90", ex.Message);
        Assert.Contains("2 stacks", ex.Message);
        Assert.Contains("1 slots", ex.Message);
    }

    // GW-U-078
    [Fact]
    public void ExactlyFullContainerPasses()
    {
        ContainerSnapshot a = FakeFixtures.ChestA();
        ItemStackSnapshot[] tenStacks = Enumerable.Repeat(new ItemStackSnapshot("Stone", 1, 1, 50), 10).ToArray();

        ConservationAssert.Holds(
            new[] { FakeFixtures.Inventory(tenStacks) },
            new[] { WithContents(a, tenStacks), FakeFixtures.Inventory() });
    }

    // GW-U-078
    [Fact]
    public void PlayerInventoryIsCheckedForCapacity()
    {
        ItemStackSnapshot[] thirtyThreeStacks = Enumerable.Repeat(new ItemStackSnapshot("Stone", 1, 1, 50), 33).ToArray();
        ContainerSnapshot inventory = FakeFixtures.Inventory(thirtyThreeStacks);

        Xunit.Sdk.XunitException ex = Fails(new[] { inventory }, new[] { inventory });

        Assert.Contains("3141592653:7", ex.Message);
        Assert.Contains("33 stacks", ex.Message);
    }

    // GW-U-078
    [Fact]
    public void LostItemOverFullStackAndOverCapacityAreReportedTogether()
    {
        ContainerSnapshot before = FakeFixtures.Inventory(Ore(30), Wood(50), Wood(25), Stone(1), Stone(2));

        Xunit.Sdk.XunitException ex = Fails(
            new[] { before },
            new[]
            {
                FakeFixtures.Inventory(),
                WithContents(FakeFixtures.ChestA(), Wood(75)),
                OneByOne(90u, Stone(1), Stone(2)),
            });

        Assert.Contains("(3 problems)", ex.Message);
        Assert.Contains("CopperOre", ex.Message);
        Assert.Contains("lost 30", ex.Message);
        Assert.Contains("Wood 75/50", ex.Message);
        Assert.Contains("3141592653:90", ex.Message);
        Assert.Contains("2 stacks", ex.Message);
    }

    private static Xunit.Sdk.XunitException Fails(IEnumerable<ContainerSnapshot> before, IEnumerable<ContainerSnapshot> after) =>
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => ConservationAssert.Holds(before, after));

    private static void RunInvalid(string scenario)
    {
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot[] valid = { a };

        switch (scenario)
        {
            case "NullBefore":
                ConservationAssert.Holds(null!, valid);
                break;
            case "NullAfter":
                ConservationAssert.Holds(valid, null!);
                break;
            case "NullElementInAfter":
                ConservationAssert.Holds(valid, new[] { a, null! });
                break;
            case "DuplicateIdInBefore":
                ConservationAssert.Holds(new[] { a, a }, valid);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    private static ContainerSnapshot OneByOne(uint serial, params ItemStackSnapshot[] contents) => new(
        new ContainerId(UserId, serial),
        new WorldPosition(0f, 0f, 0f),
        1,
        1,
        contents,
        ContainerFlags.PlayerBuilt);
}
