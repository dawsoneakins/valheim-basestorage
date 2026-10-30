namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;

/// <summary>AC-9, AC-12 (contract 0.3, 1, 2, 4, 5, 6): the exported shape of BaseStorage.Core.Bases.Membership, checked by reflection.</summary>
public sealed class MembershipSurfaceTests
{
    private const string MembershipNamespace = "BaseStorage.Core.Bases.Membership";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-224 (AC-9, contract 0.3): the exported types in the namespace are exactly the seven contract names, ordinal-sorted.
    [Fact]
    public void ExportedTypesAreExactlyTheSevenContractNames()
    {
        string[] exported = typeof(BaseMembership).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == MembershipNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Bases.Membership.BaseMembership",
                "BaseStorage.Core.Bases.Membership.ContainerEligibility",
                "BaseStorage.Core.Bases.Membership.ContainerExclusion",
                "BaseStorage.Core.Bases.Membership.ContainerPurpose",
                "BaseStorage.Core.Bases.Membership.EligibilityOptions",
                "BaseStorage.Core.Bases.Membership.EligibilityResult",
                "BaseStorage.Core.Bases.Membership.ExclusionReason",
            },
            exported);
    }

    // GW-U-224 (AC-9, contract 1): ContainerPurpose is an int enum, not a flags enum, with exactly Organize = 0 and CraftAvailability = 1.
    [Fact]
    public void ContainerPurposeIsAnIntEnumWithTheTwoContractValues()
    {
        Type type = typeof(ContainerPurpose);

        Assert.Equal(typeof(int), Enum.GetUnderlyingType(type));
        Assert.Empty(type.GetCustomAttributes(typeof(FlagsAttribute), false));
        Assert.Equal(new[] { "Organize", "CraftAvailability" }, Enum.GetNames(type));
        Assert.Equal(new[] { 0, 1 }, Enum.GetValues(type).Cast<ContainerPurpose>().Select(v => (int)v));
    }

    // GW-U-224 (AC-9, contract 2): ExclusionReason is an int enum, not a flags enum, with the five contract values and no zero member.
    [Fact]
    public void ExclusionReasonIsAnIntEnumWithTheFiveContractValues()
    {
        Type type = typeof(ExclusionReason);

        Assert.Equal(typeof(int), Enum.GetUnderlyingType(type));
        Assert.Empty(type.GetCustomAttributes(typeof(FlagsAttribute), false));
        Assert.Equal(new[] { "Tombstone", "Vehicle", "NotPlayerBuilt", "AccessDenied", "Keep" }, Enum.GetNames(type));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, Enum.GetValues(type).Cast<ExclusionReason>().Select(v => (int)v));
    }

    // GW-U-224 (AC-9, contract 5): ContainerExclusion.Reason is non-nullable, and GetExclusionReason returns a nullable reason.
    [Fact]
    public void ReasonTypesAreNonNullableOnTheResultAndNullableOnTheSingleRow()
    {
        Assert.Equal(typeof(ExclusionReason), typeof(ContainerExclusion).GetProperty("Reason")!.PropertyType);

        MethodInfo getExclusionReason = typeof(ContainerEligibility).GetMethod("GetExclusionReason", PublicDeclared)!;
        Assert.Equal(typeof(ExclusionReason?), getExclusionReason.ReturnType);
    }

    // GW-U-225 (AC-12, contract 4, 5, 6): BaseMembership and ContainerEligibility are static, with exactly the contracted public methods.
    [Fact]
    public void StaticClassesExposeOnlyTheContractedMethods()
    {
        Assert.True(typeof(BaseMembership).IsAbstract && typeof(BaseMembership).IsSealed, "BaseMembership must be static.");
        Assert.True(typeof(ContainerEligibility).IsAbstract && typeof(ContainerEligibility).IsSealed, "ContainerEligibility must be static.");

        Assert.Equal(new[] { "FindMembers" }, PublicMethodNames(typeof(BaseMembership)));
        Assert.Equal(new[] { "Evaluate", "GetExclusionReason" }, PublicMethodNames(typeof(ContainerEligibility)));
    }

    // GW-U-225 (AC-12, contract 5, 6): the method parameter types, including IReadOnlyDictionary<ContainerId, CategoryAssignment>.
    [Fact]
    public void PublicMethodParameterTypesAreTheContractedOnes()
    {
        ParameterInfo[] findMembers = typeof(BaseMembership).GetMethod("FindMembers", PublicDeclared)!.GetParameters();
        Assert.Equal(new[] { "stations", "containers" }, findMembers.Select(p => p.Name));
        Assert.Equal(
            new[] { typeof(IReadOnlyList<StationSnapshot>), typeof(IReadOnlyList<ContainerSnapshot>) },
            findMembers.Select(p => p.ParameterType));

        ParameterInfo[] evaluate = typeof(ContainerEligibility).GetMethod("Evaluate", PublicDeclared)!.GetParameters();
        Assert.Equal(new[] { "containers", "assignments", "purpose", "options" }, evaluate.Select(p => p.Name));
        Assert.Equal(
            new[]
            {
                typeof(IReadOnlyList<ContainerSnapshot>),
                typeof(IReadOnlyDictionary<ContainerId, CategoryAssignment>),
                typeof(ContainerPurpose),
                typeof(EligibilityOptions),
            },
            evaluate.Select(p => p.ParameterType));

        ParameterInfo[] getReason = typeof(ContainerEligibility).GetMethod("GetExclusionReason", PublicDeclared)!.GetParameters();
        Assert.Equal(new[] { "container", "isKeep", "purpose", "options" }, getReason.Select(p => p.Name));
        Assert.Equal(
            new[] { typeof(ContainerSnapshot), typeof(bool), typeof(ContainerPurpose), typeof(EligibilityOptions) },
            getReason.Select(p => p.ParameterType));
    }

    // GW-U-225 (AC-12, contract 5): the two result classes are sealed, have no public constructor, have only the listed get-only
    // properties, and do not override Equals or GetHashCode.
    [Fact]
    public void ResultClassesAreSealedWithGetOnlyPropertiesAndNoEqualityOverride()
    {
        foreach (Type type in new[] { typeof(EligibilityResult), typeof(ContainerExclusion) })
        {
            Assert.True(type.IsSealed, type.Name + " must be sealed.");
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

            string[] methods = PublicMethodNames(type);
            Assert.DoesNotContain("Equals", methods);
            Assert.DoesNotContain("GetHashCode", methods);
        }

        PropertyInfo[] result = typeof(EligibilityResult).GetProperties(PublicDeclared);
        Assert.Equal(new[] { "Eligible", "Excluded" }, result.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(result, p => Assert.False(p.CanWrite, p.Name + " must be get-only."));

        PropertyInfo[] exclusion = typeof(ContainerExclusion).GetProperties(PublicDeclared);
        Assert.Equal(new[] { "Container", "Reason" }, exclusion.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(exclusion, p => Assert.False(p.CanWrite, p.Name + " must be get-only."));
    }

    // GW-U-225 (AC-12, contract 4): EligibilityOptions is a readonly struct with IEquatable, the two operators, FromSettings and get-only properties.
    [Fact]
    public void EligibilityOptionsIsAReadonlyValueTypeWithTheContractedMembers()
    {
        Type type = typeof(EligibilityOptions);

        Assert.True(type.IsValueType, "EligibilityOptions must be a struct.");
        Assert.True(
            type.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"),
            "EligibilityOptions must be a readonly struct.");
        Assert.Contains(typeof(IEquatable<EligibilityOptions>), type.GetInterfaces());

        Assert.NotNull(type.GetMethod("op_Equality", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(type.GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static));

        MethodInfo fromSettings = type.GetMethod("FromSettings", BindingFlags.Public | BindingFlags.Static)!;
        Assert.Equal(typeof(BaseStorageSettings), fromSettings.GetParameters().Single().ParameterType);

        PropertyInfo[] properties = type.GetProperties(PublicDeclared);
        Assert.Equal(new[] { "ExcludeVehicles", "KeepChestsCraftable" }, properties.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(properties, p => Assert.False(p.CanWrite, p.Name + " must be get-only."));
    }

    private static string[] PublicMethodNames(Type type) =>
        type.GetMethods(PublicDeclared).Where(m => !m.IsSpecialName).Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
}
