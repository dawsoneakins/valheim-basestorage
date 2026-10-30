namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Categories.AutoAssign.AutoAssignFixtures;

/// <summary>AC-10 (contract 4.2, 5.7): argument rules throw the contracted exception and parameter, checked in the contracted order.</summary>
public sealed class AutoAssignerArgumentTests
{
    // GW-U-530
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void ArgumentRuleThrowsTheContractedExceptionAndParameter(int row)
    {
        var (label, act, expectedType, parameter) = Row(row);

        Exception ex = Assert.Throws(expectedType, act);

        Assert.Equal((label, parameter), (label, ((ArgumentException)ex).ParamName));
    }

    private static (string Label, Action Act, Type ExpectedType, string Parameter) Row(int row)
    {
        ContainerSnapshot[] b = B();
        return row switch
        {
            0 => ("containers is null", () => AutoAssigner.Resolve(null!, Assignments(), Catalog(), Kinds(), 50), typeof(ArgumentNullException), "containers"),
            1 => ("containers has a null element", () => AutoAssigner.Resolve(new ContainerSnapshot[] { b[0], null! }, Assignments(), Catalog(), Kinds(), 50), typeof(ArgumentException), "containers"),
            2 => ("containers has a duplicate id", () => AutoAssigner.Resolve(new[] { b[0], b[0] }, Assignments(), Catalog(), Kinds(), 50), typeof(ArgumentException), "containers"),
            3 => ("assignments is null", () => AutoAssigner.Resolve(B(), null!, Catalog(), Kinds(), 50), typeof(ArgumentNullException), "assignments"),
            4 => ("assignments has a null value", () => AutoAssigner.Resolve(B(), new Dictionary<ContainerId, CategoryAssignment> { [Id(101)] = null! }, Catalog(), Kinds(), 50), typeof(ArgumentException), "assignments"),
            5 => ("catalog is null", () => AutoAssigner.Resolve(B(), Assignments(), null!, Kinds(), 50), typeof(ArgumentNullException), "catalog"),
            6 => ("itemKinds is null", () => AutoAssigner.Resolve(B(), Assignments(), Catalog(), null!, 50), typeof(ArgumentNullException), "itemKinds"),
            7 => ("order: every argument null", () => AutoAssigner.Resolve(null!, null!, null!, null!, 50), typeof(ArgumentNullException), "containers"),
            8 => ("order: duplicate containers before null assignments", () => AutoAssigner.Resolve(new[] { b[0], b[0] }, null!, null!, null!, 50), typeof(ArgumentException), "containers"),
            9 => ("order: null value before null catalog", () => AutoAssigner.Resolve(B(), new Dictionary<ContainerId, CategoryAssignment> { [Id(101)] = null! }, null!, null!, 50), typeof(ArgumentException), "assignments"),
            _ => ("order: null catalog before null itemKinds", () => AutoAssigner.Resolve(B(), Assignments(), null!, null!, 50), typeof(ArgumentNullException), "catalog"),
        };
    }
}
