namespace BaseStorage.Core.Tests.Categories;

using System;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Tests.Snapshots;

/// <summary>AC-1 (contract 1, 0.1): the exported surface of BaseStorage.Core.Categories matches the contract.</summary>
public sealed class CategoriesSurfaceTests
{
    private const string CategoriesNamespace = "BaseStorage.Core.Categories";
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const BindingFlags AnyInstanceField = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly Type[] ResultTypes =
    {
        typeof(CategoryParseResult),
        typeof(CategoryCatalog),
        typeof(CategoryDefinition),
        typeof(CategoryParseIssue),
    };

    // GW-U-158
    [Fact]
    public void ExportedTypesAreExactlyTheEightContractTypes()
    {
        string[] exported = ContractTypes.CoreAssembly().GetExportedTypes()
            .Where(type => type.Namespace == CategoriesNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Categories.CategoryCatalog",
                "BaseStorage.Core.Categories.CategoryDefinition",
                "BaseStorage.Core.Categories.CategoryFile",
                "BaseStorage.Core.Categories.CategoryParseIssue",
                "BaseStorage.Core.Categories.CategoryParseIssueKind",
                "BaseStorage.Core.Categories.CategoryParseResult",
                "BaseStorage.Core.Categories.FallbackCategories",
                "BaseStorage.Core.Categories.ItemKind",
            },
            exported);
    }

    // GW-U-158
    [Fact]
    public void StaticClassesAreAbstractAndSealed()
    {
        foreach (Type type in new[] { typeof(CategoryFile), typeof(FallbackCategories) })
        {
            Assert.True(type.IsAbstract && type.IsSealed, type.Name + " must be a static class.");
        }
    }

    // GW-U-158
    [Fact]
    public void ResultTypesAreSealedWithoutPublicConstructors()
    {
        foreach (Type type in ResultTypes)
        {
            Assert.True(type.IsSealed, type.Name + " must be sealed.");
        }

        foreach (Type type in ResultTypes.Where(type => type != typeof(CategoryParseIssue)))
        {
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            string[] methodNames = type.GetMethods(PublicDeclared).Select(m => m.Name).ToArray();
            Assert.DoesNotContain("Equals", methodNames);
            Assert.DoesNotContain("GetHashCode", methodNames);
        }
    }

    // GW-U-158
    [Fact]
    public void EveryPublicPropertyIsGetOnlyAndEveryInstanceFieldIsReadOnly()
    {
        foreach (Type type in ResultTypes)
        {
            foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
            {
                Assert.True(property.SetMethod is null, type.Name + "." + property.Name + " must be get-only.");
            }

            foreach (FieldInfo field in type.GetFields(AnyInstanceField))
            {
                Assert.True(field.IsInitOnly, type.Name + "." + field.Name + " must be readonly.");
            }
        }
    }

    // GW-U-158
    [Fact]
    public void FileParseAndCatalogMembersMatchTheContractSignatures()
    {
        MethodInfo parse = Method(typeof(CategoryFile), "Parse");
        Assert.Equal(typeof(CategoryParseResult), parse.ReturnType);
        Assert.Equal(new[] { "text" }, ParameterNames(parse));
        Assert.Equal(new[] { typeof(string) }, ParameterTypes(parse));

        Assert.Equal(typeof(CategoryCatalog), Property(typeof(CategoryParseResult), "Catalog").PropertyType);
        Assert.Equal(typeof(System.Collections.Generic.IReadOnlyList<CategoryParseIssue>), Property(typeof(CategoryParseResult), "Issues").PropertyType);

        Assert.True(Property(typeof(CategoryCatalog), "Empty").GetMethod!.IsStatic);
        Assert.Equal(typeof(System.Collections.Generic.IReadOnlyList<CategoryDefinition>), Property(typeof(CategoryCatalog), "Categories").PropertyType);
        Assert.Equal(typeof(System.Collections.Generic.IReadOnlyList<string>), Property(typeof(CategoryCatalog), "CategoryNames").PropertyType);

        MethodInfo indexOf = Method(typeof(CategoryCatalog), "IndexOf");
        Assert.Equal(typeof(int), indexOf.ReturnType);
        Assert.Equal(new[] { "categoryName" }, ParameterNames(indexOf));

        MethodInfo mapped = Method(typeof(CategoryCatalog), "GetMappedCategory");
        Assert.Equal(typeof(string), mapped.ReturnType);
        Assert.Equal(new[] { "prefabName" }, ParameterNames(mapped));

        MethodInfo resolve = Method(typeof(CategoryCatalog), "Resolve");
        Assert.Equal(typeof(string), resolve.ReturnType);
        Assert.Equal(new[] { "prefabName", "kind" }, ParameterNames(resolve));
        Assert.Equal(new[] { typeof(string), typeof(ItemKind) }, ParameterTypes(resolve));

        Assert.Equal(typeof(string), Property(typeof(CategoryDefinition), "Name").PropertyType);
        Assert.Equal(typeof(System.Collections.Generic.IReadOnlyList<string>), Property(typeof(CategoryDefinition), "Prefabs").PropertyType);

        MethodInfo forKind = Method(typeof(FallbackCategories), "For");
        Assert.Equal(typeof(string), forKind.ReturnType);
        Assert.Equal(new[] { "kind" }, ParameterNames(forKind));
    }

    // GW-U-158
    [Fact]
    public void ParseIssueConstructorAndPropertiesMatchTheContract()
    {
        ConstructorInfo? ctor = typeof(CategoryParseIssue).GetConstructor(new[]
        {
            typeof(int), typeof(CategoryParseIssueKind), typeof(string), typeof(string), typeof(string), typeof(string),
        });
        Assert.NotNull(ctor);
        Assert.Equal(
            new[] { "lineNumber", "kind", "lineText", "categoryName", "prefabName", "keptCategoryName" },
            ParameterNames(ctor!));

        Assert.Equal(typeof(int), Property(typeof(CategoryParseIssue), "LineNumber").PropertyType);
        Assert.Equal(typeof(CategoryParseIssueKind), Property(typeof(CategoryParseIssue), "Kind").PropertyType);
        Assert.Equal(typeof(LogSeverity), Property(typeof(CategoryParseIssue), "Severity").PropertyType);
        Assert.Equal(typeof(string), Property(typeof(CategoryParseIssue), "LineText").PropertyType);
        Assert.Equal(typeof(string), Property(typeof(CategoryParseIssue), "CategoryName").PropertyType);
        Assert.Equal(typeof(string), Property(typeof(CategoryParseIssue), "PrefabName").PropertyType);
        Assert.Equal(typeof(string), Property(typeof(CategoryParseIssue), "KeptCategoryName").PropertyType);
    }

    // GW-U-158
    [Fact]
    public void NullableAnnotationsMatchTheContract()
    {
        Assert.True(IsNullableReturn(Method(typeof(CategoryCatalog), "GetMappedCategory")));
        Assert.False(IsNullableReturn(Method(typeof(CategoryCatalog), "Resolve")));

        Assert.True(IsNullable(Property(typeof(CategoryParseIssue), "CategoryName")));
        Assert.True(IsNullable(Property(typeof(CategoryParseIssue), "PrefabName")));
        Assert.True(IsNullable(Property(typeof(CategoryParseIssue), "KeptCategoryName")));
        Assert.False(IsNullable(Property(typeof(CategoryParseIssue), "LineText")));

        ConstructorInfo ctor = typeof(CategoryParseIssue).GetConstructors().Single();
        ParameterInfo[] parameters = ctor.GetParameters();
        Assert.True(IsNullable(parameters.Single(p => p.Name == "categoryName")));
        Assert.True(IsNullable(parameters.Single(p => p.Name == "prefabName")));
        Assert.True(IsNullable(parameters.Single(p => p.Name == "keptCategoryName")));
        Assert.False(IsNullable(parameters.Single(p => p.Name == "lineText")));
    }

    // GW-U-158
    [Fact]
    public void EnumValuesAreTheContractedIntegers()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(CategoryParseIssueKind)));
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(ItemKind)));
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, Enum.GetValues<CategoryParseIssueKind>().Select(value => (int)value));
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, Enum.GetValues<ItemKind>().Select(value => (int)value));
    }

    private static MethodInfo Method(Type type, string name)
    {
        MethodInfo? method = type.GetMethod(name, PublicDeclared);
        Assert.True(method is not null, type.Name + "." + name + " is missing.");
        return method!;
    }

    private static PropertyInfo Property(Type type, string name)
    {
        PropertyInfo? property = type.GetProperty(name, PublicDeclared);
        Assert.True(property is not null, type.Name + "." + name + " is missing.");
        return property!;
    }

    private static string[] ParameterNames(MethodBase method) =>
        method.GetParameters().Select(p => p.Name ?? string.Empty).ToArray();

    private static Type[] ParameterTypes(MethodBase method) =>
        method.GetParameters().Select(p => p.ParameterType).ToArray();

    private static bool IsNullable(PropertyInfo property) =>
        new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable;

    private static bool IsNullable(ParameterInfo parameter) =>
        new NullabilityInfoContext().Create(parameter).ReadState == NullabilityState.Nullable;

    private static bool IsNullableReturn(MethodInfo method) =>
        new NullabilityInfoContext().Create(method.ReturnParameter).ReadState == NullabilityState.Nullable;
}
