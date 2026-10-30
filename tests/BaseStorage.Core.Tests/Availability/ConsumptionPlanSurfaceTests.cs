namespace BaseStorage.Core.Tests.Availability;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;

/// <summary>AC-15 (ADR-GWIN-028): the exported shape of the three planner types, checked by reflection.</summary>
public sealed class ConsumptionPlanSurfaceTests
{
    private const BindingFlags PublicDeclared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // GW-U-459 (AC-15): ConsumptionPlanner is static, its only public method is Plan, and Plan returns a nullable ConsumptionPlan.
    [Fact]
    public void PlannerIsStaticWithExactlyThePlanMethod()
    {
        Type type = typeof(ConsumptionPlanner);

        Assert.True(type.IsAbstract && type.IsSealed, "ConsumptionPlanner must be static.");
        Assert.Equal(new[] { "Plan" }, PublicMethodNames(type));

        MethodInfo plan = type.GetMethod("Plan", PublicDeclared)!;
        Assert.Equal(typeof(ConsumptionPlan), plan.ReturnType);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(plan.ReturnParameter).ReadState);
        Assert.Equal(
            new[] { typeof(BaseSnapshot), typeof(IReadOnlyList<CraftRequirement>) },
            plan.GetParameters().Select(parameter => parameter.ParameterType));
    }

    // GW-U-459 (AC-15): ConsumptionPlan is sealed, has no public constructor, and has exactly four get-only public properties of the contract types.
    [Fact]
    public void PlanIsSealedWithNoPublicConstructorAndTheFourContractProperties()
    {
        Type type = typeof(ConsumptionPlan);

        Assert.True(type.IsSealed, "ConsumptionPlan must be sealed.");
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(
            new[] { "ExpectedContainers", "PlayerInventoryId", "Requirements", "Takes" },
            type.GetProperties(PublicDeclared).Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(typeof(ContainerId), type.GetProperty("PlayerInventoryId", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<CraftRequirement>), type.GetProperty("Requirements", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<ConsumptionTake>), type.GetProperty("Takes", PublicDeclared)!.PropertyType);
        Assert.Equal(typeof(IReadOnlyList<ContainerSnapshot>), type.GetProperty("ExpectedContainers", PublicDeclared)!.PropertyType);
        Assert.All(type.GetProperties(PublicDeclared), property => Assert.Null(property.SetMethod));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
    }

    // GW-U-459 (AC-15): ConsumptionTake is sealed, implements IEquatable, defines the two operators, and has one public constructor with the contract parameter names.
    [Fact]
    public void TakeIsSealedEquatableWithOperatorsAndOneContractConstructor()
    {
        Type type = typeof(ConsumptionTake);

        Assert.True(type.IsSealed, "ConsumptionTake must be sealed.");
        Assert.Contains(typeof(IEquatable<ConsumptionTake>), type.GetInterfaces());
        Assert.NotNull(type.GetMethod("op_Equality", PublicDeclared));
        Assert.NotNull(type.GetMethod("op_Inequality", PublicDeclared));

        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.Single(constructors);
        ParameterInfo[] parameters = constructors[0].GetParameters();
        Assert.Equal(new[] { "containerId", "stackIndex", "stack", "count", "requirementIndex" }, parameters.Select(parameter => parameter.Name));
        Assert.Equal(
            new[] { typeof(ContainerId), typeof(int), typeof(ItemStackSnapshot), typeof(int), typeof(int) },
            parameters.Select(parameter => parameter.ParameterType));
    }

    // GW-U-459 (AC-15): ConsumptionTake has exactly the five get-only properties, and no public setter or public field.
    [Fact]
    public void TakePropertiesAreGetOnlyAndNoPublicFieldIsDeclared()
    {
        Type type = typeof(ConsumptionTake);

        Assert.Equal(
            new[] { "ContainerId", "Count", "RequirementIndex", "Stack", "StackIndex" },
            type.GetProperties(PublicDeclared).Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
        Assert.All(type.GetProperties(PublicDeclared), property => Assert.Null(property.SetMethod));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
    }

    // GW-U-459 (AC-15, V10): every parameter, return and property type of the three planner types is System.* or BaseStorage.Core.*.
    [Fact]
    public void EverySignatureTypeIsSystemOrCore()
    {
        var offenders = new List<string>();
        foreach (Type type in new[] { typeof(ConsumptionPlanner), typeof(ConsumptionPlan), typeof(ConsumptionTake) })
        {
            foreach (PropertyInfo property in type.GetProperties(PublicDeclared))
            {
                AddIfForeign(offenders, type.Name + "." + property.Name, property.PropertyType);
            }

            foreach (MethodInfo method in type.GetMethods(PublicDeclared).Where(method => !method.IsSpecialName))
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
