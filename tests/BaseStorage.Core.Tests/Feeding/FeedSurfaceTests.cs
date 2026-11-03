namespace BaseStorage.Core.Tests.Feeding;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>The public surface of <c>BaseStorage.Core.Feeding</c> (contract section 1, 0.1 R3/R4, 2.1, 3.1, 3.2).</summary>
public sealed class FeedSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Feeding";

    // GW-U-2641
    [Fact]
    public void FM1_ShapeNumericValuesAreContracted()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(FeedStationShape)));
        Assert.Equal(0, (int)FeedStationShape.SmelterClass);
        Assert.Equal(1, (int)FeedStationShape.FuelOnly);
        Assert.Equal(2, (int)FeedStationShape.SingleInput);
        Assert.Equal(3, (int)FeedStationShape.SeveralInput);
        Assert.Equal(4, Enum.GetValues(typeof(FeedStationShape)).Length);
    }

    // GW-U-2642
    [Fact]
    public void TheFeedingSurfaceIsSixTypesWithFactoriesOnly()
    {
        string[] exported = typeof(FeedPlanner).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "FeedEntry", "FeedOrder", "FeedPlanner", "FeedSelection", "FeedStationShape", "FeedStationState" }, exported);

        Type state = typeof(FeedStationState);
        Assert.True(state.IsSealed);
        Assert.Null(state.GetMethod("<Clone>$"));
        Assert.Empty(state.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        MethodInfo[] factories = state.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();
        Assert.Equal(5, factories.Length);
        Assert.All(factories, factory => Assert.Equal(state, factory.ReturnType));

        Assert.Equal(
            new[] { typeof(int), typeof(int), typeof(float), typeof(float), typeof(string), typeof(IReadOnlyList<string>) },
            ParameterTypes(state, "SmelterClass"));
        Assert.Equal(new[] { typeof(float), typeof(float), typeof(string) }, ParameterTypes(state, "FuelOnly"));
        Assert.Equal(new[] { typeof(bool), typeof(IReadOnlyList<string>) }, ParameterTypes(state, "SingleInput"));
        Assert.Equal(new[] { typeof(int), typeof(int), typeof(IReadOnlyList<string>) }, ParameterTypes(state, "SeveralInput"));
        Assert.Equal(new[] { typeof(int), typeof(int), typeof(float), typeof(float), typeof(string), typeof(IReadOnlyList<string>) }, ParameterTypes(state, "SeveralInputFuelled"));

        Assert.Equal(typeof(float), state.GetProperty("MaxFuel")!.PropertyType);
        foreach (PropertyInfo property in state.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            Assert.Null(property.SetMethod);
        }

        Assert.DoesNotContain(state.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), field => !field.IsInitOnly);

        Type planner = typeof(FeedPlanner);
        Assert.True(planner.IsAbstract && planner.IsSealed);
        MethodInfo[] planMethods = planner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();
        Assert.Equal(3, planMethods.Length);
        Assert.Equal(new[] { 5, 7 }, planMethods.Where(method => method.Name == "Next").Select(method => method.GetParameters().Length).OrderBy(count => count).ToArray());
        Assert.Equal(
            new[]
            {
                typeof(FeedStationState),
                typeof(FeedSelection),
                typeof(IReadOnlyList<ContainerSnapshot>),
                typeof(IReadOnlyDictionary<ContainerId, CategoryAssignment>),
                typeof(EligibilityOptions),
                typeof(WorldPosition),
                typeof(ICollection<ContainerId>),
            },
            planMethods.Single(method => method.Name == "Next" && method.GetParameters().Length == 7).GetParameters().Select(parameter => parameter.ParameterType).ToArray());

        Assert.Empty(typeof(FeedOrder).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Single(typeof(FeedOrder).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));
    }

    private static Type[] ParameterTypes(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
}
