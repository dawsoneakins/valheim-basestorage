namespace BaseStorage.Core.Tests.Organize.Deposit;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BaseStorage.Core.Organize.Deposit;
using BaseStorage.Core.Tests.Abstractions;

/// <summary>
/// Deposit contract sections 1, 2 and 4 (R4, R9): the exported surface of <c>BaseStorage.Core.Organize.Deposit</c> is exactly the 8
/// contracted types with the contracted members, the enums have the contracted values, every public property is get-only, and every
/// public member is documented in BaseStorage.Core.xml. The S-E004-006 and S-E004-008 pins are untouched.
/// </summary>
public sealed class DepositSurfaceTests
{
    private const string Namespace = "BaseStorage.Core.Organize.Deposit";
    private const string DocFileName = "BaseStorage.Core.xml";

    private static Type[] ExportedTypes() => typeof(DepositPlanner).Assembly.GetExportedTypes()
        .Where(type => type.Namespace == Namespace)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    // GW-U-1076 (contract 1, 2, 4): the 8 types, their members, the enum values, sealed classes with get-only properties.
    [Fact]
    public void TheDepositSurfaceIsExactlyTheContract()
    {
        Assert.Equal(
            new[]
            {
                Namespace + ".DepositExclusion",
                Namespace + ".DepositExclusions",
                Namespace + ".DepositPlan",
                Namespace + ".DepositPlanner",
                Namespace + ".ExcludedStack",
                Namespace + ".InventoryItemTraits",
                Namespace + ".PlayerInventorySnapshot",
                Namespace + ".PlayerInventoryStack",
            },
            ExportedTypes().Select(type => type.FullName));

        Type planner = typeof(DepositPlanner);
        Assert.True(planner.IsAbstract && planner.IsSealed, "DepositPlanner must be static.");
        Assert.Equal(new[] { "ExclusionOf", "HotbarRow", "Plan" }, Members(planner));
        Assert.Equal(0, (int)planner.GetField("HotbarRow")!.GetRawConstantValue()!);
        Assert.Equal(
            new[] { "PlayerInventorySnapshot", "DepositExclusions", "IReadOnlyList`1", "IReadOnlyDictionary`2", "IReadOnlyDictionary`2", "CategoryCatalog", "IReadOnlyDictionary`2", "EligibilityOptions", "Int32", "Boolean" },
            planner.GetMethod("Plan")!.GetParameters().Select(p => p.ParameterType.Name));
        Assert.Equal(typeof(DepositPlan), planner.GetMethod("Plan")!.ReturnType);
        Assert.Equal(typeof(DepositExclusion), planner.GetMethod("ExclusionOf")!.ReturnType);

        Assert.Equal(new[] { "DepositableStacks", "Excluded", "FindExcluded", "Plan", "Player" }, Members(typeof(DepositPlan)));
        Assert.Equal(new[] { "Reasons", "Source", "StackIndex" }, Members(typeof(ExcludedStack)));
        Assert.Equal(new[] { "Container", "Height", "Id", "Stacks", "Width" }, Members(typeof(PlayerInventorySnapshot)));
        Assert.Equal(new[] { "IsEquipped", "Slot", "Stack", "Traits" }, Members(typeof(PlayerInventoryStack)));
        Assert.Equal(new[] { "Favourites", "FromSettings", "IsNeverItem", "NeverItems" }, Members(typeof(DepositExclusions)));

        Assert.Empty(typeof(DepositPlan).GetConstructors());
        Assert.Empty(typeof(ExcludedStack).GetConstructors());
        Assert.Single(typeof(PlayerInventorySnapshot).GetConstructors());
        Assert.Single(typeof(PlayerInventoryStack).GetConstructors());
        Assert.Single(typeof(DepositExclusions).GetConstructors());

        foreach (Type type in ExportedTypes().Where(type => type.IsClass && type != planner))
        {
            Assert.True(type.IsSealed, type.Name + " must be sealed.");
            foreach (PropertyInfo property in type.GetProperties())
            {
                Assert.True(property.SetMethod is null, type.Name + "." + property.Name + " must be get-only.");
            }

            Assert.DoesNotContain(type.GetMethods(BindingFlags.Public | BindingFlags.Static), m => m.Name.StartsWith("op_", StringComparison.Ordinal));
        }

        var context = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.Nullable, context.Create(typeof(DepositPlan).GetMethod("FindExcluded")!.ReturnParameter).ReadState);
    }

    // GW-U-1077 (contract 2): the two flags enums have int underlying types and the contracted names and values.
    [Fact]
    public void TheEnumsHaveTheContractedValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(DepositExclusion)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(InventoryItemTraits)));
        Assert.NotNull(typeof(DepositExclusion).GetCustomAttribute<FlagsAttribute>());
        Assert.NotNull(typeof(InventoryItemTraits).GetCustomAttribute<FlagsAttribute>());
        Assert.Equal(
            new[] { ("None", 0), ("Hotbar", 1), ("Equipped", 2), ("Favourite", 4), ("NeverList", 8), ("SameItemAsExcluded", 16), ("InvalidStack", 32) },
            Enum.GetValues(typeof(DepositExclusion)).Cast<DepositExclusion>().Select(v => (v.ToString(), (int)v)));
        Assert.Equal(
            new[] { ("None", 0), ("Equipped", 1), ("Mead", 4), ("Ammo", 8), ("Bait", 16) },
            Enum.GetValues(typeof(InventoryItemTraits)).Cast<InventoryItemTraits>().Select(v => (v.ToString(), (int)v)));
    }

    // GW-U-1078 (R9, CS1591): every public member of the 8 types is documented in BaseStorage.Core.xml.
    [Fact]
    public void EveryTypeMemberAndEnumMemberIsDocumented()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DocFileName);
        Assert.True(File.Exists(path), DocFileName + " is missing from " + AppContext.BaseDirectory + ".");

        Dictionary<string, XElement> members = XDocument.Load(path).Descendants("member")
            .Where(m => m.Attribute("name") is not null)
            .ToDictionary(m => m.Attribute("name")!.Value, StringComparer.Ordinal);

        Type[] types = ExportedTypes();
        Assert.Equal(8, types.Length);

        var problems = new List<string>();
        foreach (var (id, parameters, returnsValue) in XmlDocMembers.TargetsFor(types))
        {
            if (!members.TryGetValue(id, out XElement? member))
            {
                problems.Add("missing <member>: " + id);
                continue;
            }

            if (string.IsNullOrWhiteSpace(member.Element("summary")?.Value))
            {
                problems.Add("blank or missing <summary>: " + id);
            }

            foreach (string parameter in parameters)
            {
                if (!member.Elements("param").Any(p => p.Attribute("name")?.Value == parameter))
                {
                    problems.Add("missing <param name=\"" + parameter + "\">: " + id);
                }
            }

            if (returnsValue && member.Element("returns") is null)
            {
                problems.Add("missing <returns>: " + id);
            }
        }

        Assert.Empty(problems);
    }

    private static string[] Members(Type type) => type
        .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(m => m.MemberType != MemberTypes.Constructor && !(m is MethodInfo method && method.IsSpecialName))
        .Select(m => m.Name)
        .Distinct()
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();
}
