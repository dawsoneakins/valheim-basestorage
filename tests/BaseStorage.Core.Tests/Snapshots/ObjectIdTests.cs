namespace BaseStorage.Core.Tests.Snapshots;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>AC-4 and AC-5: StationId and ContainerId share one shape, so each row runs for both types.</summary>
public sealed class ObjectIdTests
{
    // GW-U-030
    [Theory]
    [InlineData("StationId")]
    [InlineData("ContainerId")]
    public void UserIdAndIdRoundTrip(string typeName)
    {
        dynamic id = ContractTypes.Make(ContractTypes.Snapshot(typeName), 3141592653L, 17u);

        Assert.Equal(3141592653L, (long)id.UserId);
        Assert.Equal(17u, (uint)id.Id);
    }

    // GW-U-030
    [Theory]
    [InlineData("StationId")]
    [InlineData("ContainerId")]
    public void IsNoneIsTrueOnlyForZeroUserIdAndZeroId(string typeName)
    {
        Type type = ContractTypes.Snapshot(typeName);
        dynamic fromDefault = ContractTypes.Default(type);
        dynamic zeroZero = ContractTypes.Make(type, 0L, 0u);
        dynamic zeroSeven = ContractTypes.Make(type, 0L, 7u);

        Assert.True((bool)fromDefault.IsNone);
        Assert.True((bool)zeroZero.IsNone);
        Assert.False((bool)zeroSeven.IsNone);
    }

    // GW-U-030
    [Theory]
    [InlineData("StationId", 2718281828L, 900u, 3141592653L, 1u, -1)]
    [InlineData("ContainerId", 3141592653L, 5u, 3141592653L, 4u, 1)]
    [InlineData("StationId", 3141592653L, 17u, 3141592653L, 17u, 0)]
    [InlineData("ContainerId", 2718281828L, 1u, 3141592653L, 1u, -1)]
    [InlineData("StationId", 3141592653L, 1u, 3141592653L, 2u, -1)]
    public void CompareToOrdersByUserIdThenIdAndTheSignIsContracted(
        string typeName,
        long leftUserId,
        uint leftId,
        long rightUserId,
        uint rightId,
        int expectedSign)
    {
        Type type = ContractTypes.Snapshot(typeName);
        dynamic left = ContractTypes.Make(type, leftUserId, leftId);
        dynamic right = ContractTypes.Make(type, rightUserId, rightId);

        int result = left.CompareTo(right);

        Assert.Equal(expectedSign, Math.Sign(result));
    }

    // GW-U-030
    [Fact]
    public void StationIdAndContainerIdAreDistinctAndHaveNoConversionBetweenThem()
    {
        Type station = ContractTypes.Snapshot("StationId");
        Type container = ContractTypes.Snapshot("ContainerId");
        object stationId = ContractTypes.Make(station, 1L, 1u);
        object containerId = ContractTypes.Make(container, 1L, 1u);

        Assert.False(stationId.Equals(containerId));
        Assert.False(containerId.Equals(stationId));
        string[] conversions = ConversionsBetween(station, container);
        Assert.Empty(conversions);
    }

    // GW-U-033
    [Theory]
    [InlineData("StationId")]
    [InlineData("ContainerId")]
    public void EqualPairsHaveEqualHashCodesAndAgreeOnEveryEqualityPath(string typeName)
    {
        Type type = ContractTypes.Snapshot(typeName);

        ValueSemantics.AssertEqual(
            ContractTypes.Make(type, 3141592653L, 17u),
            ContractTypes.Make(type, 3141592653L, 17u));
    }

    // GW-U-033
    [Theory]
    [InlineData("StationId", 3141592653L, 18u)]
    [InlineData("StationId", 2718281828L, 17u)]
    [InlineData("ContainerId", 3141592653L, 18u)]
    [InlineData("ContainerId", 2718281828L, 17u)]
    public void ChangingOnlyTheIdOrOnlyTheUserIdMakesTheIdUnequal(string typeName, long userId, uint id)
    {
        Type type = ContractTypes.Snapshot(typeName);

        ValueSemantics.AssertNotEqual(
            ContractTypes.Make(type, 3141592653L, 17u),
            ContractTypes.Make(type, userId, id));
    }

    private static string[] ConversionsBetween(Type first, Type second)
    {
        IEnumerable<MethodInfo> methods = first.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Concat(second.GetMethods(BindingFlags.Public | BindingFlags.Static));

        return methods
            .Where(m => m.Name is "op_Implicit" or "op_Explicit")
            .Where(m => Mentions(m, first) && Mentions(m, second))
            .Select(m => m.DeclaringType + "." + m.Name)
            .Distinct()
            .ToArray();
    }

    private static bool Mentions(MethodInfo method, Type type) =>
        method.ReturnType == type || method.GetParameters().Any(p => p.ParameterType == type);
}
