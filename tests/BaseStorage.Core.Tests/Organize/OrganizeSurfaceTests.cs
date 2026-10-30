namespace BaseStorage.Core.Tests.Organize;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Organize;

/// <summary>GW-U-573 (AC-15, contract 1, 2 and 4): the Organize namespace exports exactly the 8 contracted types, with the contracted members.</summary>
public sealed class OrganizeSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Organize";
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-573
    [Fact]
    public void ExportedTypesOfTheNamespaceAreExactlyTheEightContractedTypes()
    {
        string[] actual = typeof(OrganizePlanner).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == Namespace)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "ChestLayout", "ChestRole", "NoRoomEntry", "OrganizePlan", "OrganizePlanner", "PlannedMove", "SkipReason", "SkippedChest" },
            actual);
    }

    // GW-U-573
    [Fact]
    public void PublicMembersMatchContractSectionFourByNameReturnTypeAndParameters()
    {
        var expected = new Dictionary<Type, string[]>
        {
            [typeof(OrganizePlanner)] = new[]
            {
                "Plan(IReadOnlyList<ContainerSnapshot> containers, IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments, "
                    + "IReadOnlyDictionary<ContainerId, String> inUseBy, CategoryCatalog catalog, IReadOnlyDictionary<String, ItemKind> itemKinds, "
                    + "EligibilityOptions options, Int32 autoAssignThresholdPct):OrganizePlan",
                "Plan(IReadOnlyList<ContainerSnapshot> containers, IReadOnlyDictionary<ContainerId, CategoryAssignment> assignments, "
                    + "IReadOnlyDictionary<ContainerId, String> inUseBy, CategoryCatalog catalog, IReadOnlyDictionary<String, ItemKind> itemKinds, "
                    + "EligibilityOptions options, Int32 autoAssignThresholdPct, Boolean autoAssignMixedChests):OrganizePlan",
            },
            [typeof(OrganizePlan)] = new[]
            {
                "AutoAssign:AutoAssignResult",
                "ClaimedOverflow:IReadOnlyList<ChestLayout>",
                "FindLayout(ContainerId id):ChestLayout",
                "FindSkipped(ContainerId id):SkippedChest",
                "Layouts:IReadOnlyList<ChestLayout>",
                "Moves:IReadOnlyList<PlannedMove>",
                "NoRoom:IReadOnlyList<NoRoomEntry>",
                "Skipped:IReadOnlyList<SkippedChest>",
            },
            [typeof(PlannedMove)] = new[]
            {
                "Count:Int32",
                "CategoryName:String",
                "Destination:ContainerId",
                "MaxStack:Int32",
                "PrefabName:String",
                "Quality:Int32",
                "Source:ContainerId",
                "SourceStackIndex:Int32",
            },
            [typeof(NoRoomEntry)] = new[]
            {
                "Count:Int32",
                "CategoryName:String",
                "MaxStack:Int32",
                "PrefabName:String",
                "Quality:Int32",
                "Source:ContainerId",
                "SourceStackIndex:Int32",
            },
            [typeof(SkippedChest)] = new[]
            {
                "Container:ContainerSnapshot",
                "Exclusion:Nullable<ExclusionReason>",
                "Id:ContainerId",
                "Kind:SkipReason",
                "PlayerName:String",
                "Reason:String",
            },
            [typeof(ChestLayout)] = new[]
            {
                "CategoryName:String",
                "Container:ContainerSnapshot",
                "Id:ContainerId",
                "Role:ChestRole",
                "Stacks:IReadOnlyList<ItemStackSnapshot>",
            },
        };

        foreach (KeyValuePair<Type, string[]> pair in expected)
        {
            string[] want = pair.Value.OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Assert.Equal(want, Signatures(pair.Key));
        }
    }

    // GW-U-573
    [Fact]
    public void OrganizePlannerIsStaticAndTheOtherClassesAreSealedAndDefineNoOperators()
    {
        Assert.True(typeof(OrganizePlanner).IsAbstract && typeof(OrganizePlanner).IsSealed, "OrganizePlanner must be a static class.");

        Type[] sealedClasses = { typeof(OrganizePlan), typeof(PlannedMove), typeof(NoRoomEntry), typeof(SkippedChest), typeof(ChestLayout) };
        Assert.All(sealedClasses, type => Assert.True(type.IsSealed, type.Name + " must be sealed."));

        Type[] all = { typeof(OrganizePlanner), typeof(OrganizePlan), typeof(PlannedMove), typeof(NoRoomEntry), typeof(SkippedChest), typeof(ChestLayout) };
        string[] operators = all
            .SelectMany(type => type.GetMethods(Declared))
            .Where(method => method.Name.StartsWith("op_", StringComparison.Ordinal))
            .Select(method => method.Name)
            .ToArray();
        Assert.Empty(operators);
    }

    // GW-U-573
    [Fact]
    public void SkipReasonAndChestRoleHaveTheContractedNamesAndIntValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(SkipReason)));
        Assert.Equal(
            new[] { "Keep=0", "InUse=1", "Ineligible=2", "InvalidContents=3" },
            EnumPairs(typeof(SkipReason)));

        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ChestRole)));
        Assert.Equal(
            new[] { "Category=0", "ClaimedOverflow=1", "Source=2", "UnusedOverflow=3" },
            EnumPairs(typeof(ChestRole)));
    }

    // GW-U-573
    [Fact]
    public void NullableMembersAreAnnotatedAsNullable()
    {
        var context = new NullabilityInfoContext();

        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(OrganizePlan).GetMethod("FindLayout")!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(OrganizePlan).GetMethod("FindSkipped")!.ReturnParameter).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(SkippedChest).GetProperty("PlayerName")!).ReadState);
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(ChestLayout).GetProperty("CategoryName")!).ReadState);
        Assert.Equal(typeof(ExclusionReason), Nullable.GetUnderlyingType(typeof(SkippedChest).GetProperty("Exclusion")!.PropertyType));
    }

    private static string[] Signatures(Type type)
    {
        var signatures = new List<string>();
        foreach (PropertyInfo property in type.GetProperties(Declared))
        {
            signatures.Add(property.Name + ":" + TypeName(property.PropertyType));
        }

        foreach (MethodInfo method in type.GetMethods(Declared))
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            string parameters = string.Join(", ", method.GetParameters().Select(p => TypeName(p.ParameterType) + " " + p.Name));
            signatures.Add(method.Name + "(" + parameters + "):" + TypeName(method.ReturnType));
        }

        return signatures.OrderBy(s => s, StringComparer.Ordinal).ToArray();
    }

    private static string TypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name.Substring(0, type.Name.IndexOf('`'));
        return name + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">";
    }

    private static string[] EnumPairs(Type enumType) =>
        Enum.GetNames(enumType).Select(name => name + "=" + (int)Enum.Parse(enumType, name)).ToArray();
}
