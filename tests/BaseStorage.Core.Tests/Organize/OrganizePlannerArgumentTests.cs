namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;

/// <summary>GW-U-571: validation order and exact exception types, and nothing else throws (contract 5.2, 6.9, AC-13).</summary>
public sealed class OrganizePlannerArgumentTests
{
    // GW-U-571
    [Theory]
    [InlineData("containers-null", true, "containers")]
    [InlineData("containers-null-element", false, "containers")]
    [InlineData("containers-duplicate-id", false, "containers")]
    [InlineData("assignments-null", true, "assignments")]
    [InlineData("assignments-null-value", false, "assignments")]
    [InlineData("inUseBy-null", true, "inUseBy")]
    [InlineData("inUseBy-null-value", false, "inUseBy")]
    [InlineData("catalog-null", true, "catalog")]
    [InlineData("itemKinds-null", true, "itemKinds")]
    public void InvalidArgumentThrowsTheExactTypeAndParamName(string name, bool isNullArgument, string paramName)
    {
        Func<OrganizePlan> call = () => Call(name);

        ArgumentException thrown = isNullArgument
            ? Assert.Throws<ArgumentNullException>(call)
            : Assert.Throws<ArgumentException>(call);

        Assert.Equal(paramName, thrown.ParamName);
    }

    // GW-U-571 (order)
    [Fact]
    public void AllArgumentsNullReportsContainersFirst()
    {
        var thrown = Assert.Throws<ArgumentNullException>(() =>
            OrganizePlanner.Plan(null!, null!, null!, null!, null!, Options(), DefaultThreshold));

        Assert.Equal("containers", thrown.ParamName);
    }

    // GW-U-571 (order)
    [Fact]
    public void NullAssignmentsReportsAssignmentsBeforeInUseAndCatalog()
    {
        PlanInput input = E();

        var thrown = Assert.Throws<ArgumentNullException>(() =>
            OrganizePlanner.Plan(input.Containers, null!, null!, null!, null!, input.Options, input.Threshold));

        Assert.Equal("assignments", thrown.ParamName);
    }

    // GW-U-571 (order)
    [Fact]
    public void NullAssignmentValueReportsAssignmentsBeforeANullCatalog()
    {
        PlanInput input = E();
        var assignments = new Dictionary<ContainerId, CategoryAssignment> { [Id(201)] = null! };

        var thrown = Assert.Throws<ArgumentException>(() =>
            OrganizePlanner.Plan(input.Containers, assignments, input.InUseBy, null!, input.Kinds, input.Options, input.Threshold));

        Assert.Equal("assignments", thrown.ParamName);
    }

    // GW-U-571 (no throw)
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public void AnyThresholdIsAcceptedWithoutThrowing(int threshold)
    {
        PlanInput input = E() with { Threshold = threshold };

        Assert.NotNull(Run(input));
    }

    // GW-U-571 (no throw)
    [Fact]
    public void DefaultEligibilityOptionsAreAccepted()
    {
        PlanInput input = E() with { Options = default };

        Assert.NotNull(Run(input));
    }

    // GW-U-571 (no throw)
    [Fact]
    public void UnknownPrefabIsAcceptedAsOther()
    {
        PlanInput input = Make(new[] { C(1601, 0, 0, 2, S("Unobtainium", 3)) });

        Assert.NotNull(Run(input));
    }

    // GW-U-571 (no throw)
    [Fact]
    public void InUseIdThatIsNotInTheContainersIsIgnored()
    {
        PlanInput input = E() with { InUseBy = InUse((999, "Erik")) };

        Assert.NotNull(Run(input));
    }

    private static OrganizePlan Call(string name)
    {
        ContainerSnapshot[] chests = EChests();
        PlanInput input = E();
        return name switch
        {
            "containers-null" => OrganizePlanner.Plan(null!, input.Assignments, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold),
            "containers-null-element" => OrganizePlanner.Plan(new[] { chests[0], null! }, input.Assignments, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold),
            "containers-duplicate-id" => OrganizePlanner.Plan(new[] { chests[0], chests[0] }, input.Assignments, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold),
            "assignments-null" => OrganizePlanner.Plan(input.Containers, null!, input.InUseBy, input.Catalog, input.Kinds, input.Options, input.Threshold),
            "assignments-null-value" => OrganizePlanner.Plan(
                input.Containers,
                new Dictionary<ContainerId, CategoryAssignment> { [Id(201)] = null! },
                input.InUseBy,
                input.Catalog,
                input.Kinds,
                input.Options,
                input.Threshold),
            "inUseBy-null" => OrganizePlanner.Plan(input.Containers, input.Assignments, null!, input.Catalog, input.Kinds, input.Options, input.Threshold),
            "inUseBy-null-value" => OrganizePlanner.Plan(
                input.Containers,
                input.Assignments,
                new Dictionary<ContainerId, string> { [Id(206)] = null! },
                input.Catalog,
                input.Kinds,
                input.Options,
                input.Threshold),
            "catalog-null" => OrganizePlanner.Plan(input.Containers, input.Assignments, input.InUseBy, null!, input.Kinds, input.Options, input.Threshold),
            "itemKinds-null" => OrganizePlanner.Plan(input.Containers, input.Assignments, input.InUseBy, input.Catalog, null!, input.Options, input.Threshold),
            _ => throw new ArgumentException("Unknown argument case: " + name, nameof(name)),
        };
    }
}
