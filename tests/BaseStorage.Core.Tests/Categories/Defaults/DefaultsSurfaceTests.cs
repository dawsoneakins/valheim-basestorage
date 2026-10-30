namespace BaseStorage.Core.Tests.Categories.Defaults;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Categories.Defaults;

/// <summary>AC-1 (contract 0.1, 1, 2.1, 3.0, 4.1, 5.1): the exported surface of BaseStorage.Core.Categories.Defaults matches the contract.</summary>
public sealed class DefaultsSurfaceTests
{
    private const string DefaultsNamespace = "BaseStorage.Core.Categories.Defaults";
    private const BindingFlags AnyDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Type[] Types =
    {
        typeof(CategoryItem),
        typeof(DefaultCategories),
        typeof(DefaultCategoryFile),
        typeof(DefaultCategoryFileMessages),
    };

    // GW-U-458
    [Fact]
    public void ExportedTypesAreExactlyTheFourContractTypes()
    {
        string[] exported = typeof(CategoryItem).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == DefaultsNamespace)
            .Select(type => type.FullName!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "BaseStorage.Core.Categories.Defaults.CategoryItem",
                "BaseStorage.Core.Categories.Defaults.DefaultCategories",
                "BaseStorage.Core.Categories.Defaults.DefaultCategoryFile",
                "BaseStorage.Core.Categories.Defaults.DefaultCategoryFileMessages",
            },
            exported);
    }

    // GW-U-458
    [Fact]
    public void StaticClassesAreAbstractSealedAndCategoryItemIsSealedAndEquatable()
    {
        Assert.True(typeof(DefaultCategories).IsAbstract && typeof(DefaultCategories).IsSealed);
        Assert.True(typeof(DefaultCategoryFile).IsAbstract && typeof(DefaultCategoryFile).IsSealed);
        Assert.True(typeof(DefaultCategoryFileMessages).IsAbstract && typeof(DefaultCategoryFileMessages).IsSealed);
        Assert.True(typeof(CategoryItem).IsSealed);
        Assert.True(typeof(IEquatable<CategoryItem>).IsAssignableFrom(typeof(CategoryItem)));
    }

    // GW-U-458
    [Fact]
    public void EveryPublicPropertyIsGetOnlyAndEveryInstanceFieldIsReadonly()
    {
        foreach (Type type in Types)
        {
            foreach (PropertyInfo property in type.GetProperties(AnyDeclared))
            {
                Assert.Null(property.SetMethod);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.True(field.IsInitOnly, $"{type.Name}.{field.Name} is not readonly.");
            }
        }
    }

    // GW-U-458
    [Fact]
    public void PublicMembersMatchTheContractSignatures()
    {
        ConstructorInfo constructor = Assert.Single(typeof(CategoryItem).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(new[] { ("prefabName", typeof(string)), ("kind", typeof(ItemKind)) }, Parameters(constructor));

        MethodInfo isValid = Method(typeof(CategoryItem), "IsValidPrefabName", typeof(string));
        Assert.Equal(typeof(bool), isValid.ReturnType);
        Assert.Equal(NullabilityState.Nullable, Nullability(isValid.GetParameters()[0]));

        MethodInfo equals = Method(typeof(CategoryItem), "Equals", typeof(CategoryItem));
        Assert.Equal(typeof(bool), equals.ReturnType);
        Assert.Equal(NullabilityState.Nullable, Nullability(equals.GetParameters()[0]));

        Assert.Equal(typeof(string), typeof(CategoryItem).GetProperty("PrefabName")!.PropertyType);
        Assert.Equal(typeof(ItemKind), typeof(CategoryItem).GetProperty("Kind")!.PropertyType);

        Assert.Equal(typeof(IReadOnlyList<string>), typeof(DefaultCategories).GetProperty("Names")!.PropertyType);
        Assert.Equal(typeof(CategoryCatalog), typeof(DefaultCategories).GetProperty("Catalog")!.PropertyType);
        AssertSignature(Method(typeof(DefaultCategories), "GetPrefabs", typeof(string)), typeof(IReadOnlyList<string>), ("categoryName", typeof(string)));

        AssertSignature(
            Method(typeof(DefaultCategoryFile), "Generate", typeof(IEnumerable<CategoryItem>)),
            typeof(string),
            ("items", typeof(IEnumerable<CategoryItem>)));

        AssertSignature(
            Method(typeof(DefaultCategoryFileMessages), "Written", typeof(string), typeof(int)),
            typeof(string),
            ("path", typeof(string)),
            ("itemCount", typeof(int)));
        AssertSignature(
            Method(typeof(DefaultCategoryFileMessages), "KeptExisting", typeof(string)),
            typeof(string),
            ("path", typeof(string)));
        AssertSignature(
            Method(typeof(DefaultCategoryFileMessages), "WriteFailed", typeof(string), typeof(Exception)),
            typeof(string),
            ("path", typeof(string)),
            ("error", typeof(Exception)));
    }

    // GW-U-458
    [Fact]
    public void FileNameIsTheOnlyPublicConstInTheNamespaceAndHasTheContractValue()
    {
        string[] publicFields = Types
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Select(field => type.Name + "." + field.Name))
            .ToArray();
        Assert.Equal(new[] { "DefaultCategoryFile.FileName" }, publicFields);

        Assert.True(typeof(DefaultCategoryFile).GetField("FileName")!.IsLiteral);
        Assert.Equal("BaseStorage.categories.txt", DefaultCategoryFile.FileName);
    }

    private static (string Name, Type Type)[] Parameters(MethodBase method) =>
        method.GetParameters().Select(parameter => (parameter.Name!, parameter.ParameterType)).ToArray();

    private static NullabilityState Nullability(ParameterInfo parameter) =>
        new NullabilityInfoContext().Create(parameter).WriteState;

    private static MethodInfo Method(Type type, string name, params Type[] parameterTypes) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, parameterTypes, null)
        ?? throw new InvalidOperationException($"{type.Name}.{name} with {parameterTypes.Length} parameter(s) is missing.");

    private static void AssertSignature(MethodInfo method, Type returnType, params (string Name, Type Type)[] parameters)
    {
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(parameters, Parameters(method));
    }
}
