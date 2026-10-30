namespace BaseStorage.Core.Tests.Categories.AutoAssign;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.AutoAssign;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-10 (contract 1, 2, 3, 4.1, 5.8): the exported surface of BaseStorage.Core.Categories.AutoAssign matches the contract.</summary>
public sealed class AutoAssignSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Categories.AutoAssign";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const BindingFlags AnyDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-531
    [Fact]
    public void ExportedTypesAreExactlyTheFiveContractTypes()
    {
        string[] exported = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        string[] expected = new[]
        {
            "BaseStorage.Core.Categories.AutoAssign.AutoAssigner",
            "BaseStorage.Core.Categories.AutoAssign.AutoAssignOutcome",
            "BaseStorage.Core.Categories.AutoAssign.AutoAssignResult",
            "BaseStorage.Core.Categories.AutoAssign.ChestAutoAssignment",
            "BaseStorage.Core.Categories.AutoAssign.OverflowPool",
        }.OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, exported);
    }

    // GW-U-531
    [Fact]
    public void PublicMembersMatchTheContractListsByNameAndParameterTypes()
    {
        Assert.Equal(
            new[]
            {
                Method("AssignMixedChests", typeof(AutoAssignResult), typeof(CategoryCatalog), typeof(IReadOnlyDictionary<string, ItemKind>)),
                Method("Resolve", typeof(IReadOnlyList<ContainerSnapshot>), typeof(IReadOnlyDictionary<ContainerId, CategoryAssignment>), typeof(CategoryCatalog), typeof(IReadOnlyDictionary<string, ItemKind>), typeof(int)),
            },
            Members(typeof(AutoAssigner)));

        Assert.Equal(
            new[]
            {
                Property("AutoAssignThresholdPct", typeof(int)),
                Property("Chests", typeof(IReadOnlyList<ChestAutoAssignment>)),
                Property("MixedAssigned", typeof(IReadOnlyList<ChestAutoAssignment>)),
                Property("OverflowChests", typeof(IReadOnlyList<ContainerSnapshot>)),
                Method("Find", typeof(ContainerId)),
                Method("GetCategoryChests", typeof(string)),
                Method("CreateOverflowPool"),
            }.OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            Members(typeof(AutoAssignResult)));

        Assert.Equal(
            new[]
            {
                Property("Container", typeof(ContainerSnapshot)),
                Property("Id", typeof(ContainerId)),
                Property("Outcome", typeof(AutoAssignOutcome)),
                Property("Assignment", typeof(CategoryAssignment)),
                Property("CategoryName", typeof(string)),
            }.OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            Members(typeof(ChestAutoAssignment)));

        Assert.Equal(
            new[]
            {
                Property("Count", typeof(int)),
                Method("GetRemaining"),
                Method("Claim", typeof(string)),
            }.OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            Members(typeof(OverflowPool)));
    }

    // GW-U-531
    [Fact]
    public void EnumHasTheContractNamesAndValuesWithAnIntUnderlyingType()
    {
        Assert.Equal(new[] { "AlreadyAssigned", "Keep", "Assigned", "SourceOnly", "Overflow" }, Enum.GetNames(typeof(AutoAssignOutcome)));
        Assert.Equal(0, (int)AutoAssignOutcome.AlreadyAssigned);
        Assert.Equal(1, (int)AutoAssignOutcome.Keep);
        Assert.Equal(2, (int)AutoAssignOutcome.Assigned);
        Assert.Equal(3, (int)AutoAssignOutcome.SourceOnly);
        Assert.Equal(4, (int)AutoAssignOutcome.Overflow);
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(AutoAssignOutcome)));
    }

    // GW-U-531
    [Fact]
    public void TheResolverIsAbstractAndSealedAndTheOtherClassesAreSealedWithoutPublicConstructors()
    {
        Assert.True(typeof(AutoAssigner).IsAbstract && typeof(AutoAssigner).IsSealed, "AutoAssigner must be a static class.");

        foreach (Type type in new[] { typeof(AutoAssignResult), typeof(ChestAutoAssignment), typeof(OverflowPool) })
        {
            Assert.True(type.IsSealed, type.Name + " must be sealed.");
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        }
    }

    // GW-U-531
    [Fact]
    public void EveryPublicPropertyIsGetOnly()
    {
        foreach (Type type in SurfaceTypes())
        {
            foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
            {
                Assert.True(property.SetMethod is null, type.Name + "." + property.Name + " has a setter.");
            }
        }
    }

    // GW-U-531
    [Fact]
    public void EveryInstanceFieldIsReadOnlyAndNoTypeDeclaresAStaticField()
    {
        foreach (Type type in SurfaceTypes())
        {
            foreach (FieldInfo field in type.GetFields(AnyDeclared))
            {
                Assert.False(field.IsStatic, type.Name + "." + field.Name + " is static.");
                Assert.True(field.IsInitOnly, type.Name + "." + field.Name + " is not readonly.");
            }
        }
    }

    // GW-U-531
    [Fact]
    public void NullableAnnotationsOnTheContractedMembers()
    {
        var context = new NullabilityInfoContext();

        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(ChestAutoAssignment).GetProperty(nameof(ChestAutoAssignment.Assignment))!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(ChestAutoAssignment).GetProperty(nameof(ChestAutoAssignment.CategoryName))!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(AutoAssignResult).GetMethod(nameof(AutoAssignResult.Find))!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(OverflowPool).GetMethod(nameof(OverflowPool.Claim))!.ReturnParameter).ReadState);
    }

    private static Type[] SurfaceTypes() => new[]
    {
        typeof(AutoAssigner),
        typeof(AutoAssignResult),
        typeof(ChestAutoAssignment),
        typeof(OverflowPool),
    };

    /// <summary>The public declared methods (without accessors) and properties of a type, as sorted signature strings.</summary>
    private static string[] Members(Type type)
    {
        IEnumerable<string> methods = type.GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName)
            .Select(method => Method(method.Name, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray()));
        IEnumerable<string> properties = type.GetProperties(PublicDeclared)
            .Select(property => Property(property.Name, property.PropertyType));
        return methods.Concat(properties).OrderBy(signature => signature, StringComparer.Ordinal).ToArray();
    }

    private static string Method(string name, params Type[] parameterTypes) =>
        "method " + name + "(" + string.Join(", ", parameterTypes.Select(type => type.FullName)) + ")";

    private static string Property(string name, Type type) => "property " + name + ":" + type.FullName;
}
