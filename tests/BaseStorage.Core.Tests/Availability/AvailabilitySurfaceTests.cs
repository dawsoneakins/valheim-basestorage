namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;

/// <summary>AC-4 (contract 9.1 to 9.5, R3, R4, V10): the exported shape of BaseStorage.Core.Availability, checked by reflection.</summary>
public sealed class AvailabilitySurfaceTests
{
    private const string AvailabilityNamespace = "BaseStorage.Core.Availability";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Type[] AvailabilityClasses =
    {
        typeof(AvailabilityResult),
        typeof(BaseAvailability),
        typeof(BaseSnapshot),
        typeof(CraftRequirement),
        typeof(RequirementAvailability),
    };

    // GW-U-430 (AC-4, contract 9; amended by ADR-GWIN-029 for S-E003-002): the exported types are exactly the nine names, ordinal-sorted, all in the Core assembly.
    [Fact]
    public void ExportedTypesAreExactlyTheNineNames()
    {
        string[] exported = typeof(CoreAssemblyMarker).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == AvailabilityNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Availability.AvailabilityResult",
                "BaseStorage.Core.Availability.BaseAvailability",
                "BaseStorage.Core.Availability.BaseSnapshot",
                "BaseStorage.Core.Availability.ConsumptionPlan",
                "BaseStorage.Core.Availability.ConsumptionPlanner",
                "BaseStorage.Core.Availability.ConsumptionTake",
                "BaseStorage.Core.Availability.CraftRequirement",
                "BaseStorage.Core.Availability.QualityMatch",
                "BaseStorage.Core.Availability.RequirementAvailability",
            },
            exported);
    }

    // GW-U-430 (AC-4, 9.1): QualityMatch has underlying type int, with values Any = 0, Exact = 1 and AtLeast = 2.
    [Fact]
    public void QualityMatchIsAnIntEnumWithTheContractValues()
    {
        Type type = typeof(QualityMatch);

        Assert.Equal(typeof(int), Enum.GetUnderlyingType(type));
        Assert.Equal(new[] { "Any", "Exact", "AtLeast" }, Enum.GetNames(type));
        Assert.Equal(new[] { 0, 1, 2 }, Enum.GetValues(type).Cast<QualityMatch>().Select(value => (int)value));
    }

    // GW-U-430 (AC-4, 9.5): BaseAvailability is static, with exactly the two contract methods, and FindFirstMissing returns a nullable value.
    [Fact]
    public void BaseAvailabilityIsStaticWithExactlyTheTwoContractMethods()
    {
        Type type = typeof(BaseAvailability);

        Assert.True(type.IsAbstract && type.IsSealed, "BaseAvailability must be static.");
        Assert.Equal(new[] { "Compute", "FindFirstMissing" }, PublicMethodNames(type));

        MethodInfo compute = type.GetMethod("Compute", PublicDeclared)!;
        Assert.Equal(typeof(AvailabilityResult), compute.ReturnType);
        Assert.Equal(NullabilityState.NotNull, new NullabilityInfoContext().Create(compute.ReturnParameter).ReadState);
        Assert.Equal(
            new[] { typeof(BaseSnapshot), typeof(IReadOnlyList<CraftRequirement>) },
            compute.GetParameters().Select(parameter => parameter.ParameterType));

        MethodInfo findFirstMissing = type.GetMethod("FindFirstMissing", PublicDeclared)!;
        Assert.Equal(typeof(RequirementAvailability), findFirstMissing.ReturnType);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(findFirstMissing.ReturnParameter).ReadState);
        Assert.Equal(
            new[] { typeof(BaseSnapshot), typeof(IReadOnlyList<CraftRequirement>) },
            findFirstMissing.GetParameters().Select(parameter => parameter.ParameterType));
    }

    // GW-U-430 (AC-4, 9.5): BaseSnapshot has one public constructor with the four contract parameters.
    [Fact]
    public void BaseSnapshotHasOnePublicConstructorWithTheContractParameters()
    {
        ConstructorInfo[] constructors = typeof(BaseSnapshot).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Single(constructors);
        ParameterInfo[] parameters = constructors[0].GetParameters();
        Assert.Equal(new[] { "playerInventory", "containers", "assignments", "options" }, parameters.Select(parameter => parameter.Name));
        Assert.Equal(
            new[]
            {
                typeof(ContainerSnapshot),
                typeof(IReadOnlyList<ContainerSnapshot>),
                typeof(IReadOnlyDictionary<ContainerId, CategoryAssignment>),
                typeof(EligibilityOptions),
            },
            parameters.Select(parameter => parameter.ParameterType));
    }

    // GW-U-430 (AC-4, 9.4, 9.5): AvailabilityResult and the static BaseAvailability have no public constructor.
    [Fact]
    public void ResultHasNoPublicConstructor()
    {
        Assert.Empty(typeof(AvailabilityResult).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    // GW-U-430 (AC-4, R4, 9.2 to 9.5): the classes are sealed, every public property is get-only, and no public field is declared.
    [Fact]
    public void ClassesAreSealedWithGetOnlyPropertiesAndNoPublicFields()
    {
        foreach (Type type in AvailabilityClasses)
        {
            Assert.True(type.IsSealed, type.Name + " must be sealed.");

            PropertyInfo[] properties = type.GetProperties(PublicDeclared);
            Assert.All(properties, property => Assert.False(property.CanWrite, type.Name + "." + property.Name + " must be get-only."));
            Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        }
    }

    // GW-U-430 (AC-4, V10): every parameter, return and property type of these classes is System.* or BaseStorage.Core.*.
    [Fact]
    public void EverySignatureTypeIsSystemOrCore()
    {
        var offenders = new List<string>();
        foreach (Type type in AvailabilityClasses)
        {
            foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
            {
                AddIfForeign(offenders, type.Name + "." + property.Name, property.PropertyType);
            }

            foreach (MethodInfo method in type.GetMethods(PublicDeclared))
            {
                AddIfForeign(offenders, type.Name + "." + method.Name + " return", method.ReturnType);
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    AddIfForeign(offenders, type.Name + "." + method.Name + " " + parameter.Name, parameter.ParameterType);
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static string[] PublicMethodNames(Type type)
    {
        return type.GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddIfForeign(List<string> offenders, string origin, Type type)
    {
        if (type.HasElementType)
        {
            AddIfForeign(offenders, origin, type.GetElementType()!);
            return;
        }

        if (type.IsGenericType)
        {
            AddIfForeignNamespace(offenders, origin, type.GetGenericTypeDefinition());
            foreach (Type argument in type.GetGenericArguments())
            {
                AddIfForeign(offenders, origin, argument);
            }

            return;
        }

        AddIfForeignNamespace(offenders, origin, type);
    }

    private static void AddIfForeignNamespace(List<string> offenders, string origin, Type type)
    {
        string ns = type.Namespace ?? string.Empty;
        if (!ns.StartsWith("System", StringComparison.Ordinal) && !ns.StartsWith("BaseStorage.Core", StringComparison.Ordinal))
        {
            offenders.Add($"{origin} uses {type.FullName}");
        }
    }
}
