namespace BaseStorage.Core.Tests.Restock;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Restock;
using BaseStorage.Core.Snapshots.Identities;

/// <summary>GW-U-2530 (S-QR-001 AC-6, contract section 1, 4 and 7.9): the restock surface is exactly five types, and the result types are immutable.</summary>
public sealed class RestockSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Restock";
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-2530
    [Fact]
    public void TheRestockSurfaceIsExactlyFiveTypes()
    {
        string[] names = typeof(RestockPlanner).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .Select(type => type.FullName ?? string.Empty)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Restock.CarriedMarks",
                "BaseStorage.Core.Restock.CarriedStack",
                "BaseStorage.Core.Restock.RestockMove",
                "BaseStorage.Core.Restock.RestockPlan",
                "BaseStorage.Core.Restock.RestockPlanner",
            },
            names);

        foreach (Type type in new[] { typeof(RestockPlan), typeof(RestockMove) })
        {
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            Assert.Null(type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static));
            Assert.Null(type.GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static));
        }

        Assert.True(typeof(RestockPlanner).IsAbstract && typeof(RestockPlanner).IsSealed, "RestockPlanner is not static.");

        Type[] valueTypes = { typeof(CarriedStack), typeof(RestockMove), typeof(RestockPlan), typeof(RestockPlanner), typeof(ItemStackIdentity) };
        foreach (Type type in valueTypes)
        {
            foreach (PropertyInfo property in type.GetProperties(Members))
            {
                Assert.True(property.SetMethod is null, type.Name + "." + property.Name + " has a setter.");
            }
        }
    }
}
